using OpenCvSharp;

namespace Leaf.ColorDetector.ColorScience;

/// <summary>
/// 从 Lab 图像中提取鲁棒的颜色统计量。
/// <para>
/// OpenCvSharp 的 BGR→Lab 输出为 8bit 编码：L ∈ [0,255], a ∈ [0,255], b ∈ [0,255]。
/// 需要转换为标准 CIELab 范围：L* ∈ [0,100], a* ∈ [-128,127], b* ∈ [-128,127]。
/// </para>
/// </summary>
internal static class LabStatistics
{
    internal sealed class LabStatisticsDebugResult : IDisposable
    {
        public required Mat Step1InitialValidMask { get; init; }
        public required Mat Step2MadInlierMask { get; init; }
        public LabResult? Result { get; init; }

        public void Dispose()
        {
            Step1InitialValidMask.Dispose();
            Step2MadInlierMask.Dispose();
        }
    }

    /// <summary>
    /// Lab 统计量结果。
    /// </summary>
    internal readonly record struct LabResult(
        double L,
        double A,
        double B,
        int ValidCount,
        double Dispersion,
        bool IsSpatiallyConsistent);

    /// <summary>
    /// 从 Lab 图像中提取有效像素的鲁棒 Lab 统计量。
    /// <para>
    /// 流程：提取有效像素 → MAD 离群值剔除 → 中位数/截断均值 → 空间一致性检查。
    /// </para>
    /// </summary>
    /// <param name="labMat">Lab 格式图像（8bit，OpenCvSharp 编码）</param>
    /// <param name="validMask">有效像素掩码</param>
    /// <returns>统计量结果；像素不足时返回 null</returns>
    public static LabResult? ComputeRobustLab(Mat labMat, Mat validMask)
    {
        using var debug = ComputeRobustLabWithDebug(labMat, validMask);
        return debug.Result;
    }

    internal static LabStatisticsDebugResult ComputeRobustLabWithDebug(Mat labMat, Mat validMask)
    {
        var step1Mask = validMask.Clone();

        // 1. 提取有效像素的 Lab 值
        var indexer = labMat.GetGenericIndexer<Vec3b>();
        var maskIndexer = validMask.GetGenericIndexer<byte>();

        var capacity = labMat.Rows * labMat.Cols;
        var lValues = new List<double>(capacity);
        var aValues = new List<double>(capacity);
        var bValues = new List<double>(capacity);

        // 同时记录像素坐标用于空间一致性检查
        var positions = new List<(int Y, int X)>(capacity);

        for (var y = 0; y < labMat.Rows; y++)
        {
            for (var x = 0; x < labMat.Cols; x++)
            {
                if (maskIndexer[y, x] == 0)
                    continue;

                var pixel = indexer[y, x];

                // 8bit → 标准 CIELab 转换
                lValues.Add(pixel.Item0 * 100.0 / 255.0);
                aValues.Add(pixel.Item1 - 128.0);
                bValues.Add(pixel.Item2 - 128.0);
                positions.Add((y, x));
            }
        }

        if (lValues.Count == 0)
        {
            return new LabStatisticsDebugResult
            {
                Step1InitialValidMask = step1Mask,
                Step2MadInlierMask = new Mat(validMask.Rows, validMask.Cols, MatType.CV_8UC1, Scalar.All(0)),
                Result = null
            };
        }

        // 2. MAD 离群值剔除：基于 a* 和 b* 的联合色度距离
        var (filteredL, filteredA, filteredB, keptIndices) =
            RemoveOutliersByMad(lValues, aValues, bValues);

        var step2Mask = new Mat(validMask.Rows, validMask.Cols, MatType.CV_8UC1, Scalar.All(0));
        var step2MaskIndexer = step2Mask.GetGenericIndexer<byte>();
        foreach (var idx in keptIndices)
        {
            var (yy, xx) = positions[idx];
            step2MaskIndexer[yy, xx] = 255;
        }

        if (filteredL.Length == 0)
        {
            return new LabStatisticsDebugResult
            {
                Step1InitialValidMask = step1Mask,
                Step2MadInlierMask = step2Mask,
                Result = null
            };
        }

        // 3. 鲁棒统计量
        var lResult = TrimmedMean(filteredL, 0.10);
        var aResult = Median(filteredA);
        var bResult = Median(filteredB);

        // 4. 内部散布度（inlier 像素的平均色差，越小表示颜色越均匀）
        var dispersion = ComputeDispersion(filteredL, filteredA, filteredB, lResult, aResult, bResult);

        // 5. 空间一致性检查
        var isSpatiallyConsistent = CheckSpatialConsistency(
            labMat, validMask, positions, labMat.Rows, labMat.Cols);

        var result = new LabResult(lResult, aResult, bResult, filteredL.Length, dispersion, isSpatiallyConsistent);
        return new LabStatisticsDebugResult
        {
            Step1InitialValidMask = step1Mask,
            Step2MadInlierMask = step2Mask,
            Result = result
        };
    }

