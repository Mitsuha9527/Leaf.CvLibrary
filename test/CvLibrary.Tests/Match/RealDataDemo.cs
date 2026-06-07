using CvCommon;
using CvLibrary.OpenCV.Match;
using OpenCvSharp;

namespace CvLibrary.Tests.Match;

/// <summary>
/// 真实数据集可视化演示 — 仅 AnglePCB 数据集。
/// 带旋转矩形标注，显示实测位置和角度。
/// </summary>
public class RealDataDemo
{
    private static readonly Scalar DetectedColor = new(0, 255, 0);   // 绿色：检测结果

    [Fact]
    public void Demo_AnglePCB_WithRotation()
    {
        var baseDir = FindAnglePcbDir();
        if (baseDir == null)
        {
            Console.WriteLine("ERROR: AnglePCB dataset not found.");
            return;
        }

        var pcbDir = Path.Combine(baseDir, "PCBR01");
        var templatePath = Path.Combine(pcbDir, "R_T01.png");

        Console.WriteLine($"=== AnglePCB Demo: PCBR01 (Rotation Model) ===");
        Console.WriteLine($"Template: R_T01.png");
        Console.WriteLine();

        using var templateMat = new Mat(templatePath, ImreadModes.Color);
        Console.WriteLine($"Template: {templateMat.Width}x{templateMat.Height}");

        // 带旋转的 NCC 模型（角度步长 0.1°, ±2° 范围）
        var modelOpts = new NccModelOptions
        {
            AngleStart = -2.0,
            AngleExtent = 4.0,    // -2° ~ 2°
            AngleStep = 0.1,       // 0.1° 步长
            NumLevels = 1,
        };
        using var model = CvNccModel.Create(templateMat, modelOpts);

        // 测试所有角度文件
        var allFiles = Directory.GetFiles(pcbDir, "PCBR01_501_*.png")
            .OrderBy(f => f)
            .ToList();

        var outputDir = Path.Combine(
            Path.GetDirectoryName(typeof(RealDataDemo).Assembly.Location)!,
            "RealDataResults");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine($"Testing {allFiles.Count} images with angles 0.0 ~ 1.8 deg...");
        Console.WriteLine();

        foreach (var testPath in allFiles)
        {
            string filename = Path.GetFileName(testPath);
            // 从文件名提取真实角度: PCBR01_501_0.600000.png → 0.6°
            double trueAngle = ExtractAngle(filename);

            using var searchMat = new Mat(testPath, ImreadModes.Color);
            using var display = searchMat.Clone();

            var results = model.FindMatches(searchMat, new FindMatchesOptions
            {
                MinScore = 0.5,
                MaxMatches = 1,
                SubPixelRefinement = true,
            });

            Console.Write($"  {trueAngle:F1} deg  ");

            if (results.Count > 0)
            {
                var r = results[0];
                double angleErr = r.Angle - trueAngle;
                string errSign = angleErr >= 0 ? "+" : "";
                Console.WriteLine($"Detected: ({r.Position.X:F0},{r.Position.Y:F0})  "
                    + $"Angle={r.Angle:F2} deg  Score={r.Score:F4}  Err={errSign}{angleErr:F2} deg");

                // 绘制旋转矩形
                DrawRotatedRect(display, r.Position, r.Angle,
                    templateMat.Width, templateMat.Height);

                // 绘制中心十字
                int cx = (int)Math.Round(r.Position.X);
                int cy = (int)Math.Round(r.Position.Y);
                int crossLen = 10;
                Cv2.Line(display, new Point(cx - crossLen, cy), new Point(cx + crossLen, cy),
                    DetectedColor, 1, LineTypes.AntiAlias);
                Cv2.Line(display, new Point(cx, cy - crossLen), new Point(cx, cy + crossLen),
                    DetectedColor, 1, LineTypes.AntiAlias);

                // 右下角信息面板（白色底，黑色字，清晰可读）
                DrawInfoPanel(display, r.Position, r.Angle, r.Score, trueAngle);
            }
            else
            {
                Console.WriteLine("NO MATCH");
                Cv2.PutText(display, "NO MATCH", new Point(20, 50),
                    HersheyFonts.HersheySimplex, 0.8, new Scalar(0, 0, 255), 2);
            }

            // 保存
            string safeName = $"PCBR01_{trueAngle:F1}deg".Replace(".", "p") + ".png";
            string outPath = Path.Combine(outputDir, safeName);
            Cv2.ImWrite(outPath, display);

            // 显示窗口
            string winTitle = $"PCB: {trueAngle:F1} deg";
            Cv2.NamedWindow(winTitle, WindowFlags.Normal);
            Cv2.ResizeWindow(winTitle,
                Math.Min(display.Width, 800), Math.Min(display.Height, 650));
            Cv2.ImShow(winTitle, display);
            Cv2.WaitKey(3000);
            Cv2.DestroyWindow(winTitle);
        }

        Cv2.DestroyAllWindows();
        Console.WriteLine();
        Console.WriteLine($"Done! Results: {outputDir}");
    }

