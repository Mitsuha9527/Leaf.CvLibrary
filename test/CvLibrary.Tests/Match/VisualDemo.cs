using CvCommon;
using CvLibrary.OpenCV;
using CvLibrary.OpenCV.Match;
using OpenCvSharp;

namespace CvLibrary.Tests.Match;

/// <summary>
/// 可视化演示：对每张合成测试图运行模板匹配，保存标注结果图并用 OpenCV 窗口展示。
/// 运行: dotnet test --filter "Demo_AllTestImages" -v n
/// 结果保存在: test/TestData/Synthetic/_results/
/// </summary>
public class VisualDemo
{
    private static readonly Scalar MatchColor = new(0, 255, 0);
    private static readonly Scalar TemplateColor = new(255, 0, 0);
    private static readonly Scalar TextColor = new(0, 255, 255);

    [Fact]
    public void Demo_AllTestImages()
    {
        var syntheticDir = FindOrCreateSyntheticDir();

        using var template = new Mat(Path.Combine(syntheticDir, "template_100x100.png"),
            ImreadModes.Grayscale);

        var options = new NccModelOptions { AngleExtent = 0, NumLevels = 1 };
        using var model = CvNccModel.Create(template, options);

        var testCases = new (string Filename, string Title, CvPoint? Expected)[]
        {
            ("test_translation.png",    "1_Pure_Translation",        new CvPoint(120, 150)),
            ("test_rotation_10deg.png", "2_Rotation_10deg",          null),
            ("test_multi_instance.png", "3_Multi_Instance",          null),
            ("test_noisy.png",          "4_Gaussian_Noise",          new CvPoint(150, 150)),
            ("test_inverted.png",       "5_Inverted_Brightness",     new CvPoint(150, 150)),
        };

        var outputDir = Path.Combine(syntheticDir, "_results");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine($"=== NCC Template Matching Visual Demo ===");
        Console.WriteLine($"Results saved to: {outputDir}");
        Console.WriteLine();

        foreach (var (filename, title, expectedPos) in testCases)
        {
            var imagePath = Path.Combine(syntheticDir, filename);
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"SKIP: {filename} not found");
                continue;
            }

            using var searchImage = new Mat(imagePath, ImreadModes.Grayscale);
            using var displayImage = new Mat();
            Cv2.CvtColor(searchImage, displayImage, ColorConversionCodes.GRAY2BGR);

            var results = model.FindMatches(searchImage, new FindMatchesOptions
            {
                MinScore = 0.4,
                MaxMatches = 5,
            });

            Console.WriteLine($"--- {title} ---");
            Console.WriteLine($"  Found: {results.Count} match(es)");

            foreach (var r in results)
            {
                Console.WriteLine($"  Pos=({r.Position.X:F1}, {r.Position.Y:F1})  "
                    + $"Angle={r.Angle:F1}  Score={r.Score:F3}");

                int cx = (int)Math.Round(r.Position.X);
                int cy = (int)Math.Round(r.Position.Y);
                int halfW = template.Width / 2;
                int halfH = template.Height / 2;

                var rect = new Rect(cx - halfW, cy - halfH, template.Width, template.Height);
                Cv2.Rectangle(displayImage, rect, TemplateColor, 2);
                Cv2.DrawMarker(displayImage, new Point(cx, cy), MatchColor, MarkerTypes.Cross, 20, 2);

                string label = $"({r.Position.X:F1},{r.Position.Y:F1}) score={r.Score:F3}";
                Cv2.PutText(displayImage, label,
                    new Point(cx - halfW, Math.Max(cy - halfH - 5, 10)),
                    HersheyFonts.HersheySimplex, 0.4, TextColor, 1);
            }

            if (expectedPos.HasValue)
            {
                var ep = expectedPos.Value;
                Cv2.DrawMarker(displayImage,
                    new Point((int)ep.X, (int)ep.Y),
                    new Scalar(255, 255, 0), MarkerTypes.Star, 15, 2);
                Cv2.PutText(displayImage, "Expected",
                    new Point((int)ep.X + 10, (int)ep.Y - 10),
                    HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 0), 1);
            }

            // 保存结果图
            string outputPath = Path.Combine(outputDir, $"{title}_result.png");
            Cv2.ImWrite(outputPath, displayImage);
            Console.WriteLine($"  -> saved: {outputPath}");

            // 显示窗口（2 秒自动关闭）
            Cv2.NamedWindow(title, WindowFlags.Normal);
            Cv2.ResizeWindow(title, 500, 500);
            Cv2.ImShow(title, displayImage);
            Cv2.WaitKey(2000);
            Cv2.DestroyWindow(title);
        }

        Cv2.DestroyAllWindows();
        Console.WriteLine();
        Console.WriteLine($"Done! All result images in: {outputDir}");
    }

    private static string FindOrCreateSyntheticDir()
    {
        // Check next to test assembly
        var dir = Path.Combine(
            Path.GetDirectoryName(typeof(VisualDemo).Assembly.Location)!, "TestImages");
        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "template_100x100.png")))
            return dir;

        // Check test data dir
        dir = Path.Combine(
            Path.GetDirectoryName(typeof(VisualDemo).Assembly.Location)!,
            "..", "..", "..", "..", "..", "test", "TestData", "Synthetic");
        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "template_100x100.png")))
            return dir;

        // Generate
        dir = Path.Combine(Path.GetTempPath(), "CvLibrary_Demo");
        SyntheticImageGenerator.GenerateAll(dir);
        return dir;
    }
}
