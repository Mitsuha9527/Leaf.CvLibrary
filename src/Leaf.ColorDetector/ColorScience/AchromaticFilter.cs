using Leaf.ColorDetector.Models;

namespace Leaf.ColorDetector.ColorScience;

/// <summary>
/// 灰黑色中性色前置分流器。
/// <para>
/// 在完整 ΔE2000 色差匹配之前，用极低计算成本（两次数值比较）
/// 判断样本是否属于黑白灰色系。命中后仅对 Black/Gray 计算 ΔE2000，
/// 跳过其他候选颜色，提升检测效率。
/// </para>
/// </summary>
public static class AchromaticFilter
{
    /// <summary>
    /// 判断给定 Lab 值是否属于灰黑色系（中性色）。
    /// </summary>
    /// <param name="a">a* 色度分量（标准范围）</param>
    /// <param name="b">b* 色度分量（标准范围）</param>
    /// <param name="options">检测配置，为 null 时使用默认阈值</param>
    /// <returns>true 表示样本为灰黑系中性色</returns>
    public static bool IsAchromatic(double a, double b, ColorDetectorOptions? options = null)
    {
        var threshold = options?.AchromaticABThreshold ?? 6.0;
        return Math.Abs(a) < threshold && Math.Abs(b) < threshold;
    }

}
