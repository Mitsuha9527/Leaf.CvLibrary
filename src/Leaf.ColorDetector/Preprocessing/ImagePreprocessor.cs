using Leaf.ColorDetector.Models;
using OpenCvSharp;

namespace Leaf.ColorDetector.Preprocessing;

/// <summary>
/// 图像预处理工具。
/// <para>
/// 输出 Lab 色彩空间图像和有效像素掩码。
/// HSV 仅在内部用于阴影检测，高光检测改为联合 Lab 验证以避免误杀白色保险丝。
/// </para>
/// </summary>
internal static class ImagePreprocessor
{
    internal sealed class PreprocessDebugResult : IDisposable
    {
        public required Mat CroppedBgr { get; init; }
        public required Mat BlurredBgr { get; init; }
        public required Mat LabMat { get; init; }
        public required Mat HsvMat { get; init; }
        public required Mat ValidMask { get; init; }
        public required Rect CropRectInOriginal { get; init; }

        public void Dispose()
        {
            CroppedBgr.Dispose();
            BlurredBgr.Dispose();
            LabMat.Dispose();
            HsvMat.Dispose();
            ValidMask.Dispose();
        }
    }

    /// <summary>
    /// 对 ROI 图像执行预处理流程：中心裁剪 → 高斯模糊 → 转 Lab → 生成掩码。
    /// </summary>
    /// <param name="roiBgr">BGR 格式的 ROI 图像</param>
    /// <param name="options">检测配置</param>
    /// <returns>Lab 图像和有效像素掩码（调用方负责 Dispose）</returns>
    public static (Mat LabMat, Mat ValidMask) Preprocess(Mat roiBgr, ColorDetectorOptions options)
    {
        using var debug = PreprocessWithDebug(roiBgr, options);
        return (debug.LabMat.Clone(), debug.ValidMask.Clone());
    }

    internal static PreprocessDebugResult PreprocessWithDebug(Mat roiBgr, ColorDetectorOptions options)
    {
        var (cropped, cropRect) = CropCenter(roiBgr, options.CenterCropRatio);

        var blurred = new Mat();
        if (options.GaussianKernelSize >= 3)
        {
            var kSize = options.GaussianKernelSize | 1;
            Cv2.GaussianBlur(cropped, blurred, new Size(kSize, kSize), 0);
        }
        else
        {
            cropped.CopyTo(blurred);
        }

        var labMat = new Mat();
        Cv2.CvtColor(blurred, labMat, ColorConversionCodes.BGR2Lab);

        if (options.EnableLightnessNormalization)
            NormalizeLabLightness(labMat, options);

        var hsvMat = new Mat();
        Cv2.CvtColor(blurred, hsvMat, ColorConversionCodes.BGR2HSV);

        var validMask = BuildValidPixelMask(hsvMat, labMat, options);

        return new PreprocessDebugResult
        {
            CroppedBgr = cropped,
            BlurredBgr = blurred,
            LabMat = labMat,
            HsvMat = hsvMat,
            ValidMask = validMask,
            CropRectInOriginal = cropRect
        };
    }

    private static (Mat Cropped, Rect CropRect) CropCenter(Mat roiBgr, double centerCropRatio)
    {
        if (centerCropRatio is <= 0.0 or >= 1.0)
            return (roiBgr.Clone(), new Rect(0, 0, roiBgr.Width, roiBgr.Height));

        var cropW = (int)(roiBgr.Width * centerCropRatio);
        var cropH = (int)(roiBgr.Height * centerCropRatio);
        var offsetX = (roiBgr.Width - cropW) / 2;
        var offsetY = (roiBgr.Height - cropH) / 2;

        if (cropW <= 0 || cropH <= 0)
            return (roiBgr.Clone(), new Rect(0, 0, roiBgr.Width, roiBgr.Height));

        using var roiView = new Mat(roiBgr, new Rect(offsetX, offsetY, cropW, cropH));
        return (roiView.Clone(), new Rect(offsetX, offsetY, cropW, cropH));
    }

