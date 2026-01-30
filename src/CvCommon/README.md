# Leaf.CvCommon

[![NuGet](https://img.shields.io/nuget/v/Leaf.CvCommon.svg)](https://www.nuget.org/packages/Leaf.CvCommon)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

图像/计算机视觉通用基础元素库，一般和 CvLibrary 联合使用。

## 📦 安装

通过 NuGet 包管理器安装：

```bash
dotnet add package Leaf.CvCommon
```

或在 Visual Studio 中使用包管理器控制台：

```powershell
Install-Package Leaf.CvCommon
```

## 🎯 功能特性

- 提供图像处理的通用基础元素
- 支持多种像素格式 (CvPixelFormat)
- 提供常用的图像数据结构：
  - `CvPoint` - 点坐标
  - `CvSize` - 尺寸
  - `CvRect` - 矩形区域
  - `CvImage` - 图像数据
  - `MarkPoint` - 标记点
  - `AlignmentResult` - 对齐结果
- 与 CvLibrary 无缝集成
- 基于 .NET Standard 2.0，跨平台兼容

## 🚀 快速开始

```csharp
using CvCommon;

// 创建点
var point = new CvPoint(100, 200);

// 创建尺寸
var size = new CvSize(640, 480);

// 创建矩形区域
var rect = new CvRect(0, 0, 640, 480);

// 使用像素格式
var format = CvPixelFormat.RGB24;
```

## 📋 系统要求

- .NET Standard 2.0 或更高版本
- 支持平台：
  - .NET Framework 4.6.1+
  - .NET Core 2.0+
  - .NET 5/6/7/8+
  - Mono
  - Xamarin
  - UWP

## 🔗 相关项目

- **CvLibrary** - 配合本库使用的计算机视觉处理库

## 📝 版本历史

### v1.0.1
- 当前稳定版本

## 👤 作者

**Mitsuha9527**

- GitHub: [@Mitsuha9527](https://github.com/Mitsuha9527)

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🔗 链接

- [GitHub 仓库](https://github.com/Mitsuha9527/Leaf.CvLibrary.git)
- [NuGet 包](https://www.nuget.org/packages/Leaf.CvCommon)
- [问题反馈](https://github.com/Mitsuha9527/KRX/issues)
