namespace CvLibrary.OpenCV.Match
{
    /// <summary>
    /// NCC 模板匹配模型的创建参数。
    /// </summary>
    public class NccModelOptions
    {
        /// <summary>
        /// 金字塔层数。null = 自动计算（推荐）。
        /// 自动规则：log2(minSide / 16)，并保证最顶层 ≥ 12px（旋转膨胀后）。
        /// </summary>
        public int? NumLevels { get; init; }

        /// <summary>
        /// 起始角度（度）。默认 0。
        /// </summary>
        public double AngleStart { get; init; } = 0;

        /// <summary>
        /// 角度范围（度）。默认 0（仅搜索起始角度）。
        /// 例如 AngleStart=-30, AngleExtent=60 覆盖 -30° ~ 30°。
        /// </summary>
        public double AngleExtent { get; init; } = 0;

        /// <summary>
        /// L0 层的期望角度步长（度）。null = 自动（默认 1°）。
        /// 上层自动翻倍：L1 步长 = L0×2, L2 步长 = L0×4, ...
        /// </summary>
        public double? AngleStep { get; init; }

        /// <summary>
        /// 对比度极性模式。
        /// </summary>
        public MatchMetric Metric { get; init; } = MatchMetric.UsePolarity;

        /// <summary>
        /// 高斯平滑核大小。null = 不平滑。必须为奇数且 ≥ 3。
        /// 例如 3 或 5。
        /// </summary>
        public int? SmoothKernelSize { get; init; }
    }
}
