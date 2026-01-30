using CvCommon;
using CvLibrary.OpenCV;
using OpenCvSharp;

namespace CvLibraryExtensions.OpenCV
{
    public static class CvImageExtensions
    {
        public static Mat Rotate2DCvImage(this CvImage cvImg, double rotation)
        {
            var mat = CvTool.ConvertToMat(cvImg);
            if (rotation != 0)
                mat = CvTool.RotateImage(mat, rotation);
            return mat;
        }
    }
}
