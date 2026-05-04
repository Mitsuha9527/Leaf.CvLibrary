using Leaf.ColorDetector.Models;

namespace Leaf.ColorDetector.Configs;

/// <summary>
/// 汽车保险丝标准颜色定义（依据 SAE J1284 / ISO 8820 色码标准）。
/// <para>
/// Lab 参考色为典型工业相机光照条件下的经验值，建议现场部署后
/// 使用 <see cref="Calibration.ColorCalibrator"/> 从实际样品重新校准。
/// </para>
/// </summary>
public static class StandardFuseColors
{
    /// <summary>
    /// 获取汽车保险丝标准颜色定义列表（Lab 参考色 + 默认容差）。
    /// </summary>
    public static List<FuseColorDefinition> GetDefaults() =>
    [
        // 1A - 黑色
        new FuseColorDefinition
        {
            ColorName = "Black",
            RatingLabel = "1A",
            RefL = 15, RefA = 0, RefB = 0,
            MaxDeltaE = 15
        },

        // 2A - 灰色
        new FuseColorDefinition
        {
            ColorName = "Gray",
            RatingLabel = "2A",
            RefL = 50, RefA = 0, RefB = 0,
            MaxDeltaE = 12
        },

        // 3A - 紫色
        new FuseColorDefinition
        {
            ColorName = "Violet",
            RatingLabel = "3A",
            RefL = 30, RefA = 30, RefB = -40,
            MaxDeltaE = 12
        },

        // 5A - 米色/棕褐色
        new FuseColorDefinition
        {
            ColorName = "Tan",
            RatingLabel = "5A",
            RefL = 70, RefA = 10, RefB = 30,
            MaxDeltaE = 12
        },

        // 7.5A - 棕色
        new FuseColorDefinition
        {
            ColorName = "Brown",
            RatingLabel = "7.5A",
            RefL = 35, RefA = 18, RefB = 25,
            MaxDeltaE = 12
        },

        // 10A - 红色
        new FuseColorDefinition
        {
            ColorName = "Red",
            RatingLabel = "10A",
            RefL = 45, RefA = 55, RefB = 35,
            MaxDeltaE = 12
        },

        // 15A - 蓝色（浅蓝）
        new FuseColorDefinition
        {
            ColorName = "Blue",
            RatingLabel = "15A",
            RefL = 45, RefA = -5, RefB = -40,
            MaxDeltaE = 12
        },

        // 20A - 黄色
        new FuseColorDefinition
        {
            ColorName = "Yellow",
            RatingLabel = "20A",
            RefL = 85, RefA = -5, RefB = 70,
            MaxDeltaE = 12
        },

        // 25A - 白色/透明
        new FuseColorDefinition
        {
            ColorName = "White",
            RatingLabel = "25A",
            RefL = 90, RefA = 0, RefB = 3,
            MaxDeltaE = 15
        },

        // 30A - 绿色
        new FuseColorDefinition
        {
            ColorName = "Green",
            RatingLabel = "30A",
            RefL = 50, RefA = -35, RefB = 25,
            MaxDeltaE = 12
        },

        // 35A - 深蓝/藏蓝
        new FuseColorDefinition
        {
            ColorName = "DarkBlue",
            RatingLabel = "35A",
            RefL = 20, RefA = 10, RefB = -40,
            MaxDeltaE = 12
        },

        // 40A - 橙色
        new FuseColorDefinition
        {
            ColorName = "Orange",
            RatingLabel = "40A",
            RefL = 65, RefA = 40, RefB = 60,
            MaxDeltaE = 12
        },

        // 30A(Maxi) - 粉色
        new FuseColorDefinition
        {
            ColorName = "Pink",
            RatingLabel = "30A(Maxi)",
            RefL = 70, RefA = 25, RefB = -5,
            MaxDeltaE = 12
        },
    ];
}
