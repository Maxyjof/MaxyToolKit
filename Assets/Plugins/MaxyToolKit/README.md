# MaxyToolKit

MaxyToolKit是面向Unity2022.3.62f3及以上版本、URP和Windows平台的实用工具包，提供全局系统、事件总线、响应式数据、异步等待、存档、常用Unity组件、运行时动效与UI工具，以及PSB转UI预制体的编辑器工具链

框架以简单、明确和易于复用为原则，不引入大型架构层，功能按模块组织，可按项目需要选用

## 安装

将整个`MaxyToolKit`文件夹放入Unity工程的`Assets/Plugins`目录，等待Unity完成导入和脚本编译

本目录已经包含以下依赖：

- UniTask：异步等待
- DOTween：补间动画
- Easy Save 3：数据存档

如果工程中已经安装对应依赖，请只保留一份，并确认程序集引用指向同一版本

## 第一个示例

```csharp
using MaxyToolKit.Core;
using MaxyToolKit.Tool;
using UnityEngine;

public sealed class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        MSystemCenter.Register(new SaveSystem());
    }
}

public sealed class SaveSystem : ISystem
{
    public void Initialize()
    {
        MLog.Log("存档系统已初始化");
    }

    public void Shutdown()
    {
        MLog.Log("存档系统已关闭");
    }
}
```

完整教程请查看[教程目录](<Docs/Tutorials/README.md>)

## 示例场景

可运行示例位于[`Examples`](<Examples>)目录，共包含四个场景，分别演示事件总线与全局系统、响应式数据、异步等待与淡入淡出，以及常用工具和DOTween动画。每个场景由一个示例组件启动，界面在运行时创建，不依赖额外预制体

## 常用功能

| 模块 | 主要类型 | 用途 |
| --- | --- | --- |
| 全局系统 | `MSystemCenter`、`ISystem` | 注册、查询和关闭全局系统 |
| 事件总线 | `MEventBus` | 按消息类型发布和订阅事件 |
| 响应式数据 | `MProperty`、`MListProperty`、`MDictionaryProperty` | 值变化时自动通知界面或业务逻辑 |
| 异步 | `MTask` | 延时、下一帧和条件等待 |
| 存档 | `MSave` | 使用ES3将数据保存到持久化文件 |
| 日志 | `MLog` | 统一输出普通日志、警告和错误 |
| 工具 | `MTool` | 组件获取、层级处理、输入、角度、二维朝向、射线平面交点、轨迹计算和场景切换 |
| 界面淡入淡出 | `FadeEffectOverlay` | 使用Coroutine执行淡入淡出 |
| 跟随 | `Follow` | 让对象跟随目标的位置和旋转 |
| 检测 | `BoxDetection` | 执行非分配盒形碰撞检测 |
| 单例 | `MSingleton` | MonoBehaviour单实例生命周期 |
| 按钮反馈 | `MButtonFeedback` | 悬停放大、按下回弹、禁用压暗 |
| 面板过渡 | `MPanelTransition` | 面板开合动画，支持多种入场样式 |
| 数字滚动 | `MNumberText` | 数值变化时滚动显示，按decimal插值 |
| 补间工具 | `MTween` | 统一时长与缓动的DOTween封装，防泄漏 |
| 全屏转场 | `MTransition` | 遮罩式淡入淡出，跨场景复用 |
| UI工厂 | `MUIFactory` | 运行时创建Canvas、图片、文本和按钮 |
| 提示条 | `MToast` | 顶部短提示，自动排队复用画布 |
| 特效 | `MFloatingText`、`MConfetti`、`MTypewriter` | 飘字、彩带和打字机效果 |
| PSB转UI | `PSBToUIPrefab` | 把PSB图层结构生成为UI预制体 |

## 推荐使用顺序

1. 在启动对象中注册全局系统
2. 用`MEventBus`传递跨模块消息
3. 用`MProperty`保存需要通知界面的状态
4. 用`MSubscriptionBag`或`DisposeWith`管理订阅生命周期
5. 用`MTask`处理延时和条件等待
6. 用`MSave`保存需要跨场景或跨启动保留的数据
7. 新界面优先用`MUIFactory`搭临时层，动效统一走`MTween`