    /// <summary>
    /// 基于 MAD（中位绝对偏差）剔除色度空间中的离群像素。
    /// <para>
    /// 计算每个像素与中位色的色度距离，距离 > 3.5 × MAD_normalized 的像素被剔除。
    /// 与 FuseDetector.GetDepthValueByMad 的思路一致。
    /// </para>
    /// </summary>
    private static (double[] L, double[] A, double[] B, int[] KeptIndices) RemoveOutliersByMad(
        List<double> lValues, List<double> aValues, List<double> bValues)
    {
        var count = lValues.Count;
        if (count < 5) // 样本太少，不剔除
            return ([.. lValues], [.. aValues], [.. bValues], Enumerable.Range(0, count).ToArray());

        // 中位数
        var lArr = lValues.ToArray();
        var aArr = aValues.ToArray();
        var bArr = bValues.ToArray();

        var medL = Median((double[])lArr.Clone());
        var medA = Median((double[])aArr.Clone());
        var medB = Median((double[])bArr.Clone());

        // 计算每个像素到中位色的 ΔE76 距离（用简化公式，速度快）
        var distances = new double[count];
        for (var i = 0; i < count; i++)
        {
            var dL = lArr[i] - medL;
            var dA = aArr[i] - medA;
            var dB = bArr[i] - medB;
            distances[i] = Math.Sqrt(dL * dL + dA * dA + dB * dB);
        }

        // MAD
        var sortedDist = (double[])distances.Clone();
        Array.Sort(sortedDist);
        var medianDist = sortedDist[count / 2];

        var absDevs = new double[count];
        for (var i = 0; i < count; i++)
            absDevs[i] = Math.Abs(distances[i] - medianDist);
        Array.Sort(absDevs);
        var mad = absDevs[count / 2];

        // MAD 极小（颜色非常均匀），不需要剔除
        if (mad < 0.5)
            return (lArr, aArr, bArr, Enumerable.Range(0, count).ToArray());

        // 阈值 = 3.5 × 1.4826 × MAD（与正态分布的 3.5σ 等价）
        var threshold = 3.5 * 1.4826 * mad;

        var resultL = new List<double>(count);
        var resultA = new List<double>(count);
        var resultB = new List<double>(count);
        var keptIndices = new List<int>(count);

        for (var i = 0; i < count; i++)
        {
            if (distances[i] <= threshold)
            {
                resultL.Add(lArr[i]);
                resultA.Add(aArr[i]);
                resultB.Add(bArr[i]);
                keptIndices.Add(i);
            }
        }

        if (resultL.Count == 0)
            return (lArr, aArr, bArr, Enumerable.Range(0, count).ToArray()); // 回退：全部保留

        return ([.. resultL], [.. resultA], [.. resultB], [.. keptIndices]);
    }

