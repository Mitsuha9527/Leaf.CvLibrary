using System.Text;
using System.Text.Json;
using System.Globalization;
using Leaf.ColorDetector.Calibration;
using Leaf.ColorDetector.ColorScience;
using Leaf.ColorDetector.Configs;
using Leaf.ColorDetector.Detectors;
using Leaf.ColorDetector.Models;
using Leaf.ColorDetector.Preprocessing;
using OpenCvSharp;

namespace Leaf.ColorDetector.Visualization;

public sealed class FolderVisualizationOptions
{
    public int MaxVisualizedImagesPerColor { get; set; } = 20;

    public string[] ImageExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff"];

    /// <summary>
    /// 是否在 Δ 图中显示模糊区（前两名分差过小区域）。
    /// </summary>
    public bool ShowAmbiguousZone { get; set; } = true;

    /// <summary>
    /// 是否在 Δ 图中显示决策边界线。
    /// </summary>
    public bool ShowDecisionBoundary { get; set; } = true;
}

public sealed class FolderCalibrationSummary
{
    public required string DatasetRoot { get; init; }

    public required string OutputRoot { get; init; }

    public required int TotalSamples { get; init; }

    public required int CorrectSamples { get; init; }

    public required double Accuracy { get; init; }

    public required List<ColorCalibrationSummary> Colors { get; init; }
}

public sealed class ColorCalibrationSummary
{
    public required string ColorName { get; init; }

    public required int SampleCount { get; init; }

    public required int CorrectCount { get; init; }

    public required double Accuracy { get; init; }

    public required FuseColorDefinition CalibratedDefinition { get; init; }
}

/// <summary>
/// 文件夹批量校准 + 可视化验证流程。
/// <para>
/// 约定：datasetRoot 下每个子文件夹名即颜色名，子文件夹内为该颜色样本图。
/// 执行后会输出：
/// 1. 校准后的颜色定义 JSON
/// 2. 每张图的预处理中间结果图（原图/裁剪/模糊/Lab预览/掩码/掩码后图）
/// 3. 检测结果 CSV 与汇总 JSON
/// </para>
/// </summary>
public static class FolderCalibrationVisualizer
{
    private sealed class SampleValidationRecord
    {
        public required string ColorName { get; init; }

        public required string ImagePath { get; init; }

        public required ColorDetectResult Result { get; init; }

        public string? StepDir { get; init; }
    }

    /// <summary>
    /// 一键运行：只提供数据集目录即可完成校准、验证和可视化导出。
    /// 输出目录默认：datasetRoot/_viz_yyyyMMdd_HHmmss。
    /// </summary>
    public static FolderCalibrationSummary RunQuick(
        string datasetRoot,
        string? outputRoot = null,
        ColorDetectorOptions? detectorOptions = null,
        FolderVisualizationOptions? visualizationOptions = null)
    {
        outputRoot ??= Path.Combine(
            datasetRoot,
            $"_viz_{DateTime.Now:yyyyMMdd_HHmmss}");

        return Run(datasetRoot, outputRoot, detectorOptions, visualizationOptions);
    }

