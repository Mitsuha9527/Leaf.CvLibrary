namespace CvLibrary.OpenCV.Match
{
    /// <summary>
    /// NCC 模板匹配的对比度极性模式。
    /// </summary>
    public enum MatchMetric
    {
        /// <summary>
        /// 极性敏感——模板的亮暗关系必须和搜索图一致。
        /// NCC 原始值映射到 [0, 1]。
        /// </summary>
        UsePolarity = 0,

        /// <summary>
        /// 忽略全局极性反转——亮暗反转的目标也能匹配。
        /// 取 |NCC| 后映射到 [0, 1]，分数下限 0.5。
        /// </summary>
        IgnoreGlobalPolarity = 1,
    }
}
