using CvCommon;

namespace CvLibrary.OpenCV.Match
{
    /// <summary>
    /// 模板匹配结果，存储匹配位置、角度和分数。
    /// Shape 匹配的扩展字段在当前版本返回 null。
    /// </summary>
    public class CvMatchResult
    {
        /// <summary>
        /// 模板中心在搜索图中的亚像素坐标。
        /// </summary>
        public CvPoint Position { get; init; }

        /// <summary>
        /// 匹配角度（度）。
        /// </summary>
        public double Angle { get; init; }

        /// <summary>
        /// 匹配分数（0.0 ~ 1.0）。
        /// </summary>
        public double Score { get; init; }

        // === Shape 匹配扩展（NCC 返回 null） ===

        /// <summary>
        /// 缩放比例（当前 NCC 匹配返回 null）。
        /// </summary>
        public double? Scale { get; init; }

        /// <summary>
        /// 杂乱度评分（当前 NCC 匹配返回 null）。
        /// </summary>
        public double? Clutter { get; init; }

        /// <summary>
        /// 模板对比度（当前 NCC 匹配返回 null）。
        /// </summary>
        public double? Contrast { get; init; }
    }
}