    public static FolderCalibrationSummary Run(
        string datasetRoot,
        string outputRoot,
        ColorDetectorOptions? detectorOptions = null,
        FolderVisualizationOptions? visualizationOptions = null)
    {
        if (string.IsNullOrWhiteSpace(datasetRoot))
            throw new ArgumentException("Dataset folder is required.", nameof(datasetRoot));

        if (!Directory.Exists(datasetRoot))
            throw new DirectoryNotFoundException($"Dataset folder not found: {datasetRoot}");

        detectorOptions ??= new ColorDetectorOptions();
        visualizationOptions ??= new FolderVisualizationOptions();

        Directory.CreateDirectory(outputRoot);
        var stepsRoot = Path.Combine(outputRoot, "steps");
        Directory.CreateDirectory(stepsRoot);

        var colorDirs = Directory.GetDirectories(datasetRoot)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (colorDirs.Count == 0)
            throw new InvalidOperationException("No color sub-folders found in dataset folder.");

        var colorFiles = BuildColorFileMap(colorDirs, visualizationOptions.ImageExtensions);

        var definitions = new List<FuseColorDefinition>();
        foreach (var (colorName, files) in colorFiles)
        {
            if (files.Count == 0)
                continue;

            var mats = new List<Mat>(files.Count);
            try
            {
                foreach (var file in files)
                {
                    var img = Cv2.ImRead(file, ImreadModes.Color);
                    if (!img.Empty())
                        mats.Add(img);
                    else
                        img.Dispose();
                }

                if (mats.Count == 0)
                    continue;

                var def = ColorCalibrator.LearnFromMultipleRois(mats, colorName, options: detectorOptions);
                definitions.Add(def);
            }
            finally
            {
                foreach (var mat in mats)
                    mat.Dispose();
            }
        }

        if (definitions.Count == 0)
            throw new InvalidOperationException("No valid images available for calibration.");

        var definitionsPath = Path.Combine(outputRoot, "calibrated-definitions.json");
        ColorDefinitionStore.SaveAsync(definitionsPath, definitions).GetAwaiter().GetResult();

        var deltaMapPath = Path.Combine(outputRoot, "delta-map-ab.png");
        SaveDeltaMap(definitions, detectorOptions, visualizationOptions, deltaMapPath);

        var labColorMapPath = Path.Combine(outputRoot, "lab-color-map-ab.png");
        SaveLabColorMap(definitions, labColorMapPath);

        var trajectoryPath = Path.Combine(outputRoot, "rgb-lab-trajectory.png");
        SaveRgbLabTrajectoryDemo(trajectoryPath);

        var detector = new FuseColorDetector(detectorOptions);
        var csv = new StringBuilder();
        csv.AppendLine("Expected,Detected,IsMatch,Quality,DeltaE,Confidence,Dispersion,SpatialConsistent,ImagePath");

        var colorSummaries = new List<ColorCalibrationSummary>();
        var sampleRecords = new List<SampleValidationRecord>();
        var total = 0;
        var correct = 0;

        foreach (var (colorName, files) in colorFiles)
        {
            var calibrated = definitions.FirstOrDefault(d =>
                string.Equals(d.ColorName, colorName, StringComparison.OrdinalIgnoreCase));

            if (calibrated is null)
                continue;

            var colorTotal = 0;
            var colorCorrect = 0;
            var visualized = 0;

            var colorStepRoot = Path.Combine(stepsRoot, SanitizeName(colorName));
            Directory.CreateDirectory(colorStepRoot);

            foreach (var file in files)
            {
                using var image = Cv2.ImRead(file, ImreadModes.Color);
                if (image.Empty())
                    continue;

                using var debug = ImagePreprocessor.PreprocessWithDebug(image, detectorOptions);
                string? stepDir = null;

                if (visualized < visualizationOptions.MaxVisualizedImagesPerColor)
                {
                    stepDir = Path.Combine(
                        colorStepRoot,
                        $"{visualized + 1:D3}_{SanitizeName(Path.GetFileNameWithoutExtension(file))}");
                    using var labDebug = LabStatistics.ComputeRobustLabWithDebug(debug.LabMat, debug.ValidMask);
                    SavePreprocessStages(stepDir, image, debug, labDebug);
                    visualized++;
                }

                var result = detector.Detect(image, colorName, definitions);

                sampleRecords.Add(new SampleValidationRecord
                {
                    ColorName = colorName,
                    ImagePath = file,
                    Result = result,
                    StepDir = stepDir
                });

                csv.AppendLine(string.Join(',',
                    EscapeCsv(colorName),
                    EscapeCsv(result.DetectedColor),
                    result.IsMatch,
                    result.Quality,
                    result.DeltaE.ToString("F3", CultureInfo.InvariantCulture),
                    result.Confidence.ToString("F4", CultureInfo.InvariantCulture),
                    result.Dispersion.ToString("F3", CultureInfo.InvariantCulture),
                    result.IsSpatiallyConsistent,
                    EscapeCsv(file)));

                total++;
                colorTotal++;
                if (result.IsMatch)
                {
                    correct++;
                    colorCorrect++;
                }
            }

            colorSummaries.Add(new ColorCalibrationSummary
            {
                ColorName = colorName,
                SampleCount = colorTotal,
                CorrectCount = colorCorrect,
                Accuracy = colorTotal > 0 ? (double)colorCorrect / colorTotal : 0,
                CalibratedDefinition = calibrated
            });
        }

        File.WriteAllText(Path.Combine(outputRoot, "validation-results.csv"), csv.ToString(), Encoding.UTF8);

        var summary = new FolderCalibrationSummary
        {
            DatasetRoot = datasetRoot,
            OutputRoot = outputRoot,
            TotalSamples = total,
            CorrectSamples = correct,
            Accuracy = total > 0 ? (double)correct / total : 0,
            Colors = colorSummaries
        };

        var summaryJson = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(outputRoot, "summary.json"), summaryJson, Encoding.UTF8);

        var reportHtml = BuildHtmlReport(summary, sampleRecords, datasetRoot, outputRoot, visualizationOptions);
        File.WriteAllText(Path.Combine(outputRoot, "report.html"), reportHtml, Encoding.UTF8);

