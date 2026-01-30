using CvCommon;

namespace CvLibrary.OpenCV
{
    public class CvRegionProperties
    {
        public int Label { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Area { get; set; }
        public CvPoint Centroid { get; set; }

        public CvRegionProperties() { }

        public CvRegionProperties(
            int label,
            int left,
            int top,
            int width,
            int height,
            int area,
            CvPoint centroid
        )
        {
            Label = label;
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            Area = area;
            Centroid = centroid;
        }
    }
}