    /// <summary>
    /// 绘制模板匹配检测到的旋转矩形。
    /// </summary>
    private static void DrawRotatedRect(Mat image, CvPoint center, double angle,
        int templateW, int templateH)
    {
        float cx = (float)center.X;
        float cy = (float)center.Y;
        // RotatedRect 用顺时针角度，GetRotationMatrix2D 用逆时针角度
        // 检测结果的角度是逆时针（与数据集一致），绘制时需要取反
        var rect = new RotatedRect(
            new Point2f(cx, cy),
            new Size2f(templateW, templateH),
            -(float)angle);

        Point2f[] pts = rect.Points();
        // 闭合多边形
        for (int i = 0; i < 4; i++)
        {
            Cv2.Line(image,
                new Point((int)pts[i].X, (int)pts[i].Y),
                new Point((int)pts[(i + 1) % 4].X, (int)pts[(i + 1) % 4].Y),
                DetectedColor, 1, LineTypes.AntiAlias);
        }
    }

    /// <summary>
    /// 绘制信息面板（右下角，白色背景黑色文字）。
    /// </summary>
    private static void DrawInfoPanel(Mat image, CvPoint pos, double angle,
        double score, double trueAngle)
    {
        string[] lines =
        {
            $"True Angle: {trueAngle:F1} deg",
            $"Detected:   ({pos.X:F1}, {pos.Y:F1})",
            $"Angle:      {angle:F2} deg",
            $"Score:      {score:F4}",
            $"Error:      {(angle - trueAngle >= 0 ? "+" : "")}{angle - trueAngle:F2} deg",
        };

        // 面板位置：右下角
        int panelW = 240;
        int panelH = lines.Length * 18 + 12;
        int panelX = image.Width - panelW - 10;
        int panelY = image.Height - panelH - 10;

        // 白色背景 + 黑色边框
        var panelRect = new Rect(panelX, panelY, panelW, panelH);
        Cv2.Rectangle(image, panelRect, new Scalar(255, 255, 255), -1);
        Cv2.Rectangle(image, panelRect, new Scalar(0, 0, 0), 1);

        for (int i = 0; i < lines.Length; i++)
        {
            Cv2.PutText(image, lines[i],
                new Point(panelX + 8, panelY + 16 + i * 18),
                HersheyFonts.HersheySimplex, 0.45, new Scalar(0, 0, 0), 1);
        }
    }

    private static double ExtractAngle(string filename)
    {
        // PCBR01_501_0.600000.png → 0.6
        var parts = filename.Split('_');
        if (parts.Length >= 3)
        {
            string numStr = parts[^1].Replace(".png", "");
            if (double.TryParse(numStr,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double val))
                return val;
        }
        return 0;
    }

    private static string? FindAnglePcbDir()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "test", "TestData", "Real",
                "PCB_Alignment_Datasets", "02_1_Angle0_1Testing", "AnglePCB", "AnglePCB"),
            Path.Combine(
                Path.GetDirectoryName(typeof(RealDataDemo).Assembly.Location)!,
                "..", "..", "..", "..", "test", "TestData", "Real",
                "PCB_Alignment_Datasets", "02_1_Angle0_1Testing", "AnglePCB", "AnglePCB"),
        };
        foreach (var d in candidates)
        {
            var resolved = Path.GetFullPath(d);
            if (Directory.Exists(resolved)) return resolved;
        }
        return null;
    }
}
