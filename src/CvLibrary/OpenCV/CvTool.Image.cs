using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CvCommon;
using OpenCvSharp;

namespace CvLibrary.OpenCV
{
    public static partial class CvTool
    {
        #region 图片处理

        public static Mat ReadImage(string path)
        {
            Mat mat = Cv2.ImRead(path, ImreadModes.AnyColor|ImreadModes.AnyDepth);
            return mat;
        }

        public static bool WriteImage(string path, Mat mat, double scale = 1)
        {
            if (scale != 1)
            {
                mat = ResizeImage(mat, scale);
            }
            return Cv2.ImWrite(path, mat);
        }

        public static Mat ResizeImage(Mat mat, double scaleFactor)
        {
            if (scaleFactor == 1.0)
            {
                return mat.Clone();
            }

            Mat resized = new Mat();
            Cv2.Resize(
                mat,
                resized,
                new Size(0, 0),
                scaleFactor,
                scaleFactor,
                InterpolationFlags.Linear
            );
            return resized;
        }

        public static Mat ResizeImage(Mat mat, CvSize size)
        {
            Mat resized = new Mat();
            Cv2.Resize(
                mat,
                resized,
                new Size(size.Width, size.Height),
                0,
                0,
                InterpolationFlags.Linear
            );
            return resized;
        }

        public static Mat ConvertToMat(CvImage image)
        {
            var channels = image.CvPixelFormat switch
            {
                CvPixelFormat.Mono8 => 1,
                CvPixelFormat.Mono16 => 1,
                CvPixelFormat.BGR8 => 3,
                _ => throw new NotSupportedException("Unsupported pixel format"),
            };
            return ConvertToMat(image.ImageData, (int)image.Width, (int)image.Height, channels);
        }

        public static Mat ConvertToMat(byte[] data, int width, int height, int channels)
        {
            // 简单校验防止除零错误
            if (width * height == 0)
                return new Mat();

            // 根据数据总长度反推每个通道的字节数 (1=8bit, 2=16bit)
            // 8-bit: length == w * h * channels * 1
            // 16-bit: length == w * h * channels * 2
            int bytesPerPixel = data.Length / (width * height);
            int bytesPerChannel = bytesPerPixel / channels;

            var matType = (channels, bytesPerChannel) switch
            {
                // 8-bit per channel
                (1, 1) => MatType.CV_8UC1,
                (3, 1) => MatType.CV_8UC3,
                (4, 1) => MatType.CV_8UC4,

                // 16-bit per channel
                (1, 2) => MatType.CV_16UC1,
                (3, 2) => MatType.CV_16UC3,
                (4, 2) => MatType.CV_16UC4,

                _ => throw new NotSupportedException(
                    $"Unsupported format: {channels} channels with {bytesPerChannel} bytes per channel"
                ),
            };

            Mat mat = Mat.FromPixelData(height, width, matType, data);
            return mat;
        }

        public static byte[] ConvertToByteArray(Mat mat)
        {
            return mat.ToBytes();
        }

        public static ushort[] ConvertToShortArrary(Mat mat)
        {
            mat.GetArray(out ushort[] data);

            return data;
        }

        public static Mat ConvertToGray(Mat mat)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }

        public static Mat CropImage(Mat mat, double x, double y, double width, double height)
        {
            if (x < 0 || y < 0 || x + width > mat.Width || y + height > mat.Height)
                return new Mat();
            Rect rect = new Rect((int)x, (int)y, (int)width, (int)height);
            return new Mat(mat, rect);
        }

        public static Mat CropImage(Mat mat, CvRect rect)
        {
            if (
                rect.X < 0
                || rect.Y < 0
                || rect.X + rect.Width > mat.Width
                || rect.Y + rect.Height > mat.Height
            )
                return new Mat();
            Rect rect1 = new Rect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            return new Mat(mat, rect1);
        }

        public static Mat RotateImage(Mat mat, double angle)
        {
            Mat rotated = new();
            if (angle == 90)
            {
                Cv2.Rotate(mat, rotated, RotateFlags.Rotate90Clockwise);
            }
            else if (angle == -90 || angle == 270)
            {
                Cv2.Rotate(mat, rotated, RotateFlags.Rotate90Counterclockwise);
            }
            else if (angle == 180 || angle == -180)
            {
                Cv2.Rotate(mat, rotated, RotateFlags.Rotate180);
            }
            else if (angle == 0)
            {
                return mat.Clone();
            }
            else
            {
                Point2f center = new(mat.Width / 2f, mat.Height / 2f);
                Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);

                // Calculate the new bounding box size to prevent cropping
                double cos = Math.Abs(rotationMatrix.At<double>(0, 0));
                double sin = Math.Abs(rotationMatrix.At<double>(0, 1));
                int newWidth = (int)(mat.Height * sin + mat.Width * cos);
                int newHeight = (int)(mat.Height * cos + mat.Width * sin);

                // Adjust the rotation matrix translation to center the image in the new bounding box
                rotationMatrix.Set(
                    0,
                    2,
                    rotationMatrix.At<double>(0, 2) + newWidth / 2.0 - center.X
                );
                rotationMatrix.Set(
                    1,
                    2,
                    rotationMatrix.At<double>(1, 2) + newHeight / 2.0 - center.Y
                );

                Cv2.WarpAffine(mat, rotated, rotationMatrix, new Size(newWidth, newHeight));
            }
            return rotated;
        }

        public static Mat MultiImage(Mat mat1, Mat mat2, double factor = 0.005)
        {
            var multiMat = new Mat();
            Cv2.Multiply(mat1, mat2, multiMat, factor);
            return multiMat;
        }

        public static Mat Emphasize(Mat mat, int kernel = 7, double factor = 1.0)
        {
            using Mat meanMat = mat.MedianBlur(kernel);
            using Mat subMat = mat.Subtract(meanMat);
            Mat emphasizeMat = subMat.Multiply(factor).Add(mat);
            return emphasizeMat;
        }

        public static Mat ConcatenateImages(Mat mat1, Mat mat2, bool vertical = false)
        {
            Mat result = new Mat();
            if (vertical)
            {
                // 垂直拼接(上下)
                Cv2.VConcat([mat1, mat2], result);
            }
            else
            {
                // 水平拼接(左右)
                Cv2.HConcat([mat1, mat2], result);
            }
            return result;
        }

        public static Mat RotateAndConcatenate(Mat mat, bool? vertical = null)
        {
            // 旋转180度
            using Mat rotated = RotateImage(mat, 180);

            // 如果未指定拼接方向,根据图像长宽比自动判断
            bool isVertical = vertical ?? DetermineOptimalConcatenation(mat);

            // 拼接原图和旋转后的图像
            Mat result = ConcatenateImages(mat, rotated, isVertical);

            return result;
        }

        private static bool DetermineOptimalConcatenation(Mat mat)
        {
            // 计算长宽比
            double aspectRatio = (double)mat.Width / mat.Height;

            // 如果图像宽度大于高度(横向图像),使用垂直拼接
            // 如果图像高度大于宽度(纵向图像),使用水平拼接
            // 这样可以使拼接后的图像更接近正方形,更加紧凑
            return aspectRatio > 1.0;
        }

        #endregion
    }
}
