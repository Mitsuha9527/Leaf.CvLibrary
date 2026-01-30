using System;

namespace CvCommon
{
    public class CvImage
    {
        public byte[] ImageData { get; }

        public uint Width { get; }

        public uint Height { get; }

        public CvPixelFormat CvPixelFormat { get; }

        public CvImage(byte[] imageData, uint width, uint height, CvPixelFormat cvPixelFormat)
        {
            ImageData = imageData;
            Width = width;
            Height = height;
            CvPixelFormat = cvPixelFormat;
        }
    }
}
