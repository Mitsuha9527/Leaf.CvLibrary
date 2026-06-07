# Leaf.CvLibrary

[![NuGet](https://img.shields.io/nuget/v/Leaf.CvLibrary.svg)](https://www.nuget.org/packages/Leaf.CvLibrary)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

基于 OpenCvSharp 封装的图像处理与计算机视觉库，提供简洁易用的 API 和强大的图像处理功能。

## 📦 安装

### 通过 NuGet 包管理器安装

```bash
dotnet add package Leaf.CvLibrary
```

或在 Visual Studio 中使用包管理器控制台：

```powershell
Install-Package Leaf.CvLibrary
```

### 手动安装依赖

```bash
dotnet add package Leaf.CvCommon
dotnet add package OpenCvSharp4.Windows
```

## 🎯 功能特性

### 图像预处理
- **平滑处理** - 高斯模糊降噪
- **阈值分割** - 二值化图像分割
- **连通组件分析** - 区域检测与标记

### 图像匹配
- 模板匹配
- 特征点匹配

### 图像对齐
- 基于特征点的图像对齐
- 仿射变换对齐

### 图像测量
- 区域面积、周长计算
- 质心位置计算
- 边界框提取

### 图像绘制
- 绘制点、线、矩形、圆形
- 叠加文本和标记

### 通用工具
- 灰度值统计（最小值、最大值）
- 最大区域选择
- 像素格式转换

## 🚀 快速开始

### 示例 1：图像平滑与阈值分割

```csharp
using CvLibrary.OpenCV;
using OpenCvSharp;

// 加载图像
var image = Cv2.ImRead("input.jpg", ImreadModes.Grayscale);

// 平滑处理
var smoothed = CvTool.Smooth(image, ksize: 5);

// 阈值分割
var region = CvTool.Threshold(smoothed, threshold: 128);

// 保存结果
region.Mask.SaveImage("output.jpg");
```

### 示例 2：连通组件分析

```csharp
using CvLibrary.OpenCV;
using OpenCvSharp;

// 加载二值化图像
var image = Cv2.ImRead("binary.jpg", ImreadModes.Grayscale);
var region = new CvRegion(image);

// 连通组件分析
var components = CvTool.ConnectionComponents(region);

// 选择最大区域
var maxRegion = CvTool.SelecteMaxAreaRegion(components);

Console.WriteLine($"最大区域面积: {maxRegion.RegionProperties.Area}");
Console.WriteLine($"质心位置: ({maxRegion.RegionProperties.Centroid.X}, {maxRegion.RegionProperties.Centroid.Y})");
```

### 示例 3：灰度值统计

```csharp
using CvLibrary.OpenCV;
using OpenCvSharp;

var image = Cv2.ImRead("image.jpg", ImreadModes.Grayscale);

// 获取最小和最大灰度值
var (minValue, maxValue) = CvTool.GetMinMaxGrayValue(image);

Console.WriteLine($"最小灰度值: {minValue}");
Console.WriteLine($"最大灰度值: {maxValue}");
```

## 🏗️ 项目结构

```
Leaf.CvLibrary/
├── src/
│   ├── CvCommon/              # 通用基础元素库
│   ├── CvLibrary/             # 核心图像处理库
│   │   └── OpenCV/
│   │       ├── CvRegion.cs           # 区域定义
│   │       ├── CvRegionProperties.cs # 区域属性
│   │       ├── CvTool.Common.cs      # 通用工具
│   │       ├── CvTool.PreProcess.cs  # 预处理工具
│   │       ├── CvTool.Match.cs       # 匹配工具
│   │       ├── CvTool.Alignment.cs   # 对齐工具
│   │       ├── CvTool.Measure.cs     # 测量工具
│   │       ├── CvTool.Draw.cs        # 绘制工具
│   │       └── CvTool.Image.cs       # 图像工具
│   └── CvLibraryExtensions/   # 扩展功能库
├── README.md
├── LICENSE
└── THIRD-PARTY-NOTICES.txt
```

## 📋 系统要求

- **.NET 8.0** 或更高版本
- **支持平台**：
  - Windows (x64, AnyCPU)
  - Linux (需要 OpenCvSharp4.runtime.linux)
  - macOS (需要 OpenCvSharp4.runtime.osx)

## 🔧 依赖项

- **Leaf.CvCommon** (>= 1.0.2) - 基础数据结构
- **OpenCvSharp4.Windows** (>= 4.11.0) - OpenCV 包装库

## 📚 API 文档

### CvTool 类

| 方法 | 描述 |
|------|------|
| `Smooth(Mat, int)` | 高斯模糊平滑 |
| `Threshold(Mat, double, double)` | 阈值二值化 |
| `ConnectionComponents(CvRegion)` | 连通组件分析 |
| `GetMinMaxGrayValue(Mat)` | 获取灰度值范围 |
| `SelecteMaxAreaRegion(CvRegion)` | 选择最大区域 |

### CvRegion 类

表示图像中的一个区域，包含掩码和区域属性。

### CvRegionProperties 类

包含区域的统计属性：
- `Area` - 面积
- `Centroid` - 质心
- `BoundingBox` - 边界框
- `Label` - 标签

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建您的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 📝 版本历史

### v1.1.0 (最新版本)
- 重构模板匹配的功能代码，类似halcon的调用形式

### v1.0.3
- 改进性能和稳定性

### v1.0.2
- 新增功能和 Bug 修复

## 👤 作者

**Mitsuha9527**

- GitHub: [@Mitsuha9527](https://github.com/Mitsuha9527)
- 项目主页: [Leaf.CvLibrary](https://github.com/Mitsuha9527/Leaf.CvLibrary)

## 📄 许可证

本项目采用 **MIT 许可证** - 查看 [LICENSE](LICENSE) 文件了解详情。

本项目使用了以下第三方库：
- **OpenCvSharp** - Apache License 2.0（详见 [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)）

## 🔗 相关链接

- [GitHub 仓库](https://github.com/Mitsuha9527/Leaf.CvLibrary)
- [NuGet 包](https://www.nuget.org/packages/Leaf.CvLibrary)
- [问题反馈](https://github.com/Mitsuha9527/Leaf.CvLibrary/issues)
- [OpenCvSharp](https://github.com/shimat/opencvsharp)

## ⭐ 支持

如果这个项目对您有帮助，请给它一个星标 ⭐！

---

Made with ❤️ by Mitsuha9527
