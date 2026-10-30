# 06PSB转UI预制体

## 1.这个工具解决什么问题

Unity 用 PSD Importer 导入 `.psb` 后，每个图层会变成一张精灵图，但把 PSB 拖进场景只能得到一堆散落的 SpriteRenderer，需要手工摆位置、手工搭 RectTransform 层级、手工反图层顺序

`PSB 转 UI 预制体` 直接从 PSB 的图层结构生成一个 UI 预制体：每个图层对应一个 `RectTransform + Image`，层级、位置、前后遮挡顺序、图层组都和 PSB 一致，省掉这套重复劳动

工具是可选的：生成的预制体只是普通 UI 预制体，之后怎么改都行，不会绑定任何运行时脚本，也不会影响现有流程

## 2.打开方式

三种入口，效果相同：

- 在 Project 窗口里右键一个 `.psb` 文件，选择 `MaxyToolKit/PSB 转 UI 预制体`
- 菜单栏 `MaxyToolKit/PSB 转 UI 预制体`

打开后可以先改配置、看预览树，再点底部的「生成预制体」

## 3.配置项

### 输出

- 输出目录：默认 `Assets/Prefabs/UI`，不存在会自动创建
- 预制体名字：默认取 PSB 文件名

### 画布

- 创建 Canvas：在根节点上补 `Canvas(ScreenSpaceOverlay) + CanvasScaler + GraphicRaycaster`，默认勾选
- 参考分辨率：和 CanvasScaler 的参考分辨率一致，只在「创建 Canvas」勾选或「根节点使用文档尺寸」取消时出现
- 根节点使用文档尺寸：勾选时按 PSB 文档像素 1:1 生成，推荐；取消时按参考分辨率整体等比缩放

### 图层顺序

PSB 里越靠上的图层越靠前，Unity UI 里越靠后的子节点越靠前，两者的层级方向是反的

- `PSB 上层在前`（默认）：自动按 PSB 习惯摆放，生成结果和 PSB 看到的一致
- `保持导入顺序`：完全照搬导入顺序，不再翻转，用于排查问题

### 隐藏图层

- `跳过不生成`：隐藏图层不产生任何节点
- `生成但未激活`（默认）：节点保留在层级里，默认未激活，需要时手动打开，适合美术在 PSB 里临时隐藏的底图
- `保持激活`：节点保持激活，只把透明度按 PSB 的图层不透明度写入

### 结构与组件

- 生成组层级：勾选时按 PSB 的图层组生成父子容器；配合「拍平组」可以把组内的图层直接挂到上级
- 写入不透明度：把 PSB 图层不透明度写到 Image 颜色上
- 图片响应点击：默认关闭，避免整屏底图挡住按钮

## 4.命名规则（可选）

命名规则默认**关闭**：不勾选时所有图层都按普通图片节点生成，工具只负责结构、位置和前后顺序

想按名字转换结构时，到「生成配置」里勾选「按命名生成组件（可选）」，规则表才会参与生成；没勾选时规则表整块置灰但仍可编辑，方便先把规则存好

规则按「名称匹配 → 附加组件或结构」的方式工作，一张表可以随时增删改，按顺序匹配，第一条命中的生效

### 命名格式约定：类型：内容

建议图层/组直接按「类型：内容」命名，冒号前面的类型决定生成什么结构，冒号后面的内容会写进文字里：

| 图层名 | 生成结果 |
| --- | --- |
| `按钮：开始游戏` | `Image + Button`，并补一个文字子节点「开始游戏」 |
| `文本：关卡目标` | 去掉图片，换成 `TextMeshProUGUI`，内容「关卡目标」 |
| `滚动视图：背包` | `ScrollView + Viewport + Content + 滚动条`，图层子节点挂到 Content 下 |
| `滑动窗口：卡牌` | 横向版滚动视图，带横向滚动条 |
| `滚动条：音量` | 滚动条结构：轨道 / 滑动区 / 滑块 |
| `开关：音乐` | 开关结构：背景 / 勾选标记 / 文字 |
| `滑动条：音量` | 滑动条结构：背景 / 填充 / 把手 |
| `输入框：昵称` | 输入框结构：文本区 / 占位文字 / 输入文本 |
| `下拉框：难度` | 下拉框结构：标题 / 箭头 / 模板 / 选项 / 滚动内容 |
| `页签：背包` | 页签组：ToggleGroup + 横排布局，子开关自动登记互斥 |
| `开关组：选项` | 开关组：ToggleGroup，子开关自动登记互斥 |
| `遮罩图：头像框` | 用该图片的形状裁剪子节点（Image + Mask） |
| `原图：背景` | 用 `RawImage` 显示精灵原始贴图 |
| `遮罩：列表` | 加 `RectMask2D` |
| `列表：奖励` / `网格：背包` | 加 `VerticalLayoutGroup` / `GridLayoutGroup` |
| `容器：弹窗` | 加 `CanvasGroup` |
| `跳过：参考图` | 整层不生成 |

冒号支持中文全角「：」和英文半角「:」，冒号后面为空时用去掉类型前缀后的名字

### 组件结构照着 Unity 默认 UI 搭

