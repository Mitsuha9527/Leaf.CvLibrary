using OpenCvSharp;

namespace CvLibrary.Tests.Match;

/// <summary>
/// 合成测试图像生成器 - 生成用于模板匹配验证的图像文件。
/// 运行方式: dotnet run --project test/CvLibrary.Tests
/// </summary>
public static class SyntheticImageGenerator
{
    /// <summary>
    /// 生成所有测试图像到指定目录。
    /// </summary>
    public static void GenerateAll(string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        // 1. 模板图像（十字 + 不对称标记）
        using var template = CreateAsymmetricTemplate(100);
        Cv2.ImWrite(Path.Combine(outputDir, "template_100x100.png"), template);

        // 2. 纯平移测试 - 200×200 背景，模板在 (100, 120)
        using var translation = TestImageGenerator.CreateSearchImage(
            300, 300, template, 120, 150, angle: 0);
        Cv2.ImWrite(Path.Combine(outputDir, "test_translation.png"), translation);

        // 3. 旋转 10° 测试
        using var rotated = TestImageGenerator.CreateSearchImage(
            300, 300, template, 150, 150, angle: 10);
        Cv2.ImWrite(Path.Combine(outputDir, "test_rotation_10deg.png"), rotated);

        // 4. 多实例测试（3个目标）
        var instances = new List<InstanceGroundTruth>
        {
            new(60, 60, 0),
            new(200, 60, 0),
            new(130, 180, 0),
        };
        var (multiInstance, _) = TestImageGenerator.CreateMultiInstanceImage(
            300, 300, template, instances);
        Cv2.ImWrite(Path.Combine(outputDir, "test_multi_instance.png"), multiInstance);
        multiInstance.Dispose();

        // 5. 噪声测试（高斯噪声 sigma=10）
        using var noisy = TestImageGenerator.CreateSearchImage(
            300, 300, template, 150, 150, angle: 0);
        using var noisyResult = TestImageGenerator.AddGaussianNoise(noisy, 10);
        Cv2.ImWrite(Path.Combine(outputDir, "test_noisy.png"), noisyResult);

        // 6. 反转对比度
        using var normal = TestImageGenerator.CreateSearchImage(
            300, 300, template, 150, 150, angle: 0);
        using var inverted = TestImageGenerator.InvertBrightness(normal);
        Cv2.ImWrite(Path.Combine(outputDir, "test_inverted.png"), inverted);

        Console.WriteLine($"Test images generated in: {outputDir}");
        Console.WriteLine("  template_100x100.png - Template image");
        Console.WriteLine("  test_translation.png - Pure translation (template at 120,150)");
        Console.WriteLine("  test_rotation_10deg.png - 10 degree rotation");
        Console.WriteLine("  test_multi_instance.png - 3 instances");
        Console.WriteLine("  test_noisy.png - With Gaussian noise");
        Console.WriteLine("  test_inverted.png - Inverted contrast");
    }

    private static Mat CreateAsymmetricTemplate(int size)
    {
        var mat = new Mat(size, size, MatType.CV_8UC1, Scalar.Black);
        // 绘制具有旋转唯一性的 L 形图案
        Cv2.Rectangle(mat, new Rect(size/5, size/5, size*3/5, size/5), Scalar.White, -1);
        Cv2.Rectangle(mat, new Rect(size/5, size/5, size/5, size*3/5), Scalar.White, -1);
        // 不对称标记点
        Cv2.Circle(mat, new Point(size/4, size/4), 4, Scalar.Black, -1);
        Cv2.Circle(mat, new Point(size/3, size/2), 2, Scalar.Gray, -1);
        return mat;
    }
}
