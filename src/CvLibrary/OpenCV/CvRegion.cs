using CvCommon;
using OpenCvSharp;


namespace CvLibrary.OpenCV
{
    public class CvRegion : IDisposable
    {
        public Mat Mask { get; set; }

        public List<CvRegionProperties> RegionProperties { get; set; } = [];

        public CvRegion(Mat mask, params IEnumerable<CvRegionProperties> regions)
        {
            Mask = mask;

            if (regions?.Count() > 0)
            {
                RegionProperties.AddRange(regions);
            }
            else
            {
                CreateDefaultRegionProperty();
            }
        }

        public void Dispose()
        {
            Mask.Dispose();
            RegionProperties.Clear();
            GC.SuppressFinalize(this);
        }

        private void CreateDefaultRegionProperty()
        {
            this.RegionProperties.Add(
                new()
                {
                    Area = this.Mask.CountNonZero(),
                    Centroid = new CvPoint(this.Mask.Width / 2, this.Mask.Height / 2),
                    Height = this.Mask.Height,
                    Label = 1,
                    Left = 0,
                    Top = 0,
                    Width = this.Mask.Width,
                }
            );
        }
    }
}
