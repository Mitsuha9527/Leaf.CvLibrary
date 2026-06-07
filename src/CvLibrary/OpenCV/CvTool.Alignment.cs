using CvCommon;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CvLibrary.OpenCV
{
    public static partial class CvTool
    {
        #region 对齐变换计算

        /// <summary>
        /// 根据参考点集和检测点集计算对齐变换矩阵。
        /// </summary>
        /// <param name="referencePoints">参考点集（模板/理论坐标）。</param>
        /// <param name="detectedPoints">检测点集（实际坐标）。</param>
        /// <param name="type">变换类型，默认 Similarity。</param>
        /// <returns>对齐结果。</returns>
        public static AlignmentResult CalculateAlignment(
            IReadOnlyList<CvPoint> referencePoints,
            IReadOnlyList<CvPoint> detectedPoints,
            AlignmentTransformType type = AlignmentTransformType.Similarity)
        {
            int minPoints = type switch
            {
                AlignmentTransformType.Similarity => 2,
                AlignmentTransformType.Affine => 3,
                AlignmentTransformType.Perspective => 4,
                _ => 2,
            };

            if (referencePoints == null || detectedPoints == null)
                return AlignmentResult.Failed("Reference or detected points are null.");

            if (referencePoints.Count < minPoints || detectedPoints.Count < minPoints)
                return AlignmentResult.Failed(
                    $"需要至少 {minPoints} 个点对进行 {type} 变换，当前参考点 {referencePoints.Count}，检测点 {detectedPoints.Count}。");

            if (referencePoints.Count != detectedPoints.Count)
                return AlignmentResult.Failed(
                    $"参考点数量 ({referencePoints.Count}) 与检测点数量 ({detectedPoints.Count}) 不一致。");

            try
            {
                var srcPts = referencePoints
                    .Select(p => new Point2f((float)p.X, (float)p.Y))
                    .ToArray();
                var dstPts = detectedPoints
                    .Select(p => new Point2f((float)p.X, (float)p.Y))
                    .ToArray();

                Mat? resultMatrix = null;
                double[] matrixData;

                switch (type)
                {
                    case AlignmentTransformType.Similarity:
                        resultMatrix = Cv2.EstimateAffinePartial2D(
                            InputArray.Create(srcPts), InputArray.Create(dstPts));
                        if (resultMatrix == null || resultMatrix.Empty())
                            return AlignmentResult.Failed("无法计算相似变换矩阵。");
                        matrixData = To3x3(resultMatrix);
                        break;

                    case AlignmentTransformType.Affine:
                        resultMatrix = Cv2.EstimateAffine2D(
                            InputArray.Create(srcPts), InputArray.Create(dstPts));
                        if (resultMatrix == null || resultMatrix.Empty())
                            return AlignmentResult.Failed("无法计算仿射变换矩阵。");
                        matrixData = To3x3(resultMatrix);
                        break;

                    case AlignmentTransformType.Perspective:
                        resultMatrix = Cv2.FindHomography(
                            InputArray.Create(srcPts), InputArray.Create(dstPts),
                            HomographyMethods.Ransac);
                        if (resultMatrix == null || resultMatrix.Empty())
                            return AlignmentResult.Failed("无法计算透视变换矩阵。");
                        matrixData = Extract3x3(resultMatrix);
                        break;

                    default:
                        return AlignmentResult.Failed($"不支持的变换类型: {type}");
                }

                // 计算置信度（最小二乘残差）
                double confidence = CalculateConfidence(referencePoints, detectedPoints,
                    matrixData, type);

                return new AlignmentResult
                {
                    Success = true,
                    TransformMatrix = matrixData,
                    TransformType = type,
                    Confidence = confidence,
                };
            }
            catch (Exception ex)
            {
                return AlignmentResult.Failed($"计算对齐变换失败: {ex.Message}");
            }
        }

        #endregion

        #region 变换应用

        /// <summary>
        /// 使用 3×3 变换矩阵变换矩形区域，返回轴对齐包围盒。
        /// </summary>
        /// <param name="rect">待变换矩形。</param>
        /// <param name="transformMatrix">3×3 齐次变换矩阵（9 元素，行优先）。</param>
        /// <returns>变换后的轴对齐包围盒。</returns>
        public static CvRect TransformRect(CvRect rect, double[] transformMatrix)
        {
            if (transformMatrix == null || transformMatrix.Length != 9)
                return rect;

            var corners = new Point2f[]
            {
                new((float)rect.Left,     (float)rect.Top),
                new((float)rect.Right,    (float)rect.Top),
                new((float)rect.Right,    (float)rect.Bottom),
                new((float)rect.Left,     (float)rect.Bottom),
            };

            var transformed = corners.Select(c => ApplyTransform(c, transformMatrix)).ToArray();

            double minX = transformed.Min(p => p.X);
            double minY = transformed.Min(p => p.Y);
            double maxX = transformed.Max(p => p.X);
            double maxY = transformed.Max(p => p.Y);

            return new CvRect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 使用 3×3 变换矩阵变换点。
        /// </summary>
        /// <param name="point">待变换点。</param>
        /// <param name="transformMatrix">3×3 齐次变换矩阵（9 元素，行优先）。</param>
        /// <returns>变换后的点。</returns>
        public static CvPoint TransformPoint(CvPoint point, double[] transformMatrix)
        {
            if (transformMatrix == null || transformMatrix.Length != 9)
                return point;

            var pt = new Point2f((float)point.X, (float)point.Y);
            var result = ApplyTransform(pt, transformMatrix);
            return new CvPoint(result.X, result.Y);
        }

        /// <summary>
        /// 应用 3×3 齐次变换矩阵到 Point2f。
        /// </summary>
        private static Point2f ApplyTransform(Point2f p, double[] m)
        {
            // m[0..8] = 3×3 row-major
            //      [m0 m1 m2]   [x]
            // P' = [m3 m4 m5] × [y]
            //      [m6 m7 m8]   [1]
            double w = m[6] * p.X + m[7] * p.Y + m[8];
            if (Math.Abs(w) < 1e-10) w = 1.0;

            double x = (m[0] * p.X + m[1] * p.Y + m[2]) / w;
            double y = (m[3] * p.X + m[4] * p.Y + m[5]) / w;

            return new Point2f((float)x, (float)y);
        }

        #endregion

        #region 辅助计算

        /// <summary>
        /// 计算两点之间的欧氏距离。
        /// </summary>
        private static double CalculateDistance(CvPoint p1, CvPoint p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 将 OpenCV 的 2×3 矩阵转换为 3×3 齐次矩阵。
        /// </summary>
        private static double[] To3x3(Mat matrix2x3)
        {
            var m = new double[9];
            // 行 0
            m[0] = matrix2x3.At<double>(0, 0);
            m[1] = matrix2x3.At<double>(0, 1);
            m[2] = matrix2x3.At<double>(0, 2);
            // 行 1
            m[3] = matrix2x3.At<double>(1, 0);
            m[4] = matrix2x3.At<double>(1, 1);
            m[5] = matrix2x3.At<double>(1, 2);
            // 行 2 — 补齐
            m[6] = 0;
            m[7] = 0;
            m[8] = 1;

            matrix2x3.Dispose();
            return m;
        }

        /// <summary>
        /// 从 OpenCV 3×3 矩阵提取数据。
        /// </summary>
        private static double[] Extract3x3(Mat matrix3x3)
        {
            var m = new double[9];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    m[i * 3 + j] = matrix3x3.At<double>(i, j);

            matrix3x3.Dispose();
            return m;
        }

        /// <summary>
        /// 从最小二乘拟合残差计算置信度。
        /// </summary>
        private static double CalculateConfidence(
            IReadOnlyList<CvPoint> refPts,
            IReadOnlyList<CvPoint> detPts,
            double[] matrix,
            AlignmentTransformType type)
        {
            if (refPts.Count == 0) return 0;

            double totalError = 0;
            for (int i = 0; i < refPts.Count; i++)
            {
                var predicted = ApplyTransform(
                    new Point2f((float)refPts[i].X, (float)refPts[i].Y), matrix);
                double dx = predicted.X - detPts[i].X;
                double dy = predicted.Y - detPts[i].Y;
                totalError += Math.Sqrt(dx * dx + dy * dy);
            }

            double avgError = totalError / refPts.Count;
            return Math.Exp(-avgError / 2.0);  // 映射到 (0, 1]
        }

        #endregion
    }
}
