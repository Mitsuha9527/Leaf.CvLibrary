using System.Text.Json;
using System.Text.Json.Serialization;
using Leaf.ColorDetector.Models;

namespace Leaf.ColorDetector.Configs;

/// <summary>
/// 颜色定义的加载与保存工具。
/// 支持从 JSON 文件加载自定义颜色定义，或使用标准默认值。
/// </summary>
public static class ColorDefinitionStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    /// <summary>
    /// 从 JSON 文件加载颜色定义。
    /// 如果文件不存在，返回标准默认值并自动保存到文件。
    /// </summary>
    public static async Task<List<FuseColorDefinition>> LoadAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            var definitions = JsonSerializer.Deserialize<List<FuseColorDefinition>>(json, s_jsonOptions);
            return definitions ?? StandardFuseColors.GetDefaults();
        }

        var defaults = StandardFuseColors.GetDefaults();
        await SaveAsync(filePath, defaults);
        return defaults;
    }

    /// <summary>
    /// 将颜色定义保存到 JSON 文件。
    /// </summary>
    public static async Task SaveAsync(string filePath, List<FuseColorDefinition> definitions)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(definitions, s_jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// 将新的校准颜色合并到现有列表中（同名覆盖，新名追加）。
    /// </summary>
    public static List<FuseColorDefinition> MergeCalibrated(
        List<FuseColorDefinition> existing,
        IEnumerable<FuseColorDefinition> calibrated)
    {
        var result = new List<FuseColorDefinition>(existing);

        foreach (var newDef in calibrated)
        {
            var existingIndex = result.FindIndex(
                d => string.Equals(d.ColorName, newDef.ColorName, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
                result[existingIndex] = newDef; // 覆盖
            else
                result.Add(newDef); // 追加
        }

        return result;
    }
}