命中的结构不是随便挂个组件，而是按 Unity 菜单里创建 UI 时的默认层级来搭，方便直接沿用官方工作流：

```text
按钮：开始游戏        Image + Button
└── 文字              TextMeshProUGUI

滚动视图：背包        Image + ScrollRect
├── Viewport          RectMask2D
│   └── Content       RectTransform（图层的子节点会挂到这里）
└── Scrollbar Vertical  Image + Scrollbar
    └── Sliding Area
        └── Handle    Image

开关：音乐            Image + Toggle
├── Background        Image
│   └── 勾选          TextMeshProUGUI（作为 Toggle 的勾选图）
└── 文字              TextMeshProUGUI

滑动条：音量          Image + Slider
├── Background        Image
├── Fill Area
│   └── Fill          Image
└── Handle Slide Area
    └── Handle        Image

输入框：昵称          Image + TMP_InputField
└── Text Area         RectMask2D
    ├── Placeholder   TextMeshProUGUI
    └── Text          TextMeshProUGUI

下拉框：难度          Image + TMP_Dropdown
├── Label             TextMeshProUGUI（当前选项）
├── Arrow             Image（箭头占位）
└── Template          Image + ScrollRect（默认未激活）
    └── Viewport      RectMask2D
        └── Content
            └── Item  Image + Toggle
                └── Item Label  TextMeshProUGUI
```

### 匹配方式与生成结构

| 匹配方式 | 说明 |
| --- | --- |
| 开头匹配 | 名称以规则文本开头 |
| 结尾匹配 | 名称以规则文本结尾 |
| 包含 | 名称里包含规则文本 |
| 正则 | 名称整体符合正则表达式 |

| 生成结构 | 说明 |
| --- | --- |
| 仅图片 | 只保留 Image，不加组件 |
| 按钮 | `Image + Button`，可选文字子节点 |
| 文本占位 | 去掉图片，换成 `TextMeshProUGUI` |
| 滚动视图 / 滑动窗口 | Unity 默认滚动视图层级，竖向 / 横向 |
| 滚动条 / 开关 / 滑动条 / 输入框 | Unity 默认对应控件层级 |
| 竖排列表 / 横排列表 / 网格列表 | 加对应 LayoutGroup |
| 矩形遮罩 / 透明组 | 加 `RectMask2D` / `CanvasGroup` |
| 跳过不生成 | 命中后整层不生成 |

默认规则表就是上面那张「类型：内容」的表，可以直接改匹配文本、增删规则行；以后想加新结构，在枚举和生成实现里加一个分支即可，不影响已有规则

配置和规则表可以在窗口里点「保存当前配置」，下次打开自动载入（存在 EditorPrefs，不进版本库）

## 5.预览与排除

预览区按 PSB 的上下顺序列出将要生成的节点，显示尺寸、是否有图片、是否隐藏、命中的规则

勾选一行表示这次不生成该节点，取消勾选恢复；被规则或隐藏设置强制排除的节点会显示为灰色，勾选框只作状态展示

改完配置或换了 PSB 后，如果预览没刷新，点「重新扫描」

## 6.生成结果长什么样

```text
预制体根节点（Canvas + CanvasScaler + GraphicRaycaster）
├── 背景            Image
├── 面板            Image
├── 从选区          Image
├── 祝福            无组件容器（来自 PSB 图层组）
│   └── 图层 34      Image
└── 按钮            Image + Button
```

- 每个节点的 `RectTransform` 尺寸用导入后精灵的实际像素尺寸（已裁掉透明边）
- 位置按导入记录里图层的文档坐标换算到根节点中心，和 PSB 里的摆放一致
- 图层组没有像素，节点尺寸取子节点的包围盒

## 7.已知限制

- 需要 PSD Importer 导入，且精灵模式为 Multiple；如果 PSB 没导入任何精灵，窗口会提示先检查导入设置
- 同一 PSB 里重名图层依赖导入顺序匹配精灵，若图层数与导入记录数不一致，会在控制台给出提示并按名字尽力匹配
- 文本只是占位：PSB 里的文字图层本质是图片，工具只能生成 `TextMeshProUGUI` 让后续填字，不会做 OCR
- 九宫格、图集、字体、材质不在转换范围内，生成后按项目规范自行调整
- 滚动视图只搭基础结构（Viewport + Content + RectMask2D），列表项间距、滚动方向仍需按实际效果微调

## 8.相关文件

- 入口与配置：`Assets/Plugins/MaxyToolKit/Editor/PSB/PSBUIConfig.cs`
- PSB 图层结构解析：`Assets/Plugins/MaxyToolKit/Editor/PSB/PSBFileParser.cs`
- 导入记录读取：`Assets/Plugins/MaxyToolKit/Editor/PSB/PSBMetaReader.cs`
- 生成实现：`Assets/Plugins/MaxyToolKit/Editor/PSB/PSBToUIPrefabBuilder.cs`
- 窗口界面：`Assets/Plugins/MaxyToolKit/Editor/PSB/PSBToUIPrefabWindow.cs`
- 工具箱：`Assets/Plugins/MaxyToolKit/Editor/MaxyToolKitMenu.cs`（顶部菜单入口）
