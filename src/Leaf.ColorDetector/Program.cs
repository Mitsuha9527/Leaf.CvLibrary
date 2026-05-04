using Leaf.ColorDetector.Models;
using Leaf.ColorDetector.Visualization;

Console.WriteLine("Leaf.ColorDetector - Folder Calibration + Visualization");
Console.WriteLine("Dataset rule: each sub-folder name is treated as the color name.");
Console.WriteLine();

Console.Write("Input datasetRoot: ");
var datasetRoot = @"D:\Project\KRXProject\KRXVison\KRXVison\bin\Debug\net10.0-windows7.0\crop_images";
if (string.IsNullOrWhiteSpace(datasetRoot))
{
    Console.Error.WriteLine("datasetRoot is required.");
    Environment.ExitCode = 1;
    return;
}

Console.Write("Input outputRoot (empty = auto): ");
var outputRootInput = @"D:\Project\KRXProject\KRXVison\KRXVison\bin\Debug\net10.0-windows7.0\crop_images\outputs";
var outputRoot = string.IsNullOrWhiteSpace(outputRootInput) ? null : outputRootInput;

try
{
    var detectorOptions = new ColorDetectorOptions();
    var visualizationOptions = new FolderVisualizationOptions();

    var summary = FolderCalibrationVisualizer.RunQuick(
        datasetRoot,
        outputRoot,
        detectorOptions,
        visualizationOptions);

    Console.WriteLine("Calibration + validation finished.");
    Console.WriteLine($"Output: {summary.OutputRoot}");
    Console.WriteLine($"Accuracy: {summary.CorrectSamples}/{summary.TotalSamples} ({summary.Accuracy:P2})");
    Console.WriteLine($"Report: {Path.Combine(summary.OutputRoot, "report.html")}");
}
catch (Exception ex)
{
    Console.Error.WriteLine("Run failed:");
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
