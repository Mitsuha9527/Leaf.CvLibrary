namespace CvCommon
{
    /// <summary>
    /// Mark点配置信息
    /// </summary>
    public class MarkPoint
    {
        /// <summary>
        /// Mark点唯一标识
        /// </summary>
        public System.Guid Id { get; set; } = System.Guid.NewGuid();

        /// <summary>
        /// Mark点名称 (例如: "Mark1", "Mark2")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Mark点模板图像区域（用于模板匹配）
        /// </summary>
        public CvRect TemplateRect { get; set; }

        /// <summary>
        /// Mark点在标准模板中的参考位置（理论坐标）
        /// </summary>
        public CvPoint ReferencePosition { get; set; }

        /// <summary>
        /// 模板匹配阈值 (0.0 - 1.0)
        /// </summary>
        public double MatchThreshold { get; set; } = 0.8;

        /// <summary>
        /// 是否启用旋转匹配
        /// </summary>
        public bool EnableRotation { get; set; } = false;

        /// <summary>
        /// 旋转角度数组（如果启用旋转匹配）
        /// </summary>
        public double[] RotationAngles { get; set; } = System.Array.Empty<double>();

        /// <summary>
        /// Mark点搜索范围（相对于参考位置的偏移范围）
        /// 如果为空，则在整个图像中搜索
        /// </summary>
        public CvRect? SearchRegion { get; set; }
    }
}