    /// <summary>
    /// 构建有效像素掩码：排除镜面高光反射和深度阴影区域。
    /// <para>
    /// 高光判定采用联合策略：HSV 候选高光 + Lab 验证。
    /// 真正的镜面反光在 Lab 中表现为 L*≈100 且 a*≈0, b*≈0（无色彩偏向），
    /// 而白色保险丝虽然 L* 高，但通常带有轻微色彩偏向（a* 或 b* 有偏移）。
    /// 这避免了白色/浅色保险丝被误判为高光的问题。
    /// </para>
    /// </summary>
    private static Mat BuildValidPixelMask(Mat hsvMat, Mat labMat, ColorDetectorOptions options)
    {
        var validMask = new Mat(hsvMat.Rows, hsvMat.Cols, MatType.CV_8UC1, Scalar.All(255));

        var hsvIndexer = hsvMat.GetGenericIndexer<Vec3b>();
        var labIndexer = labMat.GetGenericIndexer<Vec3b>();
        var maskIndexer = validMask.GetGenericIndexer<byte>();

        for (var y = 0; y < hsvMat.Rows; y++)
        {
            for (var x = 0; x < hsvMat.Cols; x++)
            {
                var hsv = hsvIndexer[y, x];
                var v = hsv.Item2;
                var s = hsv.Item1;

                // 阴影：V 极低 → 色彩信息不可靠
                if (v <= options.ShadowValueMax)
                {
                    maskIndexer[y, x] = 0;
                    continue;
                }

                // 死像素：V = 0
                if (v == 0)
                {
                    maskIndexer[y, x] = 0;
                    continue;
                }

                // 高光候选：HSV 空间 V 高 + S 低
                if (v >= options.HighlightValueMin && s <= options.HighlightSaturationMax)
                {
                    // Lab 验证：真正的镜面反光 a*≈0, b*≈0（中性色）
                    // 白色保险丝虽然 L* 高，但 a*, b* 通常有轻微偏移
                    var lab = labIndexer[y, x];
                    var a = lab.Item1 - 128.0; // 转标准范围
                    var b = lab.Item2 - 128.0;
                    var chroma = Math.Sqrt(a * a + b * b);

                    // 色度极低（< 5）= 真正的无色高光 → 排除
                    // 色度较高 = 白色/浅色保险丝 → 保留
                    if (chroma < 5.0)
                    {
                        maskIndexer[y, x] = 0;
                    }
                }
            }
        }

        // 形态学开运算去小噪点
        if (options.MorphologyKernelSize >= 2)
        {
            using var kernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new Size(options.MorphologyKernelSize, options.MorphologyKernelSize));
            Cv2.MorphologyEx(validMask, validMask, MorphTypes.Open, kernel);
        }

        return validMask;
    }

    /// <summary>
    /// 对 Lab 图像执行亮度归一化：将 L* 均值平移到目标亮度附近，限制最大平移量。
    /// 仅调整 L* 通道，不改变 a*b* 色度结构。
    /// </summary>
    private static void NormalizeLabLightness(Mat labMat, ColorDetectorOptions options)
    {
        using var lChannel = new Mat();
        Cv2.ExtractChannel(labMat, lChannel, 0);

        var currentL8 = Cv2.Mean(lChannel).Val0;
        var targetL8 = Math.Clamp(options.TargetLStar, 0, 100) * 255.0 / 100.0;
        var maxShift8 = Math.Abs(options.MaxLightnessShift) * 255.0 / 100.0;

        var shift = targetL8 - currentL8;
        shift = Math.Clamp(shift, -maxShift8, maxShift8);

        if (Math.Abs(shift) < 0.5)
            return;

        using var shifted = new Mat();
        lChannel.ConvertTo(shifted, MatType.CV_8UC1, 1.0, shift);
        Cv2.InsertChannel(shifted, labMat, 0);
    }
}