    /// <summary>
    /// 计算 inlier 像素的内部散布度（平均 ΔE76 到中心的距离）。
    /// 用于校准时自动推算容差。
    /// </summary>
    private static double ComputeDispersion(
        double[] lValues, double[] aValues, double[] bValues,
        double centerL, double centerA, double centerB)
    {
        if (lValues.Length == 0) return 0;

        var sumDE = 0.0;
        for (var i = 0; i < lValues.Length; i++)
        {
            var dL = lValues[i] - centerL;
            var dA = aValues[i] - centerA;
            var dB = bValues[i] - centerB;
            sumDE += Math.Sqrt(dL * dL + dA * dA + dB * dB);
        }

        return sumDE / lValues.Length;
    }

    /// <summary>
    /// 空间一致性检查：将 ROI 分为 4 个象限，计算各象限 Lab 中心的最大色差。
    /// 如果色差过大，说明 ROI 中可能包含异质区域（背景/半插入/倾斜）。
    /// </summary>
    private static bool CheckSpatialConsistency(
        Mat labMat, Mat validMask,
        List<(int Y, int X)> positions,
        int height, int width)
    {
        if (positions.Count < 20) // 太少无法做空间分析
            return true;

        var midY = height / 2;
        var midX = width / 2;

        // 4 象限的 a*, b* 均值
        var quadrants = new (List<double> A, List<double> B)[4];
        for (var i = 0; i < 4; i++)
            quadrants[i] = (new List<double>(), new List<double>());

        var indexer = labMat.GetGenericIndexer<Vec3b>();
        var maskIndexer = validMask.GetGenericIndexer<byte>();

        foreach (var (y, x) in positions)
        {
            if (maskIndexer[y, x] == 0) continue;

            var qi = (y < midY ? 0 : 2) + (x < midX ? 0 : 1);
            var pixel = indexer[y, x];
            quadrants[qi].A.Add(pixel.Item1 - 128.0);
            quadrants[qi].B.Add(pixel.Item2 - 128.0);
        }

        // 至少 3 个象限有足够像素
        var validQuadrants = new List<(double A, double B)>();
        foreach (var (aList, bList) in quadrants)
        {
            if (aList.Count >= 5)
            {
                validQuadrants.Add((
                    aList.OrderBy(v => v).ElementAt(aList.Count / 2),
                    bList.OrderBy(v => v).ElementAt(bList.Count / 2)));
            }
        }

        if (validQuadrants.Count < 2)
            return true;

        // 任意两个象限间的色度距离的最大值
        var maxChromaDist = 0.0;
        for (var i = 0; i < validQuadrants.Count; i++)
        {
            for (var j = i + 1; j < validQuadrants.Count; j++)
            {
                var da = validQuadrants[i].A - validQuadrants[j].A;
                var db = validQuadrants[i].B - validQuadrants[j].B;
                maxChromaDist = Math.Max(maxChromaDist, Math.Sqrt(da * da + db * db));
            }
        }

        // 阈值：象限间色度差 > 15 认为空间不一致
        return maxChromaDist <= 15.0;
    }

    /// <summary>
    /// 截断均值：去掉两端指定百分比后取均值。
    /// </summary>
    private static double TrimmedMean(double[] values, double trimRatio)
    {
        Array.Sort(values);
        var count = values.Length;
        var skip = (int)(count * trimRatio);
        var start = skip;
        var end = count - skip;

        if (end <= start)
        {
            start = 0;
            end = count;
        }

        var sum = 0.0;
        for (var i = start; i < end; i++)
            sum += values[i];

        return sum / (end - start);
    }

    /// <summary>
    /// 中位数。
    /// </summary>
    private static double Median(double[] values)
    {
        Array.Sort(values);
        var count = values.Length;
        var mid = count / 2;
        return count % 2 == 0
            ? (values[mid - 1] + values[mid]) / 2.0
            : values[mid];
    }
}
