# MaxyToolKit示例

这里的示例场景用于快速了解MaxyToolKit的核心用法。每个场景都只依赖一个根对象和一份示例脚本，运行时自动创建简单界面，不需要额外预制体

## 场景列表

| 场景 | 演示内容 |
| --- | --- |
| `01-事件总线与全局系统` | 使用`MSystemCenter`注册全局系统，使用`MEventBus`发布和订阅消息 |
| `02-MProperty响应式数据` | 使用`MProperty`、`MListProperty`和`MDictionaryProperty`驱动界面更新，并演示ES3存档 |
| `03-异步等待与淡入淡出` | 使用`MTask`执行异步等待，使用`FadeEffectOverlay`完成界面淡入淡出 |
| `04-常用工具与动画` | 使用`MTool`计算对象位置，使用DOTween执行移动和旋转动画 |

## 使用方式

1.在Unity项目窗口中打开本目录下的任意场景
2.点击运行，按场景中的按钮观察数据、事件或动画变化
3.阅读`Scripts/MaxyToolKitExampleScenes.cs`，将需要的代码复制到自己的业务脚本中

示例代码故意保持直接和集中，适合先理解框架，再按项目需要拆分为自己的系统、界面和数据模块

