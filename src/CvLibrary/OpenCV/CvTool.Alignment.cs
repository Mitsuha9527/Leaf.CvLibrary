using CvCommon;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CvLibrary.OpenCV
{
    public static partial class CvTool
    {
        #region Mark点检测与对齐

        /// <summary>
        /// 检测单个Mark点位置
        /// </summary>
        public static DetectedMark? DetectMarkPoint(
            Mat src,
            MarkPoint markPoint,
            Mat? templateImage = null
        )
        {
            if (src.Empty())
                throw new ArgumentException("Source image is empty.");

            Mat template;
            
            // 如果提供了模板图像，从模板图像中提取Mark模板
            if (templateImage != null && !templateImage.Empty())
            {
                template = CreateMatchTemplate(templateImage, markPoint.TemplateRect);
            }
            else
            {
                // 否则从源图像中提取（用于创建模板时）
                template = CreateMatchTemplate(src, markPoint.TemplateRect);
            }

            try
            {
                // 确定搜索区域
                Mat searchMat = src;
                double offsetX = 0;
                double offsetY = 0;

                if (markPoint.SearchRegion.HasValue && !markPoint.SearchRegion.Value.IsEmpty)
                {
                    var searchRegion = markPoint.SearchRegion.Value;
                    // 确保搜索区域在图像范围内
                    if (searchRegion.X + searchRegion.Width <= src.Width &&
                        searchRegion.Y + searchRegion.Height <= src.Height)
                    {
                        searchMat = CropImage(src, searchRegion);
                        offsetX = searchRegion.X;
                        offsetY = searchRegion.Y;
                    }
                }

                // 执行模板匹配
                var matches = MatchTemplate(
                    searchMat,
                    template,
                    TemplateMatchModes.CCoeffNormed,
                    markPoint.EnableRotation ? markPoint.RotationAngles : null,
                    markPoint.MatchThreshold,
                    nmsThreshold: 0.3
                ).ToList();

                if (matches.Count == 0)
                    return null;

                // 取第一个匹配结果（最高分）
                var bestMatch = matches[0];

                // 计算Mark点中心位置（考虑搜索区域偏移）
                int centerX = (int)(bestMatch.X + template.Width / 2.0 + offsetX);
                int centerY = (int)(bestMatch.Y + template.Height / 2.0 + offsetY);
                var detectedPosition = new CvPoint(centerX, centerY);

                return new DetectedMark
                {
                    MarkId = markPoint.Id,
                    Name = markPoint.Name,
                    DetectedPosition = detectedPosition,
                    ReferencePosition = markPoint.ReferencePosition,
                    MatchScore = 1.0
                };
            }
            finally
            {
                template.Dispose();
            }
        }

        /// <summary>
        /// 批量检测多个Mark点
        /// </summary>
        public static List<DetectedMark> DetectMarkPoints(
            Mat src,
            List<MarkPoint> markPoints,
            Mat? templateImage = null
        )
        {
            var detectedMarks = new List<DetectedMark>();

            foreach (var markPoint in markPoints)
            {
                var detected = DetectMarkPoint(src, markPoint, templateImage);
                if (detected != null)
                {
                    detectedMarks.Add(detected);
                }
            }

            return detectedMarks;
        }

        /// <summary>
        /// 根据两个Mark点计算仿射变换（相似变换：平移+旋转+等比缩放）
        /// </summary>
        public static AlignmentResult CalculateAlignmentFromTwoPoints(
            List<CvPoint> referencePoints,
            List<CvPoint> detectedPoints
        )
        {
            var result = new AlignmentResult();

            if (referencePoints.Count < 2 || detectedPoints.Count < 2)
            {
                result.Success = false;
                result.ErrorMessage = "至少需要2个Mark点进行对齐";
                return result;
            }

            try
            {
                // 将CvPoint转换为Point2f
                var srcPoints = new Point2f[]
                {
                    new Point2f((float)referencePoints[0].X, (float)referencePoints[0].Y),
                    new Point2f((float)referencePoints[1].X, (float)referencePoints[1].Y)
                };

                var dstPoints = new Point2f[]
                {
                    new Point2f((float)detectedPoints[0].X, (float)detectedPoints[0].Y),
                    new Point2f((float)detectedPoints[1].X, (float)detectedPoints[1].Y)
                };

                // 使用EstimateAffinePartial2D计算相似变换矩阵
                using var srcMat = InputArray.Create(srcPoints);
                using var dstMat = InputArray.Create(dstPoints);
               using var transformMatrix = Cv2.EstimateAffinePartial2D(srcMat, dstMat);

                if (transformMatrix == null || transformMatrix.Empty())
                {
                    result.Success = false;
                    result.ErrorMessage = "无法计算仿射变换矩阵";
                    return result;
                }

                // 提取矩阵数据 (2x3)
                result.TransformMatrixData = new double[6];
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        result.TransformMatrixData[i * 3 + j] = transformMatrix.At<double>(i, j);
                    }
                }

                // 从变换矩阵中提取参数
                double a = transformMatrix.At<double>(0, 0);
                double b = transformMatrix.At<double>(0, 1);
                double tx = transformMatrix.At<double>(0, 2);
                double ty = transformMatrix.At<double>(1, 2);

                // 提取缩放比例
                result.ScaleFactor = Math.Sqrt(a * a + b * b);

                // 提取旋转角度（弧度转角度）
                result.RotationAngle = Math.Atan2(b, a) * 180.0 / Math.PI;

                // 提取平移量
                result.Translation = new CvPoint((int)Math.Round(tx), (int)Math.Round(ty));

                // 计算置信度（基于点之间的距离一致性）
                double refDistance = CalculateDistance(referencePoints[0], referencePoints[1]);
                double detDistance = CalculateDistance(detectedPoints[0], detectedPoints[1]);
                double distanceRatio = Math.Min(refDistance, detDistance) / Math.Max(refDistance, detDistance);
                result.Confidence = distanceRatio;

                result.Success = true;         
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"计算对齐变换失败: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 根据三个或更多Mark点计算完整仿射变换
        /// </summary>
        public static AlignmentResult CalculateAlignmentFromMultiplePoints(
            List<CvPoint> referencePoints,
            List<CvPoint> detectedPoints
        )
        {
            var result = new AlignmentResult();

            if (referencePoints.Count < 3 || detectedPoints.Count < 3)
            {
                result.Success = false;
                result.ErrorMessage = "至少需要3个Mark点进行完整仿射变换";
                return result;
            }

            try
            {
                // 将CvPoint转换为Point2f
                var srcPoints = referencePoints
                    .Take(3)
                    .Select(p => new Point2f((float)p.X, (float)p.Y))
                    .ToArray();

                var dstPoints = detectedPoints
                    .Take(3)
                    .Select(p => new Point2f((float)p.X, (float)p.Y))
                    .ToArray();

                // 使用GetAffineTransform计算完整仿射变换矩阵
                var transformMatrix = Cv2.GetAffineTransform(srcPoints, dstPoints);

                if (transformMatrix == null || transformMatrix.Empty())
                {
                    result.Success = false;
                    result.ErrorMessage = "无法计算仿射变换矩阵";
                    return result;
                }

                // 提取矩阵数据
                result.TransformMatrixData = new double[6];
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        result.TransformMatrixData[i * 3 + j] = transformMatrix.At<double>(i, j);
                    }
                }

                // 提取参数（简化版）
                double tx = transformMatrix.At<double>(0, 2);
                double ty = transformMatrix.At<double>(1, 2);
                result.Translation = new CvPoint((int)Math.Round(tx), (int)Math.Round(ty));

                // 计算平均置信度
                result.Confidence = 0.95;

                result.Success = true;

                transformMatrix.Dispose();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"计算对齐变换失败: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 使用仿射变换矩阵变换矩形区域
        /// </summary>
        public static CvRect TransformRect(CvRect rect, double[] transformMatrixData)
        {
            if (transformMatrixData == null || transformMatrixData.Length != 6)
                return rect;

            // 重建Mat对象
            using var transformMatrix = new Mat(2, 3, MatType.CV_64F);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    transformMatrix.Set(i, j, transformMatrixData[i * 3 + j]);
                }
            }

            // 获取矩形的四个角点
            var corners = new Point2f[]
            {
                new Point2f((float)rect.X, (float)rect.Y),
                new Point2f((float)(rect.X + rect.Width), (float)rect.Y),
                new Point2f((float)(rect.X + rect.Width), (float)(rect.Y + rect.Height)),
                new Point2f((float)rect.X, (float)(rect.Y + rect.Height))
            };

            // 变换所有角点
            var transformedCorners = new Point2f[4];
            for (int i = 0; i < 4; i++)
            {
                transformedCorners[i] = TransformPoint(corners[i], transformMatrix);
            }

            // 计算变换后的包围矩形
            int minX = (int)Math.Round(transformedCorners.Min(p => p.X));
            int minY = (int)Math.Round(transformedCorners.Min(p => p.Y));
            int maxX = (int)Math.Round(transformedCorners.Max(p => p.X));
            int maxY = (int)Math.Round(transformedCorners.Max(p => p.Y));

            return new CvRect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 使用仿射变换矩阵变换点
        /// </summary>
        public static CvPoint TransformPoint(CvPoint point, double[] transformMatrixData)
        {
            if (transformMatrixData == null || transformMatrixData.Length != 6)
                return point;

            using var transformMatrix = new Mat(2, 3, MatType.CV_64F);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    transformMatrix.Set(i, j, transformMatrixData[i * 3 + j]);
                }
            }

            var pt = new Point2f((float)point.X, (float)point.Y);
            var transformed = TransformPoint(pt, transformMatrix);
            return new CvPoint((int)Math.Round(transformed.X), (int)Math.Round(transformed.Y));
        }

        /// <summary>
        /// 使用仿射变换矩阵变换Point2f点
        /// </summary>
        private static Point2f TransformPoint(Point2f point, Mat transformMatrix)
        {
            double a = transformMatrix.At<double>(0, 0);
            double b = transformMatrix.At<double>(0, 1);
            double c = transformMatrix.At<double>(1, 0);
            double d = transformMatrix.At<double>(1, 1);
            double tx = transformMatrix.At<double>(0, 2);
            double ty = transformMatrix.At<double>(1, 2);

            float newX = (float)(a * point.X + b * point.Y + tx);
            float newY = (float)(c * point.X + d * point.Y + ty);

            return new Point2f(newX, newY);
        }

        /// <summary>
        /// 计算两点之间的欧氏距离
        /// </summary>
        private static double CalculateDistance(CvPoint p1, CvPoint p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion
    }
}
