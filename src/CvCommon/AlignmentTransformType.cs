namespace CvCommon
{
    /// <summary>
    /// 对齐变换类型。
    /// </summary>
    public enum AlignmentTransformType
    {
        /// <summary>
        /// 相似变换（4 DOF: 平移×2 + 旋转 + 等比缩放）。最少 2 个点对。
        /// </summary>
        Similarity = 0,

        /// <summary>
        /// 仿射变换（6 DOF: 平移×2 + 旋转 + 非等比缩放 + 切变）。最少 3 个点对。
        /// </summary>
        Affine = 1,

        /// <summary>
        /// 透视变换（8 DOF）。最少 4 个点对。
        /// </summary>
        Perspective = 2,
    }
}
