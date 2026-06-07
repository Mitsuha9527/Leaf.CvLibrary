# NCC 模板匹配算法详解

## 目录
1. [架构总览](#1-架构总览)
2. [Phase 1：模型创建（离线预处理）](#2-phase-1模型创建)
3. [Phase 2：查找匹配（在线搜索）](#3-phase-2查找匹配)
4. [数据流全景图](#4-数据流全景图)
5. [全部参数说明](#5-全部参数说明)
6. [关键优化技术](#6-关键优化技术)
7. [边界情况处理](#7-边界情况处理)

---

## 1. 架构总览

```
┌─────────────────────────────────────────────────────┐
│                    CvMatchModel (abstract)           │
│  - 金字塔构建 / 旋转工具 / 亚像素精炼 / NMS / 阈值   │
└────────────────────┬────────────────────────────────┘
                     │ 继承
┌────────────────────┴────────────────────────────────┐
│                    CvNccModel (sealed)               │
│  - NCC 专用匹配逻辑                                   │
│  - 并行/串行角度扫描                                   │
│  - 金字塔级联搜索                                     │
└─────────────────────────────────────────────────────┘
```

**核心数据结构**：`Pyramid[level][angleIndex]` — 三维数组，每个元素是一个 `TemplateEntry`：
- `Image`：旋转后的模板灰度图
- `Mask`：有效像素掩码（255=有效，0=旋转产生的填充）
- `CenterOffset`：从 Image 左上角到模板内容中心的偏移
- `Angle`：该条目的角度

---

## 2. Phase 1：模型创建 `CvNccModel.Create(template, options)`

**目的**：将一张模板图预计算成多角度、多分辨率的金字塔数据结构，供后续反复搜索使用。

### 2.1 输入预处理

```
彩色图 ──→ Cv2.CvtColor(BGR2GRAY) ──→ 灰度图
                                        │
                          options.SmoothKernelSize != null ?
                          ├── Yes → GaussianBlur (降噪)
                          └── No  → 保持原样
```

### 2.2 金字塔构建

**自动层数计算** (`CalculateAutoLevels`)：
```
minSide = min(width, height)
levels = floor(log₂(minSide / 25))
levels = max(1, levels)          // 至少 1 层
if (顶层旋转后 < 35px) levels--  // 保守校验

例：147×148 → log₂(147/25)=2.56 → 2 层
    73×74   → log₂(73/25)=1.55  → 1 层
```

**逐层构建流程**：
```
for level = 0 to NumLevels-1:
    ┌─ 1. 生成本层角度列表 (GenerateAnglesForLevel)
    │      levelStep = baseStep × 2^level
    │      例：L0 step=1°→51angles, L1 step=2°→26angles
    │
    ├─ 2. 准备本层 mask 源
    │      L0: 全白 mask（模板矩形内全有效）
    │      L>0: 上层 mask 降采样（PyrDown）
    │
    ├─ 3. 对每个角度 a：
    │      RotateTemplateWithMask(当前层模板, mask源, angle)
    │      → 存储到 Pyramid[level][a]
    │
    └─ 4. 如果 level < NumLevels-1：
           降采样当前模板 → 传递给下一层
```

### 2.3 模板旋转 `RotateTemplateWithMask`

```
输入：模板图 (W×H)，全白 mask，角度 θ

0° 快捷路径：直接 clone（避免插值误差）

非 0°：
┌─ 1. 计算旋转矩阵 (GetRotationMatrix2D)
│      旋转中心 = 模板中心 (W/2, H/2)
│      缩放 = 1.0
│
├─ 2. 计算旋转后包围盒 (RotatedRect → BoundingRect)
│      例：147×148 旋转 45° → 包围盒 ~209×209
│
├─ 3. 调整平移，使旋转后内容居中
│      dx = bbox.Width/2 - center.X
│      dy = bbox.Height/2 - center.Y
│
├─ 4. WarpAffine (Cubic 插值, 黑色填充)
│      模板 → rotatedImage (Cubic: 高质量，减少模糊)
│      mask  → rotatedMask  (Linear: 够用，更快)
│
└─ 5. 输出 CenterOffset = (rotated.Width/2, rotated.Height/2)
       含义：从 rotatedImage 左上角到模板内容中心的偏移
```

**关键洞察**：`CenterOffset` 确保了从 `MatchTemplate` 返回的 topLeft 能正确计算模板中心：
```
center = topLeft + CenterOffset
       = (topLeft.x + rotated.W/2, topLeft.y + rotated.H/2)
```

---

## 3. Phase 2：查找匹配 `FindMatches(searchImage, options)`

### 3.1 入口流程

```
搜索图 (彩色/灰度)
  │
  ├─ 1. 参数 clamping
  │     effectiveAngleStart/Extent 限制在模型范围内
  │
  ├─ 2. ROI 裁剪 (如果指定)
  │     裁剪区域 = ROI + 模板尺寸 padding（确保边界也能匹配）
  │     记录 offsetX/Y 用于坐标还原
  │
  ├─ 3. 转灰度
  │
  ├─ 4. 构建搜索图金字塔 (BuildSearchPyramid)
  │     L0 = 原始灰度图
  │     L1 = PyrDown(L0) 或 GaussianBlur+Resize
  │     L2 = PyrDown(L1)
  │     ...
  │
  └─ 5. 金字塔级联搜索 → 返回匹配列表
```

### 3.2 金字塔级联搜索 `SearchPyramidCascade`

这是**核心搜索流程**，分为三个阶段：

```
                    ┌──────────────────────┐
                    │  Stage 1: 顶层粗搜索  │
                    │  GreedySuppression-   │
                    │  Search(L2)           │
                    │  搜索空间: 最小        │
                    │  角度数: 最少          │
                    │  阈值: 最宽松 (0.4)   │
                    └──────────┬───────────┘
                               │ candidates (≤4个)
                    ┌──────────┴───────────┐
                    │  Stage 2: 逐层精炼    │
                    │  for L1 → L0:        │
                    │    RefineCandidates() │
                    │    局部搜索 ±4-6px    │
                    │    局部角度 ±2-3步    │
                    │    NMS 合并重叠       │
                    └──────────┬───────────┘
                               │ candidates
                    ┌──────────┴───────────┐
                    │  Stage 3: 亚像素精炼  │
                    │  (可选)               │
                    │  位置: 抛物线拟合      │
                    │  角度: 分数插值        │
                    └──────────────────────┘
```

**阈值策略** (`GetAdaptiveThreshold`)：
```
L2 (最粗): max(0.4, minScore - 2×0.12) = max(0.4, minScore-0.24)
L1:        max(0.4, minScore - 1×0.12)
L0 (最细): max(0.4, minScore - 0×0.12) = max(0.4, minScore)

设计理念：粗层降采样会自然降低 NCC 质量，阈值需放宽以确保不丢失候选
```

### 3.3 顶层搜索 `GreedySuppressionSearch` — Stage 1

这是**性能最关键**的步骤。对每个有效角度，执行 NCC 匹配并找全局最优。

**首轮（并行）**：
```
Parallel.ForEach over all angles:
  ┌─ 检查：模板尺寸 ≤ 搜索图尺寸？
  ├─ CountNonZero 快速跳过：allowMask 是否全黑？
  ├─ Cv2.MatchTemplate(search, template, CCoeffNormed, mask)
  │    → 生成 resultMat (CV_32FC1)
  │    尺寸 = (searchW-templateW+1) × (searchH-templateH+1)
  │
  ├─ SanitizeResultMat：清洗 NaN/±Inf → 0
  │   来源：搜索图中零方差黑色区域（旋转产生的边框）
  │
  ├─ Cv2.MinMaxLoc(resultMat, mask=allowSub) → 全局最大值
  │
  └─ NormalizeScore(rawNCC) → [0, 1] 分数
     公式：(rawNCC + 1) / 2  （rawNCC ∈ [-1, 1]）
```

**后续轮（串行）**：同上，但 allowMask 已被之前找到的匹配涂黑，实现贪心抑制。

**候选数控制**：
- 单层金字塔：`maxCandidates = MaxMatches`（无冗余）
- 多层金字塔：`maxCandidates = min(20, MaxMatches + 3)`（为精炼留余量）

### 3.4 逐层精炼 `RefineCandidates` — Stage 2

将上层候选投影到当前层，进行局部搜索：

```
对每个上层候选：
  ┌─ 坐标投影：topLeft × 2 → currentLevel
  ├─ 搜索窗口：projectedTL ± margin
  │    L1: ±6px (粗层定位不准，需大窗口)
  │    L0: ±4px
  │
  ├─ 角度窗口：prevAngle ± angleHalfSpan × angleStep
  │    L1: ±3步 (粗层角度估计不准)
  │    L0: ±2步
  │
  ├─ 对局部窗口内每个候选角度：
  │    MatchTemplate(子搜索区域, 模板, CCoeffNormed, mask)
  │    MinMaxLoc → 最高分
  │    分数 ≥ 当前层阈值 → 加入候选池
  │
  └─ 全局 NMS (IoU > MaxOverlap 的只保留最高分)
```

**关键实现细节**：
- 搜索窗口用 `topLeft` 而非 `center` 定位（之前是 bug：用 center 定位导致真值在窗口外）
- 子搜索区域 = 模板尺寸 + 2×margin，结果矩阵仅为 (2×margin+1)² → 极快

### 3.5 亚像素精炼 — Stage 3（可选）

**位置精炼**（`RefineSubPixel`）：
```
在 3×3 邻域做 X/Y 方向抛物线拟合：
  dy = (v(-1) - v(+1)) / 2×(v(-1)+v(+1)-2v(0))  → clamp[-1,1]
  dx = 同理
  refinedPos = (x+dx, y+dy)
```

**角度精炼**（`RefineAngle`）：
```
取最佳角度 ±1 步的三个响应图：
  fraction = (s(-1) - s(+1)) / 2×(s(-1)+s(+1)-2×s(0))
  refinedAngle = angle + fraction × step
```

> ⚠️ **已知限制**：亚像素精炼在金字塔多级联用时会引入偏差（层间投影误差传导到精炼窗口），
> 建议 `NumLevels=1` 时使用，多层金字塔时保持 `SubPixelRefinement=false`。

---

## 4. 数据流全景图

```
Create(模板, NccModelOptions)                  FindMatches(搜索图, FindMatchesOptions)
══════════════════════════════                ══════════════════════════════════════

 模板图 (147×148)                               搜索图 (501×501)
      │                                              │
      ├─ 转灰度                                      ├─ ROI? → 裁剪 + 偏移量
      ├─ 计算层数 (auto: 2)                          ├─ 转灰度
      │                                              ├─ BuildSearchPyramid
      ├─ L0 (147×148):                               │   ├─ L0: 501×501
      │   ├─ 旋转 0°  → entry[0]                     │   └─ L1: 251×251
      │   ├─ 旋转 1°  → entry[1]
      │   ├─ ...                                     ├─ SearchPyramidCascade
      │   └─ 旋转 50° → entry[50]   (51 个)          │   │
      │                                              │   ├─ Stage 1: L1 顶层搜索
      ├─ 降采样 (PyrDown)                             │   │   ├─ 26 角度并行
      │                                              │   │   ├─ 每角度 MatchTemplate
      ├─ L1 (73×74):                                 │   │   └─ 选出 ≤4 个候选
      │   ├─ 旋转 0°  → entry[0]                     │   │
      │   ├─ 旋转 2°  → entry[1]                     │   ├─ Stage 2: L0 精炼
      │   ├─ ...                                     │   │   ├─ 每个候选局部搜索
      │   └─ 旋转 50° → entry[25]   (26 个)          │   │   ├─ NMS 去重
      │                                              │   │   └─ 保留 ≤1 个
      └─ 存储 Pyramid[2][]                           │   │
                                                      │   └─ Stage 3: 亚像素精炼 (skip)
                                                      │
                                                      └─ 返回 List<CvMatchResult>
                                                           Position, Angle, Score
```

---

## 5. 全部参数说明

### NccModelOptions（创建模型时指定，不可修改）

| 参数 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `AngleStart` | double | 0° | 搜索起始角度 |
| `AngleExtent` | double | 0° | 搜索角度范围。例：`AngleStart=-30, Extent=60` → -30°~30° |
| `AngleStep` | double? | 1° | L0 角度步长。null=自动。越细越准但越慢。上层的步长自动×2ⁿ |
| `NumLevels` | int? | auto | 金字塔层数。null=自动计算。1=单层（最准但最慢）|
| `Metric` | MatchMetric | UsePolarity | `UsePolarity`=正常 NCC, `IgnoreGlobalPolarity`=允许极性反转 |
| `SmoothKernelSize` | int? | null | 高斯平滑核大小（奇数≥3）。null=不平滑 |

### FindMatchesOptions（每次查找可不同）

| 参数 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `MinScore` | double | 0.7 | 最低分数阈值 [0,1]。越高越严格，越少误检 |
| `MaxMatches` | int | 1 | 最多返回匹配数 |
| `MaxOverlap` | double | 0.5 | NMS 抑制阈值 [0,1]。0=不抑制，1=完全抑制重叠 |
| `AngleStart` | double? | null | 覆盖模型的角度起始。null=使用模型值 |
| `AngleExtent` | double? | null | 覆盖模型的角度范围。null=使用模型值 |
| `SubPixelRefinement` | bool | true | 是否亚像素精炼。多层金字塔建议 false |
| `SearchRegion` | CvRect? | null | 搜索区域（源图坐标）。null=全图搜索 |

### 性能调优速查

| 场景 | 推荐配置 |
|------|---------|
| 追求极致速度 | `NumLevels=auto, AngleStep=2.0, MinScore=0.7, SubPixelRefinement=false` |
| 追求极致精度 | `NumLevels=1, AngleStep=0.5, MinScore=0.6, SubPixelRefinement=false` |
| 多目标检测 | `MaxMatches=5, MaxOverlap=0.3` |
| 极性反转容忍 | `Metric=IgnoreGlobalPolarity, MinScore=0.5` |
| 限定区域搜索 | `SearchRegion=new CvRect(x,y,w,h)` |

---

## 6. 关键优化技术

### 6.1 并行角度扫描
- **位置**：`ParallelScanAngles` — 顶层搜索的首轮
- **原理**：每个角度的 `MatchTemplate` + `MinMaxLoc` 完全独立，无共享可变状态
- **策略**：C# `Parallel.ForEach` 并行 + 关闭 OpenCV 内部多线程（避免过度订阅）
- **效果**：4-8 核 CPU 上 3-5× 加速

### 6.2 结果矩阵清洗 `SanitizeResultMat`
- **问题**：`MatchTemplate` + mask 在零方差区域（黑边）返回 ±Infinity 或 NaN
- **修复**：三阶段 OpenCV 操作（纯原生，无 C# 循环）
  1. `Compare(A, A, EQ)` → NaN 检测（NaN ≠ NaN）
  2. `Compare(A, 1.0, GT)` → +Inf 检测
  3. `Compare(A, -1.0, LT)` → -Inf 检测
  4. `SetTo(0, mask)` → 全部置零
- **为什么不用 clamp**：∞→1.0 会制造虚假完美匹配

### 6.3 CountNonZero 预筛
- **位置**：每次 `MatchTemplate` 前
- **原理**：如果 allowMask 子区域全黑（无合法位置），直接跳过昂贵的 MatchTemplate
- **效果**：消除末轮迭代中全部角度白费扫描的问题

### 6.4 MinMaxLoc 替代 C# 循环
- **原来**：双重 `for` 循环 `resultMat.At<float>(y,x)` — 每像素跨托管/原生边界
- **现在**：`Cv2.MinMaxLoc(resultMat, mask)` — 纯原生代码
- **效果**：数十倍加速（250K 像素 × 51 角度 = 12.7M 跨边界调用 → 51 次原生调用）

### 6.5 高质量插值
- **模板旋转**：`InterpolationFlags.Cubic`（原 Linear）
- **搜索图旋转**：`InterpolationFlags.Cubic`
- **降采样**：小尺寸用 `Cubic`，大尺寸用 `PyrDown`（5×5 高斯核）
- **效果**：消除 45° 时 1° 的角度偏差

### 6.6 自适应阈值
- **公式**：`threshold(level) = max(0.4, minScore - level × 0.12)`
- **设计**：越粗的层阈值越低（降采样会降低 NCC 质量）
- **注意**：`level` 是绝对层级（L0=0, L1=1, L2=2），不是相对层级

---

## 7. 边界情况处理

| 边界情况 | 处理方式 |
|---------|---------|
| 模板 > 搜索图 | `GetAnglesInRange` 中检查尺寸 → 跳过该角度 |
| 搜索图为彩色 | `FindMatches` 中 `CvtColor(BGR2GRAY)` 自动转换 |
| 模板为彩色 | `Create` 中自动转灰度 |
| 角度超出范围 | `FindMatches` 中 clamp 到模型范围 |
| 零方差区域（黑边） | `SanitizeResultMat` 清洗 NaN/±Inf |
| 已释放模型调用 | `ObjectDisposedException` |
| 空模板/搜索图 | `ArgumentException` |
| 边界像素（亚像素精炼） | 直接返回整数坐标，不做拟合 |
| 最顶层候选全部丢失 | 返回 `Array.Empty<CvMatchResult>()` |
| 多实例 + 同角度 | while 循环 + allowMask 涂黑实现贪心抑制 |
| 0° 旋转 | 特殊快捷路径：直接 clone，避免插值误差 |
