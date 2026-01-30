# Leaf.CvLibraryExtensions

[![NuGet](https://img.shields.io/nuget/v/Leaf.CvLibraryExtensions.svg)](https://www.nuget.org/packages/Leaf.CvLibraryExtensions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-512BD4)](https://dotnet.microsoft.com/)

基于 Leaf.CvLibrary 的 WPF 扩展功能库，提供 OpenCV Mat 与 WPF 图像格式的高效转换和绘制扩展方法。

## 📦 安装

### 通过 NuGet 包管理器安装

```bash
dotnet add package Leaf.CvLibraryExtensions
```

或在 Visual Studio 中使用包管理器控制台：

```powershell
Install-Package Leaf.CvLibraryExtensions
```

### 手动安装依赖

```bash
dotnet add package Leaf.CvLibrary
```

## 🎯 功能特性

### Mat 转换扩展
- **BitmapFrame 转换** - 高效将 Mat 转换为 BitmapFrame
- **WriteableBitmap 转换** - 最高性能的 WPF 图像转换
- **异步转换支持** - 提供异步转换方法避免 UI 阻塞
- **线程安全** - 自动处理 Dispatcher 调度

### 绘制扩展
- **结果 ROI 绘制** - 快速绘制带状态的感兴趣区域（成功/失败）

## 🚀 快速开始

### 示例 1：Mat 转换为 BitmapFrame

```csharp
using CvLibraryExtensions.OpenCV;
using OpenCvSharp;

// 加载图像
var mat = Cv2.ImRead("image.jpg");

// 转换为 BitmapFrame（同步）
var bitmapFrame = mat.ToBitmapFrame();
if (bitmapFrame != null)
{
    // 绑定到 WPF Image 控件
    myImage.Source = bitmapFrame;
}
```

### 示例 2：异步转换为 WriteableBitmap

```csharp
using CvLibraryExtensions.OpenCV;
using OpenCvSharp;

// 异步加载并转换
var mat = await Task.Run(() => Cv2.ImRead("image.jpg"));
var writeableBitmap = await mat.ToWriteableBitmapAsync();

if (writeableBitmap != null)
{
    // 绑定到 WPF Image 控件
    myImage.Source = writeableBitmap;
}
```

### 示例 3：绘制检测结果 ROI

```csharp
using CvLibraryExtensions.OpenCV;
using CvCommon;
using OpenCvSharp;

var mat = Cv2.ImRead("image.jpg");
var roi = new CvRect(100, 100, 200, 150);
bool detectionSuccess = true;

// 绘制结果 ROI（成功为绿色，失败为红色）
mat.DrawResultROI(roi, detectionSuccess, thickness: 3);

// 显示结果
var bitmapFrame = mat.ToBitmapFrame();
myImage.Source = bitmapFrame;
```

## 🏗️ API 文档

### MatConverterExtensions 类

| 方法 | 描述 |
|------|------|
| `ToBitmapFrame(Mat)` | 同步转换 Mat 为 BitmapFrame |
| `ToBitmapFrameAsync(Mat)` | 异步转换 Mat 为 BitmapFrame |
| `ToWriteableBitmap(Mat)` | 同步转换 Mat 为 WriteableBitmap（最高性能） |
| `ToWriteableBitmapAsync(Mat)` | 异步转换 Mat 为 WriteableBitmap |

**特点：**
- 避免使用内存流，直接复制像素数据，性能更高
- 自动处理 WPF Dispatcher 线程调度
- 支持多种像素格式（Bgr24, Bgra32, Gray8 等）
- 返回线程安全的冻结位图

### DrawROIExtensions 类

| 方法 | 描述 |
|------|------|
| `DrawResultROI(Mat, CvRect, bool, int)` | 绘制带状态的感兴趣区域 |

**参数：**
- `mat` - 目标图像
- `roi` - 感兴趣区域
- `isOK` - 状态（true=成功/绿色，false=失败/红色）
- `thickness` - 线条粗细（默认 2）

## 📋 系统要求

- **.NET 8.0 for Windows** 或更高版本
- **WPF 支持** - 仅支持 Windows 平台
- **支持平台**：
  - Windows 10/11 (x64, AnyCPU)

## 🔧 依赖项

- **Leaf.CvLibrary** (>= 1.0.5) - 核心图像处理库
- **OpenCvSharp4.Windows** - 通过 CvLibrary 间接依赖

## 💡 最佳实践

### 1. 异步转换避免 UI 阻塞

```csharp
// 推荐：在后台线程处理图像，异步转换到 UI
var processedMat = await Task.Run(() => ProcessImage(sourceMat));
var bitmap = await processedMat.ToWriteableBitmapAsync();
myImage.Source = bitmap;
```

### 2. 选择合适的转换方法

- **BitmapFrame** - 通用选择，适合大多数场景
- **WriteableBitmap** - 需要频繁更新图像时使用，性能最高

### 3. 线程安全

扩展方法会自动处理 Dispatcher 调度，无需手动调用 `Dispatcher.Invoke`。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建您的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 📝 版本历史

### v1.0.2 (当前版本)
- 当前稳定版本

### v1.0.1
- 改进性能和稳定性

### v1.0.0
- 初始发布版本

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
- [问题反馈](https://github.com/Mitsuha9527/Leaf.CvLibrary/issues)
- 
## ⭐ 支持

如果这个项目对您有帮助，请给它一个星标 ⭐！

---

Made with ❤️ by Mitsuha9527
