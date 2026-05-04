namespace Leaf.ColorDetector.Models;

/// <summary>
/// 颜色检测器的配置参数。
/// <para>
/// 操作员通常不需要修改这些参数，只需调整每种颜色定义中的
/// <see cref="FuseColorDefinition.MaxDeltaE"/>（容差）即可。
/// 此处的参数仅供调试和特殊环境适配。
/// </para>
/// </summary>
public class ColorDetectorOptions
{
    // ===== 图像预处理参数 =====

    /// <summary>
    /// 高斯模糊核大小（必须为奇数）。用于抑制噪声。
    /// 设为 0 或 1 则跳过模糊。
    /// </summary>
    public int GaussianKernelSize { get; set; } = 5;

    /// <summary>
    /// 中心采样区域比例 (0.0-1.0)。
    /// 仅使用 ROI 中心部分区域做颜色分析，以避免边缘噪声。
    /// 1.0 表示使用整个 ROI。
    /// </summary>
    public double CenterCropRatio { get; set; } = 0.7;

    /// <summary>
    /// 形态学开运算核大小。用于去除掩码中的小噪点。
    /// 设为 0 则跳过形态学处理。
    /// </summary>
    public int MorphologyKernelSize { get; set; } = 3;

    /// <summary>
    /// 是否启用 L* 亮度归一化补偿，用于抑制光照波动带来的偏差。
    /// </summary>
    public bool EnableLightnessNormalization { get; set; } = true;

    /// <summary>
    /// 亮度归一化目标值（L*，0~100）。
    /// </summary>
    public double TargetLStar { get; set; } = 55;

    /// <summary>
    /// 单次归一化允许的最大亮度偏移（L*）。
    /// </summary>
    public double MaxLightnessShift { get; set; } = 12;

    // ===== 像素掩码参数（HSV 仅用于此处，不参与颜色判定） =====

    /// <summary>
    /// 高光判定：明度上限。V &gt; 此值的像素可能是镜面反光。
    /// </summary>
    public int HighlightValueMin { get; set; } = 240;

    /// <summary>
    /// 高光判定：饱和度上限。V &gt; HighlightValueMin 且 S &lt; 此值的像素判定为高光。
    /// </summary>
    public int HighlightSaturationMax { get; set; } = 40;

    /// <summary>
    /// 阴影判定：明度下限。V &lt; 此值的像素判定为过暗/阴影。
    /// </summary>
    public int ShadowValueMax { get; set; } = 30;

    // ===== 质量门控参数 =====

    /// <summary>
    /// 最低有效像素数量。
    /// 有效像素少于此值时，结果标记为 <see cref="DetectQuality.Insufficient"/>。
    /// </summary>
    public int MinValidPixelCount { get; set; } = 50;

    /// <summary>
    /// 有效像素最低比例 (0.0-1.0)。
    /// 低于此值时，结果质量降级为 <see cref="DetectQuality.LowPixelRatio"/>。
    /// </summary>
    public double MinValidPixelRatio { get; set; } = 0.25;

    /// <summary>
    /// 第1名与第2名得分最小差距。
    /// 差距小于此值时，结果标记为 <see cref="DetectQuality.Ambiguous"/>。
    /// </summary>
    public double MinConfidenceGap { get; set; } = 0.05;

    /// <summary>
    /// 高斯评分宽度因子（sigma = MaxDeltaE / 此值）。值越大，评分衰减越快。
    /// </summary>
    public double ScoreSigmaFactor { get; set; } = 2.5;

    /// <summary>
    /// 自适应椭球容差在最终评分中的权重 (0~1)。
    /// 0 = 只看 ΔE2000；1 = 只看椭球距离。
    /// </summary>
    public double AdaptiveToleranceWeight { get; set; } = 0.35;

    /// <summary>
    /// 颜色内部散布度惩罚系数。值越大，对不均匀区域降分越强。
    /// </summary>
    public double DispersionPenaltyStrength { get; set; } = 0.06;

    /// <summary>
    /// 空间不一致时的评分惩罚系数 (0~1)。
    /// </summary>
    public double SpatialPenaltyFactor { get; set; } = 0.7;
}
