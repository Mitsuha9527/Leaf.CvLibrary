using CvCommon;
using CvLibrary.OpenCV;
using OpenCvSharp;

namespace CvLibraryExtensions.OpenCV
{
    public static class CvImageExtensions
    {
        public static Mat RotateCvImage(this CvImage cvImg, double rotation)
        {
            var mat = CvTool.ConvertToMat(cvImg);
            if (rotation != 0)
                mat = CvTool.RotateImage(mat, rotation);
            return mat;
        }

        public static Mat EnhanceCvImage(this CvImage cvImg, double factor)
        {
            var mat = CvTool.ConvertToMat(cvImg);
            if (factor > 0)
                mat = CvTool.MultiImage(mat, mat, factor);
            return mat;
        }
    }
}
