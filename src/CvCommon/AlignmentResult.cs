using System;
using System.Collections.Generic;

namespace CvCommon
{
    /// <summary>
    /// Mark点对齐结果
    /// </summary>
    public class AlignmentResult
    {
        /// <summary>
        /// 对齐是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 仿射变换矩阵数据 (2x3 矩阵，以行优先顺序存储)
        /// [a, b, tx]
        /// [c, d, ty]
        /// 存储为: [a, b, tx, c, d, ty]
        /// </summary>
        public double[] TransformMatrixData { get; set; } = Array.Empty<double>();

        /// <summary>
        /// 检测到的Mark点位置
        /// </summary>
        public List<DetectedMark> DetectedMarks { get; set; } = new List<DetectedMark>();

        /// <summary>
        /// 旋转角度（度）
        /// </summary>
        public double RotationAngle { get; set; }

        /// <summary>
        /// 平移量 (X, Y)
        /// </summary>
        public CvPoint Translation { get; set; }

        /// <summary>
        /// 缩放比例
        /// </summary>
        public double ScaleFactor { get; set; } = 1.0;

        /// <summary>
        /// 对齐置信度 (0.0 - 1.0)
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 检测到的Mark点信息
    /// </summary>
    public class DetectedMark
    {
        /// <summary>
        /// 对应的Mark点ID
        /// </summary>
        public System.Guid MarkId { get; set; }

        /// <summary>
        /// Mark点名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 检测到的位置（实际坐标）
        /// </summary>
        public CvPoint DetectedPosition { get; set; }

        /// <summary>
        /// 参考位置（理论坐标）
        /// </summary>
        public CvPoint ReferencePosition { get; set; }

        /// <summary>
        /// 匹配得分 (0.0 - 1.0)
        /// </summary>
        public double MatchScore { get; set; }

        /// <summary>
        /// 位置偏差
        /// </summary>
        public CvPoint Deviation => new CvPoint(
            DetectedPosition.X - ReferencePosition.X,
            DetectedPosition.Y - ReferencePosition.Y
        );
    }
}
