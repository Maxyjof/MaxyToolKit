# MaxyToolKit

MaxyToolKit是面向Unity2022.3.62f3及以上版本、URP和Windows平台的轻量开发工具包

设计目标只有三个：简单易懂、常用够用、复制即可开始使用

## 安装

将整个`MaxyToolKit`文件夹放入Unity工程的`Assets/Plugins`目录，然后等待Unity完成导入

本目录已经包含以下依赖：

- UniTask：异步等待
- DOTween：补间动画
- Easy Save 3：数据存档

如果工程中已经安装这些依赖，请保留一份即可，避免重复导入

## 第一个示例

```csharp
using MaxyToolKit;
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

更多完整示例请查看[教程目录](<Documentation~/Tutorials>)

## 常用功能

| 模块 | 主要类型 | 用途 |
| --- | --- | --- |
| 全局系统 | `MSystemCenter`、`ISystem` | 注册、查询和关闭全局系统 |
| 事件总线 | `MEventBus` | 按消息类型发布和订阅事件 |
| 响应式数据 | `MProperty`、`MListProperty`、`MDictionaryProperty` | 值变化时自动通知界面或业务逻辑 |
| 异步 | `MTask` | 延时、下一帧和条件等待 |
| 存档 | `MStorage` | 使用ES3保存、读取和删除数据 |
| 日志 | `MLog` | 统一输出普通日志、警告和错误 |
| 工具 | `MTool` | 组件获取、层级处理、随机和列表操作 |
| 界面 | `FadeEffectOverlay` | 使用Coroutine执行淡入淡出 |
| 跟随 | `Follow` | 让对象跟随目标的位置和旋转 |
| 检测 | `BoxDetection` | 执行非分配盒形碰撞检测 |

## 推荐使用顺序

1. 在启动对象中注册全局系统
2. 用`MEventBus`传递跨模块消息
3. 用`MProperty`保存需要通知界面的状态
4. 用`MSubscriptionBag`或`DisposeWith`管理订阅生命周期
5. 用`MTask`处理延时和条件等待
6. 用`MStorage`保存需要跨场景或跨启动保留的数据

## 重要约定

- `MEventBus`是同步事件总线，默认在主线程使用
- 订阅方法返回`IDisposable`，对象销毁时建议调用`DisposeWith(gameObject)`
- 列表和字典属性的回调参数是只读接口，不要在回调中直接修改集合
- `MSystemCenter.Reset()`会逆序关闭系统，同时清空全局事件
- `FadeEffectOverlay`保留Coroutine实现，其他等待逻辑优先使用UniTask
- `DestroyInReleaseBuild`只在正式发布包中销毁自身
- 修改Unity脚本后，使用MaxyMCP的`request_recompile`刷新并编译，再检查编译错误

## 目录说明

- `Runtime/Core`：全局系统和统一入口
- `Runtime/Events`：事件总线和订阅管理
- `Runtime/Data`：响应式值、列表和字典
- `Runtime/Async`：UniTask辅助方法
- `Runtime/Storage`：ES3存档封装
- `Runtime/Tools`：日志和常用工具
- `Runtime/Components`：可直接挂载的Unity组件
- `Runtime/Integrations`：第三方库集成扩展
- `Editor`：Unity编辑器菜单
- `Documentation~`：注释规范和教程
- `Plugins`：随框架提供的第三方依赖

## 文档

- [快速开始](<Documentation~/Tutorials/01-快速开始.md>)
- [事件总线和全局系统](<Documentation~/Tutorials/02-事件总线和全局系统.md>)
- [MProperty响应式数据](<Documentation~/Tutorials/03-MProperty响应式数据.md>)
- [异步、动画和存档](<Documentation~/Tutorials/04-异步动画和存档.md>)
- [常见问题和建议](<Documentation~/Tutorials/05-常见问题和建议.md>)
- [代码注释规范](<Documentation~/代码注释规范.md>)
