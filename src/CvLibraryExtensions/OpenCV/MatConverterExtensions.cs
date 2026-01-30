using OpenCvSharp;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CvLibraryExtensions.OpenCV
{
    public static class MatConverterExtensions
    {
        /// <summary>
        /// 高效地将 Mat 转换为 BitmapFrame（避免使用 ToMemoryStream）
        /// </summary>
        /// <param name="mat">源 Mat 对象</param>
        /// <returns>线程安全的 BitmapFrame，如果 Mat 为空则返回 null</returns>
        public static BitmapFrame? ToBitmapFrame(this Mat mat)
        {
            if (mat.Empty())
            {
                return null;
            }

            return Application.Current?.Dispatcher.CheckAccess() == true
                ? CreateBitmapFrameDirect(mat)
                : Application.Current?.Dispatcher.Invoke(() => CreateBitmapFrameDirect(mat));
        }

        /// <summary>
        /// 异步高效转换 Mat 到 BitmapFrame
        /// </summary>
        /// <param name="mat">源 Mat 对象</param>
        /// <returns>BitmapFrame 任务</returns>
        public static async Task<BitmapFrame?> ToBitmapFrameAsync(this Mat mat)
        {
            if (mat.Empty())
            {
                return null;
            }

            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                return CreateBitmapFrameDirect(mat);
            }

            return await Application.Current!.Dispatcher.InvokeAsync(() => CreateBitmapFrameDirect(mat));
        }

        /// <summary>
        /// 将 Mat 转换为可冻结的 WriteableBitmap（最高性能，安全代码）
        /// </summary>
        /// <param name="mat">源 Mat 对象</param>
        /// <returns>冻结的 WriteableBitmap</returns>
        public static WriteableBitmap? ToWriteableBitmap(this Mat mat)
        {
            if (mat.Empty())
            {
                return null;
            }

            return Application.Current?.Dispatcher.CheckAccess() == true
                ? CreateWriteableBitmapDirect(mat)
                : Application.Current?.Dispatcher.Invoke(() => CreateWriteableBitmapDirect(mat));
        }

        /// <summary>
        /// 异步将 Mat 转换为 WriteableBitmap
        /// </summary>
        /// <param name="mat">源 Mat 对象</param>
        /// <returns>WriteableBitmap 任务</returns>
        public static async Task<WriteableBitmap?> ToWriteableBitmapAsync(this Mat mat)
        {
            if (mat.Empty())
            {
                return null;
            }

            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                return CreateWriteableBitmapDirect(mat);
            }

            return await Application.Current!.Dispatcher.InvokeAsync(() => CreateWriteableBitmapDirect(mat));
        }

        /// <summary>
        /// 直接从 Mat 创建 BitmapFrame（无需内存流）
        /// </summary>
        /// <param name="mat">源 Mat 对象</param>
        /// <returns>BitmapFrame</returns>
        private static BitmapFrame CreateBitmapFrameDirect(Mat mat)
        {
            var pixelFormat = GetPixelFormat(mat);
            var stride = GetStride(mat, pixelFormat);

            // 创建字节数组来存储像素数据
            var pixelData = new byte[stride * mat.Height];

            // 直接复制 Mat 数据到字节数组
            CopyMatData(mat, pixelData, stride);

            // 使用 BitmapSource.Create 直接创建位图
            var bitmapSource = BitmapSource.Create(
                mat.Width,
                mat.Height,
                96, // DPI X
                96, // DPI Y
                pixelFormat,
                null, // 调色板
                pixelData,
                stride
            );

            // 冻结以提高性能并允许跨线程访问
            bitmapSource.Freeze();

            return BitmapFrame.Create(bitmapSource);
        }

        /// <summary>
        /// 直接从 Mat 创建 WriteableBitmap（最高性能选项，完全安全代码）
        /// </summary>
        /// <param name="mat">源 Mat 对象</param>
        /// <returns>WriteableBitmap</returns>
        private static WriteableBitmap CreateWriteableBitmapDirect(Mat mat)
        {
            var pixelFormat = GetPixelFormat(mat);

            // 创建 WriteableBitmap
            var writeableBitmap = new WriteableBitmap(
                mat.Width,
                mat.Height,
                96, // DPI X
                96, // DPI Y
                pixelFormat,
                null // 调色板
            );

            // 锁定位图进行写入
            writeableBitmap.Lock();
            try
            {
                var stride = GetStride(mat, pixelFormat);

                // 使用安全代码复制 Mat 数据到 WriteableBitmap 的后备缓冲区
                CopyMatDataToWriteableBitmap(mat, writeableBitmap, stride);

                // 标记整个区域为脏区域
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, mat.Width, mat.Height));
            }
            finally
            {
                writeableBitmap.Unlock();
            }

            // 冻结以提高性能
            writeableBitmap.Freeze();

            return writeableBitmap;
        }

        /// <summary>
        /// 获取对应的 WPF 像素格式
        /// </summary>
        /// <param name="mat">Mat 对象</param>
        /// <returns>WPF 像素格式</returns>
        private static PixelFormat GetPixelFormat(Mat mat)
        {
            var channels = mat.Channels();
            var depth = mat.Depth();

            return (channels, depth) switch
            {
                // 单通道图像
                (1, MatType.CV_8U) => PixelFormats.Gray8,
                (1, MatType.CV_16U) => PixelFormats.Gray16,

                // 三通道图像
                (3, MatType.CV_8U) => PixelFormats.Bgr24,

                // 四通道图像
                (4, MatType.CV_8U) => PixelFormats.Bgra32,

                _ => throw new NotSupportedException($"不支持的图像格式: {channels} 通道, 深度 {depth}")
            };
        }

        /// <summary>
        /// 计算步长
        /// </summary>
        /// <param name="mat">Mat 对象</param>
        /// <param name="pixelFormat">像素格式</param>
        /// <returns>步长</returns>
        private static int GetStride(Mat mat, PixelFormat pixelFormat)
        {
            return (mat.Width * pixelFormat.BitsPerPixel + 7) / 8;
        }

        /// <summary>
        /// 复制 Mat 数据到字节数组（安全代码）
        /// </summary>
        /// <param name="mat">源 Mat</param>
        /// <param name="pixelData">目标字节数组</param>
        /// <param name="stride">步长</param>
        private static void CopyMatData(Mat mat, byte[] pixelData, int stride)
        {
            if (mat.IsContinuous())
            {
                // 如果 Mat 数据是连续的，可以直接复制
                Marshal.Copy(mat.Data, pixelData, 0, pixelData.Length);
            }
            else
            {
                // 如果数据不连续，按行复制
                var matStride = (int)mat.Step();
                var copyStride = Math.Min(stride, matStride);

                for (int y = 0; y < mat.Height; y++)
                {
                    var srcPtr = mat.Data + y * matStride;
                    Marshal.Copy(srcPtr, pixelData, y * stride, copyStride);
                }
            }
        }

        /// <summary>
        /// 使用安全代码将 Mat 数据复制到 WriteableBitmap 后备缓冲区
        /// </summary>
        /// <param name="mat">源 Mat</param>
        /// <param name="writeableBitmap">目标 WriteableBitmap</param>
        /// <param name="stride">步长</param>
        private static void CopyMatDataToWriteableBitmap(Mat mat, WriteableBitmap writeableBitmap, int stride)
        {
            var matStride = (int)mat.Step();
            var bitmapStride = writeableBitmap.BackBufferStride;
            var copyStride = Math.Min(stride, Math.Min(matStride, bitmapStride));

            if (mat.IsContinuous() && stride == bitmapStride)
            {
                // 如果步长相同且数据连续，可以一次性复制整个图像
                var totalBytes = mat.Height * bitmapStride;
                var tempBuffer = new byte[totalBytes];
                Marshal.Copy(mat.Data, tempBuffer, 0, totalBytes);
                Marshal.Copy(tempBuffer, 0, writeableBitmap.BackBuffer, totalBytes);
            }
            else
            {
                // 按行复制数据
                var lineBuffer = new byte[copyStride];
                for (int y = 0; y < mat.Height; y++)
                {
                    // 从 Mat 复制一行数据到临时缓冲区
                    var srcPtr = mat.Data + y * matStride;
                    Marshal.Copy(srcPtr, lineBuffer, 0, copyStride);

                    // 从临时缓冲区复制数据到 WriteableBitmap
                    var dstPtr = writeableBitmap.BackBuffer + y * bitmapStride;
                    Marshal.Copy(lineBuffer, 0, dstPtr, copyStride);
                }
            }
        }

        /// <summary>
        /// 更新现有 WriteableBitmap 的内容（避免重新创建对象，安全代码）
        /// </summary>
        /// <param name="mat">源 Mat</param>
        /// <param name="writeableBitmap">要更新的 WriteableBitmap</param>
        /// <returns>是否更新成功</returns>
        public static bool UpdateWriteableBitmap(this Mat mat, WriteableBitmap writeableBitmap)
        {
            if (mat.Empty() || writeableBitmap == null)
                return false;

            // 检查尺寸是否匹配
            if (mat.Width != writeableBitmap.PixelWidth || mat.Height != writeableBitmap.PixelHeight)
                return false;

            var action = new Action(() =>
            {
                writeableBitmap.Lock();
                try
                {
                    var stride = GetStride(mat, writeableBitmap.Format);
                    CopyMatDataToWriteableBitmap(mat, writeableBitmap, stride);
                    writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, mat.Width, mat.Height));
                }
                finally
                {
                    writeableBitmap.Unlock();
                }
            });

            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                action();
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(action);
            }

            return true;
        }

        /// <summary>
        /// 高性能批量更新 WriteableBitmap（适用于实时视频流）
        /// </summary>
        /// <param name="mat">源 Mat</param>
        /// <param name="writeableBitmap">要更新的 WriteableBitmap</param>
        /// <param name="dirtyRect">需要更新的区域，null 表示整个图像</param>
        /// <returns>是否更新成功</returns>
        public static bool UpdateWriteableBitmapRegion(this Mat mat, WriteableBitmap writeableBitmap, Int32Rect? dirtyRect = null)
        {
            if (mat.Empty() || writeableBitmap == null)
                return false;

            if (mat.Width != writeableBitmap.PixelWidth || mat.Height != writeableBitmap.PixelHeight)
                return false;

            var updateRect = dirtyRect ?? new Int32Rect(0, 0, mat.Width, mat.Height);

            var action = new Action(() =>
            {
                writeableBitmap.Lock();
                try
                {
                    var stride = GetStride(mat, writeableBitmap.Format);
                    var matStride = (int)mat.Step();
                    var bitmapStride = writeableBitmap.BackBufferStride;
                    var bytesPerPixel = writeableBitmap.Format.BitsPerPixel / 8;

                    // 只更新指定区域
                    var lineBuffer = new byte[updateRect.Width * bytesPerPixel];
                    for (int y = updateRect.Y; y < updateRect.Y + updateRect.Height; y++)
                    {
                        var srcPtr = mat.Data + y * matStride + updateRect.X * bytesPerPixel;
                        var dstPtr = writeableBitmap.BackBuffer + y * bitmapStride + updateRect.X * bytesPerPixel;

                        Marshal.Copy(srcPtr, lineBuffer, 0, lineBuffer.Length);
                        Marshal.Copy(lineBuffer, 0, dstPtr, lineBuffer.Length);
                    }

                    writeableBitmap.AddDirtyRect(updateRect);
                }
                finally
                {
                    writeableBitmap.Unlock();
                }
            });

            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                action();
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(action);
            }

            return true;
        }
    }
}
