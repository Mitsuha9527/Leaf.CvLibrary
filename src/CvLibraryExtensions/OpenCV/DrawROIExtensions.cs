using System.Runtime.CompilerServices;
using CvCommon;
using CvLibrary.OpenCV;
using OpenCvSharp;

namespace CvLibraryExtensions.OpenCV
{
    public static class DrawROIExtensions
    {
        public static void DrawResultROI(this Mat mat, CvRect roi, bool isOK, int thickness = 2)
        {         
            CvTool.DrawRectangle(mat, roi, isOK ? Scalar.Lime : Scalar.Red, thickness);
            return ;
        }
    }
}
