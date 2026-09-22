# MFramework迁移说明

MaxyToolKit是独立实现，不兼容旧MFramework的角色接口和类名。迁移时按功能替换，不要将两套框架同时复制到同一工程

| 旧概念 | MaxyToolKit写法 |
|---|---|
| `MArchitecture<T>` | 场景中的`GameBootstrap`和`MSystemCenter` |
| `RegisterSystem`和`GetSystem` | `MSystemCenter.Register`和`MSystemCenter.Get` |
| `MEventSystem.Global` | `Maxy.Events`或`MEventBus.Global` |
| `IUnRegister` | `IDisposable` |
| `MProperty<T>` | `MProperty<T>` |
| 架构事件 | 独立创建`new MEventBus()` |
| `BaseController`和`BaseView` | 普通`MonoBehaviour` |
| Command和Query | 直接调用系统方法 |
| `OverlayFadeEffect` | `FadeEffectOverlay` |
| `DestroyWhenRelease` | `DestroyInReleaseBuild` |

旧MFramework的全局静态状态、程序集和脚本不要复制到MaxyToolKit目录，避免类型冲突和重复初始化
