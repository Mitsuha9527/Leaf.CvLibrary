using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CvLibrary.OpenCV
{
    public static partial class CvTool
    {
        public static Point2f[][] GetSubPixelContours(CvRegion region)
        {
            // 1. 首先找到二值图像的轮廓
            Point[][] contours;
            HierarchyIndex[] hierarchyIndices;
            Cv2.FindContours(
                region.Mask,
                out contours,
                out hierarchyIndices,
                RetrievalModes.External, // 只检测外轮廓
                ContourApproximationModes.ApproxSimple
            );

            // 转换为亚像素精度的轮廓数组
            Point2f[][] subPixelContours = new Point2f[contours.Length][];

            // 亚像素精度优化参数
            var criteria = new TermCriteria(
                CriteriaTypes.Eps | CriteriaTypes.MaxIter,
                30, // 最大迭代次数
                0.01 // 精度要求
            );

            // 为每条轮廓进行亚像素精度优化
            for (int i = 0; i < contours.Length; i++)
            {
                // 将轮廓点转换为Point2f
                Point2f[] contourPoints = Array.ConvertAll(contours[i], p => new Point2f(p.X, p.Y));

                if (contourPoints.Length > 5) // 确保有足够的点进行处理
                {
                    // 使用CornerSubPix优化轮廓点位置
                    Cv2.CornerSubPix(
                        region.Mask,
                        contourPoints,
                        new Size(5, 5), // 搜索窗口大小
                        new Size(-1, -1), // 死区大小
                        criteria
                    );
                }

                subPixelContours[i] = contourPoints;
            }

            return subPixelContours;
        }

        public static RotatedRect FitEllipse(Point2f[] contour)
        {
            if (contour.Length < 5)
                return new RotatedRect();
            // 拟合椭圆
            RotatedRect ellipse = Cv2.FitEllipseAMS(contour);
            return ellipse;
        }

        public static RotatedRect FitEllipse(CvRegion region)
        {
            Point2f[][] contours = GetSubPixelContours(region);
            if (contours.Length == 0)
                return new RotatedRect();
            return FitEllipse(contours[0]);
        }
    }
}
