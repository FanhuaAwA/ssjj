# SSJJ GameHelper

SSJJ GameHelper 是一个基于 Unity/Mono 的 C# 插件与注入器示例项目。当前仓库版本是纯反射实现，不依赖 MonoMod RuntimeDetour，也没有 hook 游戏方法。

本项目仅用于授权环境、本地调试和技术研究。请勿用于破坏线上公平性、违反服务条款或未授权场景。

## 项目结构

```text
.
├── SSJJPlugin/        # GameHelper 插件源码
│   ├── Main.cs        # 入口：SSJJPlugin.Loader.Load()
│   ├── PluginController.cs
│   ├── Systems/       # ESP、Aim、菜单、按键、调试模块
│   └── Utils/         # 反射与绘制工具
└── SSJJ-Injector/     # Mono 注入器源码和启动脚本
    ├── Program.cs
    ├── Injector.csproj
    ├── inject.cmd
    └── inject.bat
```

## 使用介绍

### 1. 准备依赖

仓库不包含游戏托管程序集、Unity 程序集和编译产物。构建 `SSJJPlugin` 前，需要把匹配当前游戏版本的依赖 DLL 放到仓库根目录下的 `lib/` 目录，例如：

- `Assembly-CSharp.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.InputLegacyModule.dll`
- `SSJJUserCmd_Library.dll`
- `SSJJBase_Library.dll`
- 其他项目文件中引用的 SSJJ/Unity 依赖

也可以在 MSBuild 时传入 `GameManagedDir` 指向你的依赖目录。

### 2. 编译插件

```powershell
MSBuild.exe .\SSJJPlugin\SSJJPlugin.csproj /p:Configuration=Release /p:Platform=x64
```

如果依赖不在 `.\lib`：

```powershell
MSBuild.exe .\SSJJPlugin\SSJJPlugin.csproj /p:Configuration=Release /p:Platform=x64 /p:GameManagedDir="D:\path\to\managed"
```

编译后会生成：

```text
SSJJPlugin\bin\x64\Release\GameHelper.dll
```

### 3. 编译注入器

```powershell
MSBuild.exe .\SSJJ-Injector\Injector.csproj /p:Configuration=Release /p:Platform=x64
```

输出位置：

```text
SSJJ-Injector\bin\Injector.exe
```

### 4. 放置 DLL 并启动

把编译好的 `GameHelper.dll` 放到 `SSJJ-Injector` 目录：

```text
SSJJ-Injector\GameHelper.dll
```

然后以管理员权限运行：

```text
SSJJ-Injector\inject.cmd
```

插件加载入口：

```text
SSJJPlugin.Loader.Load()
```

默认菜单热键是 `Home`。

## 项目介绍

`SSJJPlugin` 是一个 Unity Mono 插件。插件被加载后会创建一个 `GameObject`，挂载 `PluginController`，并通过 Unity 生命周期方法运行：

- `Start()` 初始化相机、上下文、菜单和功能模块。
- `Update()` 刷新玩家列表、处理按键绑定、计算辅助目标。
- `OnGUI()` 绘制菜单、ESP、FOV 圆和辅助线。

`SSJJ-Injector` 是一个独立的 .NET Framework x64 注入器。它查找目标 Mono 进程，定位 Mono 导出函数，然后调用目标进程中的 Mono API 加载 `GameHelper.dll` 并执行入口方法。

## 如何实现

### 插件侧

插件不直接引用游戏对象实例，而是通过 `ReflectionHelper` 查询和访问运行时对象：

- 查找 `Contexts.sharedInstance`
- 读取 `player.myPlayerEntity`
- 调用 `GetEntities()` 遍历玩家实体
- 读取队伍、名称、血量、死亡状态等信息
- 根据第三人称模型根节点和骨骼 Transform 计算屏幕坐标

功能模块分工：

- `EspSystem`：绘制方框、血条、名称、距离、骨骼和射线。
- `AimbotSystem`：筛选目标、计算 FOV 内最近目标和屏幕偏移。
- `MenuSystem`：使用 Unity IMGUI 绘制功能菜单。
- `KeyBinder`：处理菜单键和辅助键绑定。
- `DebugSystem`：提供运行时日志输出。

### 注入侧

注入器流程：

1. 查找游戏进程。
2. 枚举目标进程模块，定位 Mono 运行时模块。
3. 本地加载同路径 Mono 模块，计算导出函数偏移。
4. 在目标进程内写入 DLL 路径、命名空间、类名和方法名。
5. 构造 x64 shellcode，依次调用：
   - `mono_get_root_domain`
   - `mono_thread_attach`
   - `mono_assembly_open`
   - `mono_assembly_get_image`
   - `mono_class_from_name`
   - `mono_class_get_method_from_name`
   - `mono_runtime_invoke`
6. 执行 `SSJJPlugin.Loader.Load()`。

## 特点

- 纯反射实现，没有 MonoMod hook。
- 插件结构简单，模块边界清晰。
- 菜单、ESP、Aim、调试和按键绑定拆分为独立系统。
- 注入器不依赖外部注入框架，直接调用 Mono API。
- 项目输出为单个 `GameHelper.dll`，部署方式直观。

## 缺点

- 依赖游戏内部类型名、字段名和对象层级，游戏更新后容易失效。
- 纯反射只能读取和调用现有对象，无法稳定修改底层 `UserCmd` 发包逻辑。
- 不适合实现高精度 movement、air-strafe、silent aim 等需要 hook 或命令层修改的功能。
- 注入器需要管理员权限，并且依赖目标进程使用兼容的 Mono 运行时。
- 仓库不包含游戏依赖 DLL，首次构建需要手动准备匹配版本的依赖。
- 当前没有自动化测试、CI 和版本化配置文件。

## 构建环境

- Windows x64
- .NET Framework 4.8 Developer Pack
- Visual Studio / MSBuild
- 与目标游戏版本匹配的 Managed DLL

## 说明

本仓库默认保存源码，不提交 `bin/`、`obj/`、`*.dll`、`*.exe` 等编译产物和第三方依赖。需要发布时请在本地构建，并把生成的 `GameHelper.dll` 放入 `SSJJ-Injector` 目录。
