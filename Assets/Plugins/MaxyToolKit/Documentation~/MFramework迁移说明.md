# MFramework 迁移说明

MaxyToolKit 不兼容旧 MFramework 的角色接口和类名，迁移时建议按功能重写。

| 旧概念 | 新写法 |
|---|---|
| `MArchitecture<T>` | 场景中的 `GameBootstrap` + `MSystemCenter` |
| `RegisterSystem` / `GetSystem` | `MSystemCenter.Register` / `MSystemCenter.Get` |
| `MEventSystem.Global` | `Maxy.Events` |
| `IUnRegister` | `IDisposable` |
| `MProperty<T>` | `MProperty<T>` |
| 架构事件 | 独立创建 `new MEventBus()` |
| `BaseController` / `BaseView` | 普通 `MonoBehaviour` |
| Command / Query | 直接调用系统方法 |
| `OverlayFadeEffect` | `FadeEffectOverlay` |
| `DestroyWhenRelease` | `DestroyInReleaseBuild` |

不要把旧的 `MFramework` 文件复制到本目录。MaxyToolKit 是独立实现，避免两套全局静态状态同时运行。
