using CvCommon;
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
        public static Mat Smooth(Mat mat, int ksize = 5)
        {
            Mat smoothed = new Mat();
            Cv2.GaussianBlur(mat, smoothed, new Size(ksize, ksize), 0);
            return smoothed;
        }


        public static CvRegion Threshold(Mat mat, double threshold, double maxVable = 256)
        {
            Mat thresholded = new Mat();
            Cv2.Threshold(mat, thresholded, threshold, maxVable, ThresholdTypes.Binary);
            return new CvRegion(thresholded);
        }

        public static CvRegion ConnectionComponents(CvRegion region)
        {
            // 识别连通组件
            var labels = new Mat();
            using var stats = new Mat();
            using var centroids = new Mat();
            using var mat = region.Mask.Clone();
            if (region.Mask.Type() != MatType.CV_8UC1 || region.Mask.Type() != MatType.CV_8SC1)
                mat.ConvertTo(mat, MatType.CV_8UC1);
            var numLabels = Cv2.ConnectedComponentsWithStats(mat, labels, stats, centroids);
            List<CvRegionProperties> regionPropertys = [];

            // 4. 遍历所有区域（跳过背景）
            for (int i = 1; i < numLabels; i++)
            {
                // 获取区域参数
                int left = stats.At<int>(i, 0); // x坐标
                int top = stats.At<int>(i, 1); // y坐标
                int width = stats.At<int>(i, 2); // 宽度
                int height = stats.At<int>(i, 3); // 高度
                int area = stats.At<int>(i, 4); // 面积
                CvPoint centroid = new CvPoint(
                    centroids.At<float>(i, 0),
                    centroids.At<float>(i, 1)
                );
                var regionProperties = new CvRegionProperties(
                    i,
                    left,
                    top,
                    width,
                    height,
                    area,
                    centroid
                );
                regionPropertys.Add(regionProperties);
            }
            var connectedRegion = new CvRegion(labels, regionPropertys);
            return connectedRegion;
        }


        public static CvRegion ClosingCircle(CvRegion region, double radius)
        {
            double actualRadius = Math.Round(radius);
            int kernelSize = 2 * (int)actualRadius + 1;

            // 生成结构元素
            using Mat kernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new Size(kernelSize, kernelSize),
                new Point(actualRadius, actualRadius) // 确保中心对称
            );

            // 执行闭运算
            Mat closed = new Mat();
            Cv2.MorphologyEx(
                region.Mask,
                closed,
                MorphTypes.Close,
                kernel,
                iterations: 1,
                borderType: BorderTypes.Reflect101
            ); // 边界模式

            return new CvRegion(closed);
        }

        public static CvRegion OpeningCircle(CvRegion region, double radius)
        {
            double actualRadius = Math.Round(radius);
            int kernelSize = 2 * (int)actualRadius + 1;

            // 生成结构元素
            using Mat kernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new Size(kernelSize, kernelSize),
                new Point(actualRadius, actualRadius) // 确保中心对称
            );

            // 执行开运算
            Mat open = new Mat();
            Cv2.MorphologyEx(
                region.Mask,
                open,
                MorphTypes.Open,
                kernel,
                iterations: 1,
                borderType: BorderTypes.Reflect101
            ); // 边界模式
            return new CvRegion(open);
        }

        public static CvRegion Erode(CvRegion region, MorphShapes shape, Size size)
        {
            using Mat kernel = Cv2.GetStructuringElement(shape, size);
            Mat eroded = new Mat();
            Cv2.Erode(region.Mask, eroded, kernel);
            return new CvRegion(eroded);
        }

        public static CvRegion Dilate(CvRegion region, MorphShapes shape, Size size)
        {
            using Mat kernel = Cv2.GetStructuringElement(shape, size);
            Mat dilated = new Mat();
            Cv2.Dilate(region.Mask, dilated, kernel);
            return new CvRegion(dilated);
        }

        public static CvRegion FillUpRegion(CvRegion region)
        {
            // 克隆原始掩码
            Mat filled = region.Mask.Clone();
            // 用FloodFill处理可能的大孔洞
            Cv2.FloodFill(filled, new Point(0, 0), Scalar.White);
            // 如果需要保持原始轮廓并只填充内部孔洞，
            Cv2.BitwiseOr(region.Mask, filled, filled);
            return new CvRegion(filled);
        }

        public static CvRegion[] FillUpRegions(IEnumerable<CvRegion> regions)
        {
            return regions.Select(region => FillUpRegion(region)).ToArray();
        }
    }
}
