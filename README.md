# WebUI 文件监控工具

极简的 WebUI 输出文件夹监控工具，用于检测生成任务是否卡顿。

## 功能

- 📁 自动检测 WebUI 输出文件夹位置（支持 C/D/E 盘）
- 📊 监控每日输出文件夹内的文件数量
- 🚨 文件数量不增加时自动触发警报
- 🔊 循环播放警报音乐（7 you.wav）
- 🎨 简洁的 UI 显示监控状态

## 项目结构

```
C#sd-webui_monitor_new/
├── FileMonitor.cs       # 文件监控核心逻辑
├── AudioPlayer.cs       # 音频播放控制
├── Config.cs            # 配置和路径检测
├── Form1.cs             # UI 窗体
├── Program.cs           # 程序入口
├── 7 you.wav           # 警报音乐
├── .gitignore          # Git 忽略文件
└── C#sd-webui_monitor_new.csproj
```

## 核心工作流程

```
Config 检测输出路径
    ↓
FileMonitor 监控文件数量 (3秒检查一次)
    ↓
isAlarm 状态标志 (文件不增加 → true)
    ↓
Form1 UI 显示状态 + AudioPlayer 播放/停止警报
```

## 状态机

| isAlarm | 含义 | UI 显示 | 音频 |
|---------|------|--------|------|
| false | 文件正常增长 | ✓ 正常 (绿色) | 停止 |
| true | 文件未增加 | ⚠️ 警报 (红色) | 播放 |

## 使用方法

### 编译
```bash
dotnet build
```

### 运行
```bash
dotnet run
```

### 发布
```bash
dotnet publish -c Release -o ./publish
```

## 配置

输出文件夹自动检测顺序：
1. `C:\stable-diffusion-webui\outputs`
2. `D:\stable-diffusion-webui\outputs`
3. `E:\stable-diffusion-webui\outputs`

当天监控的文件夹格式：`outputs\yyyy-MM-dd\`

## 代码统计

| 文件 | 行数 | 功能 |
|------|------|------|
| Form1.cs | 94 | UI 界面 |
| FileMonitor.cs | 64 | 核心监控 |
| AudioPlayer.cs | 40 | 音乐播放 |
| Config.cs | 29 | 路径配置 |
| Program.cs | 16 | 启动入口 |
| **总计** | **243** | - |

## 依赖

- .NET 10.0 (Windows Desktop)
- Windows Forms
- System.Media (SoundPlayer)

## 许可证

MIT

## 开发者备注

设计理念：
- ✅ 极简设计（仅 243 行代码）
- ✅ 单一职责原则
- ✅ 状态驱动架构（isAlarm 标志）
- ✅ 无外部依赖

如需修改警报检查间隔，编辑 `FileMonitor.cs` 中的 `Thread.Sleep(3000)` 值。
