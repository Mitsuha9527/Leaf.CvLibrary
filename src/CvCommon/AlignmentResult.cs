using System;

namespace CvCommon
{
    /// <summary>
    /// 对齐变换结果。
    /// </summary>
    public class AlignmentResult
    {
        /// <summary>
        /// 对齐是否成功。
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 失败时的错误消息。
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 3×3 齐次变换矩阵（行优先存储，共 9 个元素）。
        /// 对于 Similarity / Affine（2×3）：补齐第三行 [0, 0, 1]。
        /// 对于 Perspective（3×3）：直接存储。
        /// </summary>
        public double[] TransformMatrix { get; set; } = new double[9];

        /// <summary>
        /// 变换类型。
        /// </summary>
        public AlignmentTransformType TransformType { get; set; }

        /// <summary>
        /// 旋转角度（度）。仅 Similarity 类型有值，其余返回 null。
        /// </summary>
        public double? RotationAngle
        {
            get
            {
                if (TransformType != AlignmentTransformType.Similarity)
                    return null;

                double a = TransformMatrix[0];
                double b = TransformMatrix[1];
                return Math.Atan2(b, a) * 180.0 / Math.PI;
            }
        }

        /// <summary>
        /// 平移量。仅 Similarity / Affine 类型有值，Perspective 返回 null。
        /// </summary>
        public CvPoint? Translation
        {
            get
            {
                if (TransformType == AlignmentTransformType.Perspective)
                    return null;

                return new CvPoint(TransformMatrix[2], TransformMatrix[5]);
            }
        }

        /// <summary>
        /// 缩放比例。仅 Similarity 类型有值，其余返回 null。
        /// </summary>
        public double? ScaleFactor
        {
            get
            {
                if (TransformType != AlignmentTransformType.Similarity)
                    return null;

                double a = TransformMatrix[0];
                double b = TransformMatrix[1];
                return Math.Sqrt(a * a + b * b);
            }
        }

        /// <summary>
        /// 对齐置信度（0.0 ~ 1.0）。由最小二乘拟合残差计算得到。
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 创建一个失败结果。
        /// </summary>
        public static AlignmentResult Failed(string message)
        {
            return new AlignmentResult
            {
                Success = false,
                ErrorMessage = message,
            };
        }
    }
}
