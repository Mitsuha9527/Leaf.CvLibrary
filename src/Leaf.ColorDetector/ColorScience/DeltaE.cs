namespace Leaf.ColorDetector.ColorScience;

/// <summary>
/// CIE ΔE2000 色差计算。
/// <para>
/// 该公式是 CIE 推荐的最新色差度量标准，比 ΔE76（简单欧氏距离）
/// 更符合人眼对颜色差异的感知，特别适合工业颜色检测。
/// </para>
/// <para>
/// 输入为标准 CIELab 值：L* ∈ [0,100]，a* ∈ [-128,127]，b* ∈ [-128,127]。
/// </para>
/// </summary>
public static class DeltaE
{
    private const double Deg2Rad = Math.PI / 180.0;
    private const double Rad2Deg = 180.0 / Math.PI;

    /// <summary>
    /// 计算两个 Lab 颜色之间的 ΔE2000 色差。
    /// </summary>
    /// <param name="l1">参考色 L*</param>
    /// <param name="a1">参考色 a*</param>
    /// <param name="b1">参考色 b*</param>
    /// <param name="l2">样本色 L*</param>
    /// <param name="a2">样本色 a*</param>
    /// <param name="b2">样本色 b*</param>
    /// <param name="kL">明度权重因子（默认 1.0）</param>
    /// <param name="kC">色度权重因子（默认 1.0）</param>
    /// <param name="kH">色相权重因子（默认 1.0）</param>
    /// <returns>ΔE2000 色差值</returns>
    public static double Calculate(
        double l1, double a1, double b1,
        double l2, double a2, double b2,
        double kL = 1.0, double kC = 1.0, double kH = 1.0)
    {
        // Step 1: 计算 C'ab 和 h'ab
        var c1 = Math.Sqrt(a1 * a1 + b1 * b1);
        var c2 = Math.Sqrt(a2 * a2 + b2 * b2);
        var cAvg = (c1 + c2) / 2.0;

        var cAvg7 = Math.Pow(cAvg, 7);
        var g = 0.5 * (1.0 - Math.Sqrt(cAvg7 / (cAvg7 + 6103515625.0))); // 25^7 = 6103515625

        var a1Prime = a1 * (1.0 + g);
        var a2Prime = a2 * (1.0 + g);

        var c1Prime = Math.Sqrt(a1Prime * a1Prime + b1 * b1);
        var c2Prime = Math.Sqrt(a2Prime * a2Prime + b2 * b2);

        var h1Prime = ComputeHPrime(a1Prime, b1);
        var h2Prime = ComputeHPrime(a2Prime, b2);

        // Step 2: 计算 ΔL', ΔC', ΔH'
        var deltaLPrime = l2 - l1;
        var deltaCPrime = c2Prime - c1Prime;

        double deltaHPrime;
        if (c1Prime * c2Prime == 0)
        {
            deltaHPrime = 0;
        }
        else
        {
            var dhPrime = h2Prime - h1Prime;
            if (dhPrime > 180) dhPrime -= 360;
            else if (dhPrime < -180) dhPrime += 360;
            deltaHPrime = 2.0 * Math.Sqrt(c1Prime * c2Prime) * Math.Sin(dhPrime * Deg2Rad / 2.0);
        }

        // Step 3: 计算 CIEDE2000 的加权函数
        var lAvgPrime = (l1 + l2) / 2.0;
        var cAvgPrime = (c1Prime + c2Prime) / 2.0;

        double hAvgPrime;
        if (c1Prime * c2Prime == 0)
        {
            hAvgPrime = h1Prime + h2Prime;
        }
        else if (Math.Abs(h1Prime - h2Prime) <= 180)
        {
            hAvgPrime = (h1Prime + h2Prime) / 2.0;
        }
        else if (h1Prime + h2Prime < 360)
        {
            hAvgPrime = (h1Prime + h2Prime + 360) / 2.0;
        }
        else
        {
            hAvgPrime = (h1Prime + h2Prime - 360) / 2.0;
        }

        var t = 1.0
            - 0.17 * Math.Cos((hAvgPrime - 30) * Deg2Rad)
            + 0.24 * Math.Cos(2 * hAvgPrime * Deg2Rad)
            + 0.32 * Math.Cos((3 * hAvgPrime + 6) * Deg2Rad)
            - 0.20 * Math.Cos((4 * hAvgPrime - 63) * Deg2Rad);

        var lAvgPrime50Sq = (lAvgPrime - 50) * (lAvgPrime - 50);
        var sL = 1.0 + 0.015 * lAvgPrime50Sq / Math.Sqrt(20 + lAvgPrime50Sq);
        var sC = 1.0 + 0.045 * cAvgPrime;
        var sH = 1.0 + 0.015 * cAvgPrime * t;

        var cAvgPrime7 = Math.Pow(cAvgPrime, 7);
        var rC = 2.0 * Math.Sqrt(cAvgPrime7 / (cAvgPrime7 + 6103515625.0));
        var deltaTheta = 30.0 * Math.Exp(-((hAvgPrime - 275) / 25.0) * ((hAvgPrime - 275) / 25.0));
        var rT = -Math.Sin(2 * deltaTheta * Deg2Rad) * rC;

        // Step 4: 最终 ΔE2000
        var termL = deltaLPrime / (kL * sL);
        var termC = deltaCPrime / (kC * sC);
        var termH = deltaHPrime / (kH * sH);

        return Math.Sqrt(
            termL * termL +
            termC * termC +
            termH * termH +
            rT * termC * termH);
    }

    private static double ComputeHPrime(double aPrime, double b)
    {
        if (Math.Abs(aPrime) < 1e-10 && Math.Abs(b) < 1e-10)
            return 0;

        var h = Math.Atan2(b, aPrime) * Rad2Deg;
        if (h < 0) h += 360;
        return h;
    }
}
