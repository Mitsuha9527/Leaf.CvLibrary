using Leaf.ColorDetector.ColorScience;
using Leaf.ColorDetector.Models;
using OpenCvSharp;

namespace Leaf.ColorDetector.Preprocessing;

/// <summary>
/// 图像预处理工具。
/// <para>
/// 输出 Lab 色彩空间图像和有效像素掩码。
/// HSV 仅在内部用于低亮度不可靠像素剔除，灰黑色系（消色）像素通过
/// <see cref="AchromaticFilter.IsAchromatic"/> 豁免，不参与低亮度过滤。
/// </para>
/// </summary>
public static class ImagePreprocessor
{
    /// <summary>
    /// 对 ROI 图像执行预处理流程：中心裁剪 → 高斯模糊 → 转 Lab → 生成掩码。
    /// </summary>
    /// <param name="roiBgr">BGR 格式的 ROI 图像</param>
    /// <param name="options">检测配置</param>
    /// <returns>Lab 图像和有效像素掩码（调用方负责 Dispose）</returns>
    public static (Mat LabMat, Mat ValidMask) Preprocess(Mat roiBgr, ColorDetectorOptions options)
    {
        var (cropped, _) = CropCenter(roiBgr, options.CenterCropRatio);

        Mat blurred;
        if (options.GaussianKernelSize >= 3)
        {
            var kSize = options.GaussianKernelSize | 1;
            blurred = new Mat();
            Cv2.GaussianBlur(cropped, blurred, new Size(kSize, kSize), 0);
        }
        else
        {
            blurred = cropped.Clone();
        }

        var labMat = new Mat();
        Cv2.CvtColor(blurred, labMat, ColorConversionCodes.BGR2Lab);

        if (options.EnableLightnessNormalization)
            NormalizeLabLightness(labMat, options);

        using var hsvMat = new Mat();
        Cv2.CvtColor(blurred, hsvMat, ColorConversionCodes.BGR2HSV);

        var validMask = BuildValidPixelMask(hsvMat, labMat, options);

        // cropped 只在模糊前有用，用完释放
        cropped.Dispose();
        blurred.Dispose();

        return (labMat, validMask);
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
    /// 构建有效像素掩码：排除非消色的低亮度不可靠像素。
    /// <para>
    /// 灰黑色系（消色）像素通过 <see cref="AchromaticFilter.IsAchromatic"/> 检测后直接豁免。
    /// 非消色像素若 V 低于 <see cref="ColorDetectorOptions.MinValidLightnessV"/> 则视为色彩信息不可靠而排除。
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
                // 消色豁免：灰黑色系像素始终保留
                var lab = labIndexer[y, x];
                var a = lab.Item1 - 128.0;
                var b = lab.Item2 - 128.0;
                if (AchromaticFilter.IsAchromatic(a, b, options))
                    continue;

                var hsv = hsvIndexer[y, x];
                var v = hsv.Item2;

                // 低亮度：V 极低 → 非消色像素色彩信息不可靠
                if (v <= options.MinValidLightnessV)
                {
                    maskIndexer[y, x] = 0;
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
