using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CvCommon;
using OpenCvSharp;

namespace CvLibrary.OpenCV
{
    public static partial class CvTool
    {
        public static void DrawEllipse(
            Mat mat,
            RotatedRect ellipse,
            Scalar color,
            int thickness = 1
        )
        {
            Cv2.Ellipse(mat, ellipse, color, thickness);

            // 绘制椭圆中心
            Point center = new Point((int)ellipse.Center.X, (int)ellipse.Center.Y);
            Cv2.DrawMarker(mat, center, color, MarkerTypes.Cross, 10, thickness);
        }

        public static void DrawRectangle(Mat mat, CvRect rect, Scalar color, int thinckness = 1)
        {
            Cv2.Rectangle(
                mat,
                new Rect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height),
                color,
                thinckness
            );
        }

        public static void DrawRectangle(Mat mat, Rect rect, Scalar color, int thickness = 1)
        {
            Cv2.Rectangle(mat, rect, color, thickness);
        }

        public static void DrawRectangle(
            Mat mat,
            Point topLeft,
            int width,
            int height,
            Scalar color,
            int thickness = 1
        )
        {
            Cv2.Rectangle(mat, new Rect(topLeft, new Size(width, height)), color, thickness);
        }

        public static void DrawText(
            Mat mat,
            string text,
            Point position,
            Scalar color,
            double fontScale = 1.0,
            int thickness = 1
        )
        {
            if (string.IsNullOrEmpty(text))
                return;
            Cv2.PutText(mat, text, position, HersheyFonts.Italic, fontScale, color, thickness);
        }

        public static Mat ColorMap(Mat mat, ColormapTypes types = ColormapTypes.Jet)
        {
            Mat colorMap = new Mat();
            if (mat.Type() != MatType.CV_8UC1)
                mat.ConvertTo(colorMap, MatType.CV_8UC1, 255.0 / 4096);
            Cv2.ApplyColorMap(colorMap, colorMap, types);
            return colorMap;
        }
    }
}