## 重要约定

- `MEventBus`是同步事件总线，默认在主线程使用
- 私有字段和私有事件使用下划线前缀，例如`_handlers`和`_subscriptions`
- 私有方法保持PascalCase，便于与Unity生命周期方法和公开方法保持一致
- 订阅方法返回`IDisposable`，对象销毁时建议调用`DisposeWith(gameObject)`
- 列表和字典属性的回调参数是只读接口，不要在回调中直接修改集合
- `MSystemCenter.Reset()`会逆序关闭系统，同时清空全局事件
- `FadeEffectOverlay`保留Coroutine实现，其他等待逻辑优先使用UniTask
- 所有DOTween补间先`DOKill`再创建，并设置`SetUpdate`与`SetLink(gameObject)`
- 一次性特效在补间结束后自行销毁节点，不留下空对象
- `MUIFactory`创建TextMeshPro时遵循先建未激活、配好字体文字、再激活的顺序
- 编辑器非播放模式下不创建补间，直接设置终态
- `DestroyInReleaseBuild`只在正式发布包中销毁自身
- 修改Unity脚本后，使用MaxyMCP的`request_recompile`刷新并编译，再检查编译错误

## 目录说明

- `Runtime/Core`：全局系统和统一入口
- `Runtime/Event`：事件总线和订阅管理
- `Runtime/Data`：响应式值、列表和字典
- `Runtime/Async`：UniTask辅助方法
- `Runtime/Save`：ES3存档封装
- `Runtime/Tool`：日志和常用工具
- `Runtime/MComponent`：可直接挂载的Unity组件
- `Runtime/Animation`：补间封装与全屏转场
- `Runtime/Effect`：飘字、彩带、打字机等运行时特效
- `Runtime/UI`：运行时UI工厂和提示条
- `Runtime/Integration`：第三方库集成扩展，具体集成模块使用`M`前缀避免与第三方类型冲突
- `Editor`：Unity编辑器菜单与工具窗口
- `Editor/PSB`：PSB转UI预制体的完整工具链
- `Examples`：可直接运行的框架功能示例场景和示例脚本
- `Docs`：注释规范、迁移说明和教程
- `Plugins`：随框架提供的第三方依赖

## 命名空间

- `MaxyToolKit.Core`：`MSystemCenter`、`ISystem`和`Maxy`
- `MaxyToolKit.Event`：`MEventBus`和订阅生命周期管理
- `MaxyToolKit.Data`：`MProperty`、`MListProperty`和`MDictionaryProperty`
- `MaxyToolKit.Async`：`MTask`
- `MaxyToolKit.Save`：`MSave`
- `MaxyToolKit.Tool`：`MTool`和`MLog`
- `MaxyToolKit.MComponent`：可挂载的Unity组件
- `MaxyToolKit.Animation`：`MTween`和`MTransition`
- `MaxyToolKit.Effect`：`MFloatingText`、`MConfetti`和`MTypewriter`
- `MaxyToolKit.UI`：`MUIFactory`和`MToast`
- `MaxyToolKit.Integration.MDOTween`：DOTween集成扩展
- `MaxyToolKit.Editor`：编辑器菜单、统计窗口与PSB工具

## 文档

- [快速开始](<Docs/Tutorials/01-快速开始.md>)
- [事件总线和全局系统](<Docs/Tutorials/02-事件总线和全局系统.md>)
- [MProperty响应式数据](<Docs/Tutorials/03-MProperty响应式数据.md>)
- [异步、动画和存档](<Docs/Tutorials/04-异步动画和存档.md>)
- [常见问题和建议](<Docs/Tutorials/05-常见问题和建议.md>)
- [PSB转UI预制体](<Docs/Tutorials/06-PSB转UI预制体.md>)
- [工具箱与项目经验](<Docs/Tutorials/07-工具箱与项目经验.md>)
- [运行时动效与转场](<Docs/Tutorials/08-运行时动效与转场.md>)
- [代码注释规范](<Docs/代码注释规范.md>)
- [MFramework迁移说明](<Docs/MFramework迁移说明.md>)