        return summary;
    }

    private static string BuildHtmlReport(
        FolderCalibrationSummary summary,
        List<SampleValidationRecord> samples,
        string datasetRoot,
        string outputRoot,
        FolderVisualizationOptions visualizationOptions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html><head><meta charset=\"utf-8\" />");
        sb.AppendLine("<title>Leaf.ColorDetector Validation Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:16px;} table{border-collapse:collapse;width:100%;margin:8px 0 16px;} th,td{border:1px solid #ddd;padding:6px 8px;font-size:13px;} th{background:#f5f5f5;} .ok{color:#0a7a2f;font-weight:600;} .ng{color:#c62828;font-weight:600;} .muted{color:#666;} .mono{font-family:Consolas,monospace;} h2{margin-top:24px;} a{color:#1565c0;text-decoration:none;} a:hover{text-decoration:underline;} </style>");
        sb.AppendLine("</head><body>");

        sb.AppendLine("<h1>Leaf.ColorDetector 校准与验证报告</h1>");
        sb.AppendLine($"<p class='muted'>数据集: <span class='mono'>{Html(datasetRoot)}</span><br/>输出目录: <span class='mono'>{Html(outputRoot)}</span></p>");
        sb.AppendLine($"<p><b>总样本:</b> {summary.TotalSamples} &nbsp; <b>正确:</b> {summary.CorrectSamples} &nbsp; <b>准确率:</b> {summary.Accuracy:P2}</p>");

        var deltaMapFile = "delta-map-ab.png";
        sb.AppendLine("<h2>Δ 图（a*b* 平面）</h2>");
        var legend = new StringBuilder("圆心 = 每个颜色标准位置 (RefA, RefB)；外圈 = 该颜色范围区间（优先 ChromaTolerance，否则 MaxΔE）。");
        if (visualizationOptions.ShowDecisionBoundary)
            legend.Append(" 深色线 = 决策边界;");
        if (visualizationOptions.ShowAmbiguousZone)
            legend.Append(" 灰色带 = 模糊区（前两名分差过小）。");
        sb.AppendLine($"<p class='muted'>{Html(legend.ToString())}</p>");
        sb.AppendLine($"<p><a href='{HtmlAttr(deltaMapFile)}'><img src='{HtmlAttr(deltaMapFile)}' alt='delta map' style='max-width:100%;border:1px solid #ddd'/></a></p>");

        var labMapFile = "lab-color-map-ab.png";
        sb.AppendLine("<h2>Lab 颜色图（a*b* 平面）</h2>");
        sb.AppendLine("<p class='muted'>背景 = 固定 L* 下的 Lab 真实颜色分布；叠加圆心与容差圈 = 校准结果。</p>");
        sb.AppendLine($"<p><a href='{HtmlAttr(labMapFile)}'><img src='{HtmlAttr(labMapFile)}' alt='lab-color-map' style='max-width:100%;border:1px solid #ddd'/></a></p>");

        var trajectoryFile = "rgb-lab-trajectory.png";
        sb.AppendLine("<h2>RGB 与 Lab 轨迹对比图（同一颜色扰动）</h2>");
        sb.AppendLine("<p class='muted'>左图：RGB(R-G投影) 轨迹；右图：Lab(a*b*平面) 轨迹。蓝=亮度变化，绿=饱和度变化，紫=色相变化。</p>");
        sb.AppendLine($"<p><a href='{HtmlAttr(trajectoryFile)}'><img src='{HtmlAttr(trajectoryFile)}' alt='rgb-lab-trajectory' style='max-width:100%;border:1px solid #ddd'/></a></p>");

        sb.AppendLine("<h2>按颜色汇总</h2>");
        sb.AppendLine("<table><thead><tr><th>颜色</th><th>样本数</th><th>正确数</th><th>准确率</th><th>RefLab</th><th>MaxΔE</th><th>L容差</th><th>C容差</th></tr></thead><tbody>");
        foreach (var c in summary.Colors.OrderBy(x => x.ColorName, StringComparer.OrdinalIgnoreCase))
        {
            var d = c.CalibratedDefinition;
            sb.AppendLine($"<tr><td>{Html(c.ColorName)}</td><td>{c.SampleCount}</td><td>{c.CorrectCount}</td><td>{c.Accuracy:P2}</td><td class='mono'>({d.RefL:F1},{d.RefA:F1},{d.RefB:F1})</td><td>{d.MaxDeltaE:F1}</td><td>{d.LightnessTolerance:F1}</td><td>{d.ChromaTolerance:F1}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<h2>样本明细</h2>");
        sb.AppendLine("<table><thead><tr><th>期望</th><th>检测</th><th>匹配</th><th>质量</th><th>ΔE</th><th>置信度</th><th>Disp</th><th>Spatial</th><th>原图</th><th>步骤目录</th></tr></thead><tbody>");
        foreach (var s in samples.OrderBy(x => x.ColorName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.ImagePath, StringComparer.OrdinalIgnoreCase))
        {
            var cls = s.Result.IsMatch ? "ok" : "ng";
            var relImg = MakeRelativePath(outputRoot, s.ImagePath);
            var relStep = string.IsNullOrWhiteSpace(s.StepDir) ? "" : MakeRelativePath(outputRoot, s.StepDir!);
            var stepCell = string.IsNullOrWhiteSpace(relStep)
                ? "-"
                : $"<a href='{HtmlAttr(relStep)}'>打开</a>";

            sb.AppendLine($"<tr><td>{Html(s.ColorName)}</td><td>{Html(s.Result.DetectedColor)}</td><td class='{cls}'>{(s.Result.IsMatch ? "OK" : "NG")}</td><td>{Html(s.Result.Quality.ToString())}</td><td>{s.Result.DeltaE:F2}</td><td>{s.Result.Confidence:F3}</td><td>{s.Result.Dispersion:F2}</td><td>{s.Result.IsSpatiallyConsistent}</td><td><a href='{HtmlAttr(relImg)}'>{Html(Path.GetFileName(s.ImagePath))}</a></td><td>{stepCell}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string MakeRelativePath(string fromPath, string toPath)
    {
        var rel = Path.GetRelativePath(fromPath, toPath);
        return rel.Replace('\\', '/');
    }

    private static string Html(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static string HtmlAttr(string value) => Html(value);

    private static Dictionary<string, List<string>> BuildColorFileMap(
        IEnumerable<string> colorDirs,
        IEnumerable<string> extensions)
    {
        var extSet = new HashSet<string>(extensions.Select(e => e.ToLowerInvariant()));
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in colorDirs)
        {
            var colorName = Path.GetFileName(dir);
            var files = Directory.EnumerateFiles(dir)
                .Where(f => extSet.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            result[colorName] = files;
        }

        return result;
    }

    private static void SavePreprocessStages(
        string sampleDir,
        Mat original,
        ImagePreprocessor.PreprocessDebugResult debug,
        LabStatistics.LabStatisticsDebugResult labDebug)
    {
        Directory.CreateDirectory(sampleDir);

        Cv2.ImWrite(Path.Combine(sampleDir, "00_original.png"), original);
        Cv2.ImWrite(Path.Combine(sampleDir, "01_cropped.png"), debug.CroppedBgr);
        Cv2.ImWrite(Path.Combine(sampleDir, "02_blurred.png"), debug.BlurredBgr);

        using var labPreview = new Mat();
        Cv2.CvtColor(debug.LabMat, labPreview, ColorConversionCodes.Lab2BGR);
        Cv2.ImWrite(Path.Combine(sampleDir, "03_lab_preview.png"), labPreview);

        Cv2.ImWrite(Path.Combine(sampleDir, "04_valid_mask.png"), debug.ValidMask);

        using var masked = new Mat();
        Cv2.BitwiseAnd(debug.CroppedBgr, debug.CroppedBgr, masked, debug.ValidMask);
        Cv2.ImWrite(Path.Combine(sampleDir, "05_masked.png"), masked);

        Cv2.ImWrite(Path.Combine(sampleDir, "06_lab_step1_initial_valid_mask.png"), labDebug.Step1InitialValidMask);
        Cv2.ImWrite(Path.Combine(sampleDir, "07_lab_step2_mad_inlier_mask.png"), labDebug.Step2MadInlierMask);

        using var step1Masked = new Mat();
        Cv2.BitwiseAnd(debug.CroppedBgr, debug.CroppedBgr, step1Masked, labDebug.Step1InitialValidMask);
        Cv2.ImWrite(Path.Combine(sampleDir, "08_lab_step1_masked.png"), step1Masked);

        using var step2Masked = new Mat();
        Cv2.BitwiseAnd(debug.CroppedBgr, debug.CroppedBgr, step2Masked, labDebug.Step2MadInlierMask);
        Cv2.ImWrite(Path.Combine(sampleDir, "09_lab_step2_masked.png"), step2Masked);

        using var finalOnOriginal = original.Clone();
        using var overlay = new Mat(finalOnOriginal.Size(), finalOnOriginal.Type(), new Scalar(40, 220, 40));
        using var fullMask = new Mat(finalOnOriginal.Rows, finalOnOriginal.Cols, MatType.CV_8UC1, Scalar.All(0));

        using (var fullMaskRoi = new Mat(fullMask, debug.CropRectInOriginal))
            labDebug.Step2MadInlierMask.CopyTo(fullMaskRoi);

        using var blended = new Mat();
        Cv2.AddWeighted(finalOnOriginal, 0.65, overlay, 0.35, 0, blended);
        blended.CopyTo(finalOnOriginal, fullMask);

        Cv2.Rectangle(finalOnOriginal, debug.CropRectInOriginal, new Scalar(0, 180, 255), 2, LineTypes.AntiAlias);
        Cv2.PutText(finalOnOriginal, "Final pixels used for Lab statistics", new Point(12, 28),
            HersheyFonts.HersheySimplex, 0.7, new Scalar(0, 180, 255), 2, LineTypes.AntiAlias);
        Cv2.ImWrite(Path.Combine(sampleDir, "10_final_lab_region_on_original.png"), finalOnOriginal);
    }

    private static string SanitizeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) >= 0)
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static void SaveDeltaMap(
        IReadOnlyList<FuseColorDefinition> definitions,
        ColorDetectorOptions options,
        FolderVisualizationOptions visualizationOptions,
        string outputPath)
    {
        const int width = 980;
        const int height = 900;
        const int padLeft = 90;
        const int padTop = 80;
        const int plot = 760;

        using var canvas = new Mat(height, width, MatType.CV_8UC3, new Scalar(255, 255, 255));

        var plotRect = new Rect(padLeft, padTop, plot, plot);
        RenderDeltaE2000Background(canvas, definitions, options, visualizationOptions, padLeft, padTop, plot);
        Cv2.Rectangle(canvas, plotRect, new Scalar(220, 220, 220), 1);

        for (var tick = -128; tick <= 128; tick += 32)
        {
            var x = MapA(tick, padLeft, plot);
            var y = MapB(tick, padTop, plot);

            var gridColor = tick == 0 ? new Scalar(120, 120, 120) : new Scalar(235, 235, 235);
            var thick = tick == 0 ? 2 : 1;

            Cv2.Line(canvas, new Point(x, padTop), new Point(x, padTop + plot), gridColor, thick);
            Cv2.Line(canvas, new Point(padLeft, y), new Point(padLeft + plot, y), gridColor, thick);

            if (tick % 64 == 0)
            {
                Cv2.PutText(canvas, tick.ToString(CultureInfo.InvariantCulture), new Point(x - 14, padTop + plot + 24),
                    HersheyFonts.HersheySimplex, 0.45, new Scalar(100, 100, 100), 1);
                Cv2.PutText(canvas, tick.ToString(CultureInfo.InvariantCulture), new Point(padLeft - 40, y + 4),
                    HersheyFonts.HersheySimplex, 0.45, new Scalar(100, 100, 100), 1);
            }
        }

        Cv2.PutText(canvas, "a*", new Point(padLeft + plot + 18, padTop + plot / 2), HersheyFonts.HersheySimplex, 0.7, new Scalar(60, 60, 60), 2);
        Cv2.PutText(canvas, "b*", new Point(padLeft + plot / 2, padTop - 22), HersheyFonts.HersheySimplex, 0.7, new Scalar(60, 60, 60), 2);
        Cv2.PutText(canvas, "Delta Map in a*b* Plane (CIE DE2000 background + center + tolerance)", new Point(60, 36), HersheyFonts.HersheySimplex, 0.65, new Scalar(30, 30, 30), 2);

        foreach (var def in definitions)
        {
            var center = new Point(MapA(def.RefA, padLeft, plot), MapB(def.RefB, padTop, plot));

            var tol = def.ChromaTolerance > 0 ? def.ChromaTolerance : def.MaxDeltaE;
            var radius = (int)Math.Round(Math.Max(2, tol / 255.0 * plot));

            var bgr = LabToBgr(def.RefL, def.RefA, def.RefB);
            var ring = Lighten(bgr, 0.45);

            Cv2.Circle(canvas, center, radius, ring, 2, LineTypes.AntiAlias);
            Cv2.Circle(canvas, center, 5, bgr, -1, LineTypes.AntiAlias);
            Cv2.Circle(canvas, center, 6, new Scalar(40, 40, 40), 1, LineTypes.AntiAlias);

            var label = $"{def.ColorName} (±{tol:F1})";
            Cv2.PutText(canvas, label, new Point(center.X + 8, center.Y - 8), HersheyFonts.HersheySimplex, 0.45, new Scalar(40, 40, 40), 1, LineTypes.AntiAlias);
        }

        Cv2.ImWrite(outputPath, canvas);
    }

    private static void RenderDeltaE2000Background(
        Mat canvas,
        IReadOnlyList<FuseColorDefinition> definitions,
        ColorDetectorOptions options,
        FolderVisualizationOptions visualizationOptions,
        int padLeft,
        int padTop,
        int plot)
    {
        if (definitions.Count == 0)
            return;

        var lMap = definitions.Average(d => d.RefL);
        var indexer = canvas.GetGenericIndexer<Vec3b>();
        var ownerMap = new int[plot, plot];
        var ambiguousMap = new bool[plot, plot];
        var ambiguousThreshold = Math.Max(0.04, options.MinConfidenceGap * 1.8);

        for (var y = 0; y < plot; y++)
        {
            var py = padTop + y;
            var bStar = UnmapB(py, padTop, plot);

            for (var x = 0; x < plot; x++)
            {
                var px = padLeft + x;
                var aStar = UnmapA(px, padLeft, plot);

                var bestNorm = double.MaxValue;
                var secondNorm = double.MaxValue;
                var nearestIndex = -1;
                FuseColorDefinition? nearest = null;

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    var de = ColorScience.DeltaE.Calculate(
                        d.RefL, d.RefA, d.RefB,
                        lMap, aStar, bStar);

                    var tol = d.ChromaTolerance > 0 ? d.ChromaTolerance : d.MaxDeltaE;
                    tol = Math.Max(tol, 1e-6);

                    var norm = de / tol;
                    if (norm < bestNorm)
                    {
                        secondNorm = bestNorm;
                        bestNorm = norm;
                        nearestIndex = i;
                        nearest = d;
                    }
                    else if (norm < secondNorm)
                    {
                        secondNorm = norm;
                    }
                }

                if (nearest is null)
                    continue;

                var baseColor = LabToBgr(nearest.RefL, nearest.RefA, nearest.RefB);

                // ΔE2000 背景强度：越接近中心越饱和，越远越趋于白色
                // norm<=1 属于容差区，norm>1 逐步淡出
                var fade = bestNorm <= 1.0
                    ? 0.20 + 0.55 * bestNorm
                    : Math.Min(0.95, 0.75 + 0.20 * (bestNorm - 1.0));

                var mixed = Lighten(baseColor, fade);

                var ambiguous = (secondNorm - bestNorm) < ambiguousThreshold;
                if (ambiguous && visualizationOptions.ShowAmbiguousZone)
                {
                    mixed = Blend(mixed, new Scalar(175, 175, 175), 0.50);
                }

                ownerMap[y, x] = nearestIndex;
                ambiguousMap[y, x] = ambiguous;
                indexer[py, px] = new Vec3b((byte)mixed.Val0, (byte)mixed.Val1, (byte)mixed.Val2);
            }
        }

        // 叠加决策边界：相邻像素主导颜色不同且都非模糊区时绘制深色边界
        if (!visualizationOptions.ShowDecisionBoundary)
            return;

        for (var y = 1; y < plot; y++)
        {
            var py = padTop + y;
            for (var x = 1; x < plot; x++)
            {
                if (ambiguousMap[y, x])
                    continue;

                var owner = ownerMap[y, x];
                if (owner < 0)
                    continue;

                var leftDiff = !ambiguousMap[y, x - 1] && ownerMap[y, x - 1] >= 0 && ownerMap[y, x - 1] != owner;
                var upDiff = !ambiguousMap[y - 1, x] && ownerMap[y - 1, x] >= 0 && ownerMap[y - 1, x] != owner;

                if (!leftDiff && !upDiff)
                    continue;

                var px = padLeft + x;
                indexer[py, px] = new Vec3b(35, 35, 35);
            }
        }
    }

    private static int MapA(double a, int padLeft, int plot)
    {
        var t = (a + 128.0) / 255.0;
        return padLeft + (int)Math.Round(Math.Clamp(t, 0, 1) * plot);
    }

    private static double UnmapA(int px, int padLeft, int plot)
    {
        var t = (double)(px - padLeft) / plot;
        t = Math.Clamp(t, 0, 1);
        return t * 255.0 - 128.0;
    }

    private static int MapB(double b, int padTop, int plot)
    {
        var t = (127.0 - b) / 255.0;
        return padTop + (int)Math.Round(Math.Clamp(t, 0, 1) * plot);
    }

    private static double UnmapB(int py, int padTop, int plot)
    {
        var t = (double)(py - padTop) / plot;
        t = Math.Clamp(t, 0, 1);
        return 127.0 - t * 255.0;
    }

    private static Scalar LabToBgr(double lStar, double aStar, double bStar)
    {
        var l8 = (byte)Math.Clamp((int)Math.Round(lStar * 255.0 / 100.0), 0, 255);
        var a8 = (byte)Math.Clamp((int)Math.Round(aStar + 128.0), 0, 255);
        var b8 = (byte)Math.Clamp((int)Math.Round(bStar + 128.0), 0, 255);

        using var lab = new Mat(1, 1, MatType.CV_8UC3, new Scalar(l8, a8, b8));
        using var bgr = new Mat();
        Cv2.CvtColor(lab, bgr, ColorConversionCodes.Lab2BGR);

        var v = bgr.At<Vec3b>(0, 0);
        return new Scalar(v.Item0, v.Item1, v.Item2);
    }

    private static Scalar Lighten(Scalar color, double ratio)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        var b = color.Val0 + (255 - color.Val0) * ratio;
        var g = color.Val1 + (255 - color.Val1) * ratio;
        var r = color.Val2 + (255 - color.Val2) * ratio;
        return new Scalar(b, g, r);
    }

    private static Scalar Blend(Scalar a, Scalar b, double alpha)
    {
        alpha = Math.Clamp(alpha, 0, 1);
        var inv = 1.0 - alpha;
        return new Scalar(
            a.Val0 * inv + b.Val0 * alpha,
            a.Val1 * inv + b.Val1 * alpha,
            a.Val2 * inv + b.Val2 * alpha);
    }

    private static void SaveLabColorMap(IReadOnlyList<FuseColorDefinition> definitions, string outputPath)
    {
        const int width = 980;
        const int height = 900;
        const int padLeft = 90;
        const int padTop = 80;
        const int plot = 760;

        using var canvas = new Mat(height, width, MatType.CV_8UC3, new Scalar(255, 255, 255));
        var indexer = canvas.GetGenericIndexer<Vec3b>();

        var lStar = definitions.Count > 0 ? definitions.Average(d => d.RefL) : 60.0;

        for (var y = 0; y < plot; y++)
        {
            var py = padTop + y;
            var b = UnmapB(py, padTop, plot);
            for (var x = 0; x < plot; x++)
            {
                var px = padLeft + x;
                var a = UnmapA(px, padLeft, plot);
                var c = LabToBgr(lStar, a, b);
                indexer[py, px] = new Vec3b((byte)c.Val0, (byte)c.Val1, (byte)c.Val2);
            }
        }

        var plotRect = new Rect(padLeft, padTop, plot, plot);
        Cv2.Rectangle(canvas, plotRect, new Scalar(220, 220, 220), 1);

        for (var tick = -128; tick <= 128; tick += 32)
        {
            var x = MapA(tick, padLeft, plot);
            var y = MapB(tick, padTop, plot);

            var gridColor = tick == 0 ? new Scalar(120, 120, 120) : new Scalar(240, 240, 240);
            var thick = tick == 0 ? 2 : 1;

            Cv2.Line(canvas, new Point(x, padTop), new Point(x, padTop + plot), gridColor, thick);
            Cv2.Line(canvas, new Point(padLeft, y), new Point(padLeft + plot, y), gridColor, thick);

            if (tick % 64 == 0)
            {
                Cv2.PutText(canvas, tick.ToString(CultureInfo.InvariantCulture), new Point(x - 14, padTop + plot + 24),
                    HersheyFonts.HersheySimplex, 0.45, new Scalar(80, 80, 80), 1);
                Cv2.PutText(canvas, tick.ToString(CultureInfo.InvariantCulture), new Point(padLeft - 40, y + 4),
                    HersheyFonts.HersheySimplex, 0.45, new Scalar(80, 80, 80), 1);
            }
        }

        Cv2.PutText(canvas, $"Lab Color Map in a*b* plane (L*={lStar:F1})", new Point(60, 36),
            HersheyFonts.HersheySimplex, 0.75, new Scalar(30, 30, 30), 2);
        Cv2.PutText(canvas, "a*", new Point(padLeft + plot + 18, padTop + plot / 2), HersheyFonts.HersheySimplex, 0.7, new Scalar(60, 60, 60), 2);
        Cv2.PutText(canvas, "b*", new Point(padLeft + plot / 2, padTop - 22), HersheyFonts.HersheySimplex, 0.7, new Scalar(60, 60, 60), 2);

        foreach (var def in definitions)
        {
            var center = new Point(MapA(def.RefA, padLeft, plot), MapB(def.RefB, padTop, plot));
            var tol = def.ChromaTolerance > 0 ? def.ChromaTolerance : def.MaxDeltaE;
            var radius = (int)Math.Round(Math.Max(2, tol / 255.0 * plot));

            var bgr = LabToBgr(def.RefL, def.RefA, def.RefB);
            Cv2.Circle(canvas, center, radius, new Scalar(25, 25, 25), 2, LineTypes.AntiAlias);
            Cv2.Circle(canvas, center, 5, bgr, -1, LineTypes.AntiAlias);
            Cv2.Circle(canvas, center, 6, new Scalar(25, 25, 25), 1, LineTypes.AntiAlias);

            Cv2.PutText(canvas, def.ColorName, new Point(center.X + 8, center.Y - 8),
                HersheyFonts.HersheySimplex, 0.45, new Scalar(25, 25, 25), 1, LineTypes.AntiAlias);
        }

        Cv2.ImWrite(outputPath, canvas);
    }

    private enum DemoVariation
    {
        Brightness,
        Saturation,
        Hue
    }

    private sealed record DemoPoint(double R, double G, double B, double L, double A, double BB, Scalar Bgr);

    private static void SaveRgbLabTrajectoryDemo(string outputPath)
    {
        const int width = 1320;
        const int height = 760;
        var left = new Rect(60, 120, 540, 540);
        var right = new Rect(720, 120, 540, 540);

        using var canvas = new Mat(height, width, MatType.CV_8UC3, new Scalar(255, 255, 255));

        Cv2.PutText(canvas, "RGB vs Lab Movement (same base color)", new Point(40, 48),
            HersheyFonts.HersheySimplex, 0.95, new Scalar(30, 30, 30), 2);
        Cv2.PutText(canvas, "Blue: brightness, Green: saturation, Purple: hue", new Point(40, 78),
            HersheyFonts.HersheySimplex, 0.6, new Scalar(70, 70, 70), 1);

        DrawDemoGrid(canvas, left, "RGB plane (R-G projection)", "R", "G");
        DrawDemoGrid(canvas, right, "Lab plane (a*-b*)", "a*", "b*");

        var baseBgr = new Scalar(30, 120, 220);
        var brightness = GenerateDemoTrajectory(baseBgr, DemoVariation.Brightness);
        var saturation = GenerateDemoTrajectory(baseBgr, DemoVariation.Saturation);
        var hue = GenerateDemoTrajectory(baseBgr, DemoVariation.Hue);

        DrawDemoTrajectory(canvas, left, right, brightness, new Scalar(255, 120, 60));
        DrawDemoTrajectory(canvas, left, right, saturation, new Scalar(70, 180, 70));
        DrawDemoTrajectory(canvas, left, right, hue, new Scalar(220, 80, 210));

        DrawSwatchStrip(canvas, 60, 690, brightness, "Brightness");
        DrawSwatchStrip(canvas, 460, 690, saturation, "Saturation");
        DrawSwatchStrip(canvas, 860, 690, hue, "Hue");

        Cv2.ImWrite(outputPath, canvas);
    }

    private static List<DemoPoint> GenerateDemoTrajectory(Scalar baseBgr, DemoVariation variation)
    {
        var points = new List<DemoPoint>(9);

        using var baseMat = new Mat(1, 1, MatType.CV_8UC3, baseBgr);
        using var baseHsv = new Mat();
        Cv2.CvtColor(baseMat, baseHsv, ColorConversionCodes.BGR2HSV);
        var hv = baseHsv.At<Vec3b>(0, 0);

        for (var i = -4; i <= 4; i++)
        {
            var h = hv.Item0;
            var s = hv.Item1;
            var v = hv.Item2;

            switch (variation)
            {
                case DemoVariation.Brightness:
                    v = ClampToByte(v * (1.0 + i * 0.12));
                    break;
                case DemoVariation.Saturation:
                    s = ClampToByte(s * (1.0 + i * 0.14));
                    break;
                case DemoVariation.Hue:
                    h = (byte)((h + i * 6 + 180) % 180);
                    break;
            }

            using var hsv = new Mat(1, 1, MatType.CV_8UC3, new Scalar(h, s, v));
            using var bgr = new Mat();
            Cv2.CvtColor(hsv, bgr, ColorConversionCodes.HSV2BGR);

            var bgrPix = bgr.At<Vec3b>(0, 0);
            var rgbR = bgrPix.Item2;
            var rgbG = bgrPix.Item1;
            var rgbB = bgrPix.Item0;
            var lab = ConvertBgrToStandardLab(new Scalar(bgrPix.Item0, bgrPix.Item1, bgrPix.Item2));

            points.Add(new DemoPoint(
                rgbR,
                rgbG,
                rgbB,
                lab.L,
                lab.A,
                lab.B,
                new Scalar(bgrPix.Item0, bgrPix.Item1, bgrPix.Item2)));
        }

        return points;
    }

    private static void DrawDemoGrid(Mat canvas, Rect rect, string title, string xAxis, string yAxis)
    {
        Cv2.Rectangle(canvas, rect, new Scalar(210, 210, 210), 1);
        for (var i = 1; i < 10; i++)
        {
            var x = rect.X + i * rect.Width / 10;
            var y = rect.Y + i * rect.Height / 10;
            Cv2.Line(canvas, new Point(x, rect.Y), new Point(x, rect.Bottom), new Scalar(242, 242, 242), 1);
            Cv2.Line(canvas, new Point(rect.X, y), new Point(rect.Right, y), new Scalar(242, 242, 242), 1);
        }

        Cv2.PutText(canvas, title, new Point(rect.X, rect.Y - 14), HersheyFonts.HersheySimplex, 0.6, new Scalar(45, 45, 45), 2);
        Cv2.PutText(canvas, xAxis, new Point(rect.Right + 8, rect.Y + rect.Height / 2), HersheyFonts.HersheySimplex, 0.55, new Scalar(55, 55, 55), 1);
        Cv2.PutText(canvas, yAxis, new Point(rect.X + rect.Width / 2, rect.Y - 30), HersheyFonts.HersheySimplex, 0.55, new Scalar(55, 55, 55), 1);
    }

    private static void DrawDemoTrajectory(
        Mat canvas,
        Rect rgbRect,
        Rect labRect,
        List<DemoPoint> points,
        Scalar lineColor)
    {
        for (var i = 1; i < points.Count; i++)
        {
            var p0Rgb = MapRgbPoint(points[i - 1], rgbRect);
            var p1Rgb = MapRgbPoint(points[i], rgbRect);
            Cv2.Line(canvas, p0Rgb, p1Rgb, lineColor, 2, LineTypes.AntiAlias);

            var p0Lab = MapLabPoint(points[i - 1], labRect);
            var p1Lab = MapLabPoint(points[i], labRect);
            Cv2.Line(canvas, p0Lab, p1Lab, lineColor, 2, LineTypes.AntiAlias);
        }

        for (var i = 0; i < points.Count; i++)
        {
            var rgbP = MapRgbPoint(points[i], rgbRect);
            var labP = MapLabPoint(points[i], labRect);

            Cv2.Circle(canvas, rgbP, 4, points[i].Bgr, -1, LineTypes.AntiAlias);
            Cv2.Circle(canvas, rgbP, 5, new Scalar(35, 35, 35), 1, LineTypes.AntiAlias);

            Cv2.Circle(canvas, labP, 4, points[i].Bgr, -1, LineTypes.AntiAlias);
            Cv2.Circle(canvas, labP, 5, new Scalar(35, 35, 35), 1, LineTypes.AntiAlias);
        }
    }

    private static void DrawSwatchStrip(Mat canvas, int x, int y, List<DemoPoint> points, string label)
    {
        Cv2.PutText(canvas, label, new Point(x, y - 8), HersheyFonts.HersheySimplex, 0.55, new Scalar(50, 50, 50), 1);
        for (var i = 0; i < points.Count; i++)
        {
            var r = new Rect(x + i * 34, y, 30, 26);
            Cv2.Rectangle(canvas, r, points[i].Bgr, -1);
            Cv2.Rectangle(canvas, r, new Scalar(80, 80, 80), 1);
        }
    }

    private static Point MapRgbPoint(DemoPoint p, Rect rect)
    {
        var tx = p.R / 255.0;
        var ty = 1.0 - p.G / 255.0;
        return new Point(
            rect.X + (int)Math.Round(tx * rect.Width),
            rect.Y + (int)Math.Round(ty * rect.Height));
    }

    private static Point MapLabPoint(DemoPoint p, Rect rect)
    {
        var tx = (p.A + 128.0) / 255.0;
        var ty = (127.0 - p.BB) / 255.0;
        return new Point(
            rect.X + (int)Math.Round(Math.Clamp(tx, 0, 1) * rect.Width),
            rect.Y + (int)Math.Round(Math.Clamp(ty, 0, 1) * rect.Height));
    }

    private static (double L, double A, double B) ConvertBgrToStandardLab(Scalar bgr)
    {
        using var bgrMat = new Mat(1, 1, MatType.CV_8UC3, bgr);
        using var labMat = new Mat();
        Cv2.CvtColor(bgrMat, labMat, ColorConversionCodes.BGR2Lab);
        var v = labMat.At<Vec3b>(0, 0);

        return (
            v.Item0 * 100.0 / 255.0,
            v.Item1 - 128.0,
            v.Item2 - 128.0);
    }

    private static byte ClampToByte(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    }
}
