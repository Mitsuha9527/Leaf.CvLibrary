using CvCommon;
using CvLibrary.OpenCV;

namespace CvLibrary.Tests.Alignment
{
    public class AlignmentTests
    {
        [Fact]
        public void CalculateAlignment_Similarity_TwoPoints_ShouldSucceed()
        {
            var refPts = new List<CvPoint>
            {
                new(0, 0),
                new(100, 0),
            };
            var detPts = new List<CvPoint>
            {
                new(10, 20),
                new(110, 20),
            };

            var result = CvTool.CalculateAlignment(refPts, detPts,
                AlignmentTransformType.Similarity);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(9, result.TransformMatrix.Length);
            Assert.True(result.Confidence > 0.9,
                $"Expected confidence > 0.9, got {result.Confidence}");
            Assert.NotNull(result.RotationAngle);
            Assert.NotNull(result.ScaleFactor);
            Assert.NotNull(result.Translation);
        }

        [Fact]
        public void CalculateAlignment_Affine_ThreePoints_ShouldSucceed()
        {
            var refPts = new List<CvPoint>
            {
                new(0, 0),
                new(100, 0),
                new(0, 100),
            };
            var detPts = new List<CvPoint>
            {
                new(5, 10),
                new(105, 12),
                new(4, 108),
            };

            var result = CvTool.CalculateAlignment(refPts, detPts,
                AlignmentTransformType.Affine);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(9, result.TransformMatrix.Length);
            Assert.True(result.Confidence > 0.9);
            Assert.Null(result.RotationAngle);   // Affine: no simple rotation
            Assert.Null(result.ScaleFactor);      // Affine: no simple scale
            Assert.NotNull(result.Translation);   // Affine: has translation
        }

        [Fact]
        public void CalculateAlignment_Perspective_FourPoints_ShouldSucceed()
        {
            var refPts = new List<CvPoint>
            {
                new(0, 0),
                new(100, 0),
                new(100, 100),
                new(0, 100),
            };
            var detPts = new List<CvPoint>
            {
                new(5, 8),
                new(108, 3),
                new(110, 105),
                new(3, 98),
            };

            var result = CvTool.CalculateAlignment(refPts, detPts,
                AlignmentTransformType.Perspective);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(9, result.TransformMatrix.Length);
            Assert.Null(result.RotationAngle);
            Assert.Null(result.Translation);   // Perspective: no simple translation
        }

        [Fact]
        public void CalculateAlignment_InsufficientPoints_ShouldFail()
        {
            var refPts = new List<CvPoint> { new(0, 0) };
            var detPts = new List<CvPoint> { new(5, 10) };

            var result = CvTool.CalculateAlignment(refPts, detPts,
                AlignmentTransformType.Affine);

            Assert.False(result.Success);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public void CalculateAlignment_MismatchedCounts_ShouldFail()
        {
            var refPts = new List<CvPoint> { new(0, 0), new(100, 0) };
            var detPts = new List<CvPoint> { new(5, 10) };

            var result = CvTool.CalculateAlignment(refPts, detPts,
                AlignmentTransformType.Similarity);

            Assert.False(result.Success);
        }

        [Fact]
        public void TransformPoint_Identity_ShouldReturnSamePoint()
        {
            var pt = new CvPoint(42, 73);
            var identity = new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };

            var result = CvTool.TransformPoint(pt, identity);

            Assert.True(Math.Abs(result.X - pt.X) < 0.01);
            Assert.True(Math.Abs(result.Y - pt.Y) < 0.01);
        }

        [Fact]
        public void TransformRect_Translation_ShouldOffsetCorrectly()
        {
            var rect = new CvRect(10, 20, 100, 200);
            var translate = new double[] { 1, 0, 50, 0, 1, 30, 0, 0, 1 };

            var result = CvTool.TransformRect(rect, translate);

            Assert.True(Math.Abs(result.X - 60) < 0.01);
            Assert.True(Math.Abs(result.Y - 50) < 0.01);
            Assert.True(Math.Abs(result.Width - 100) < 0.01);
            Assert.True(Math.Abs(result.Height - 200) < 0.01);
        }

        [Fact]
        public void TransformPoint_NullMatrix_ShouldReturnSamePoint()
        {
            var pt = new CvPoint(10, 20);
            var result = CvTool.TransformPoint(pt, null!);
            Assert.Equal(pt.X, result.X);
            Assert.Equal(pt.Y, result.Y);
        }

        [Fact]
        public void TransformRect_NullMatrix_ShouldReturnSameRect()
        {
            var rect = new CvRect(0, 0, 100, 100);
            var result = CvTool.TransformRect(rect, null!);
            Assert.Equal(rect.X, result.X);
            Assert.Equal(rect.Y, result.Y);
        }
    }
}
