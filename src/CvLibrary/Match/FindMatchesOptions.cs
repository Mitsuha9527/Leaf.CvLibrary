using CvCommon;

namespace CvLibrary.OpenCV.Match
{
    /// <summary>
    /// 模板匹配查找参数。每次调用 FindMatches 可以不同。
    /// </summary>
    public class FindMatchesOptions
    {
        /// <summary>
        /// 最低匹配分数（0.0 ~ 1.0）。默认 0.7。
        /// </summary>
        public double MinScore { get; init; } = 0.7;

        /// <summary>
        /// 最多返回的匹配实例数。默认 1。
        /// </summary>
        public int MaxMatches { get; init; } = 1;

        /// <summary>
        /// 匹配实例间允许的最大重叠比例（0.0 ~ 1.0）。默认 0.5。
        /// 用于 NMS 抑制和金字塔层间合并。
        /// </summary>
        public double MaxOverlap { get; init; } = 0.5;

        /// <summary>
        /// 查找角度起始（度）。null = 使用模型创建时的角度范围。
        /// 指定的范围必须 ≤ 模型的角度范围，超出部分自动 clamp。
        /// </summary>
        public double? AngleStart { get; init; }

        /// <summary>
        /// 查找角度范围（度）。null = 使用模型创建时的角度范围。
        /// </summary>
        public double? AngleExtent { get; init; }

        /// <summary>
        /// 是否启用亚像素精炼。默认 true。
        /// </summary>
        public bool SubPixelRefinement { get; init; } = true;

        /// <summary>
        /// 搜索区域（源图坐标）。null = 全图搜索。
        /// </summary>
        public CvRect? SearchRegion { get; init; }
    }
}
