# WebUI 监控工具

轻量级的 Stable Diffusion WebUI 监控工具，实时监控系统资源与文件生成状态。

## 功能特性

- 📊 **系统监控**: 实时显示 GPU 显存、CPU、物理内存、虚拟内存占用
- 📁 **文件监控**: 自动监控输出文件夹，检测文件数量变化
- 🚨 **智能警报**: 30 秒内无新文件生成时触发循环音频警报
- ⚙️ **配置驱动**: 通过 `config.json` 灵活配置监控路径
- 🎨 **简洁 UI**: 暗色主题界面，清晰展示所有监控信息
- 🔄 **异步架构**: 后台线程更新数据，UI 完全不阻塞

## 项目结构

```
C#sd-webui_monitor_new/
├── Form1.cs                    # UI 界面与事件驱动更新 (181 行)
├── MonitoringService.cs        # 后台监控服务与数据聚合 (190 行)
├── ConfigManager.cs            # 配置管理 - 静态工具类 (26 行)
├── SystemMonitor.cs            # 系统资源监控（CPU/内存/GPU）(54 行)
├── FileMonitor.cs              # 文件数量监控与警报逻辑 (71 行)
├── GpuVramHelper.cs            # GPU 显存查询（PowerShell）(98 行)
├── AudioPlayer.cs              # 音频循环播放 (35 行)
├── Program.cs                  # 程序入口 (16 行)
├── config.json                 # 配置文件
├── alarm.wav                   # 警报音频
└── C#sd-webui_monitor_new.csproj
```

## 核心架构

```
ConfigManager (静态) ──读取配置──> MonitoringService
                                      ↓
                          ┌───────────┴───────────┐
                          ↓                       ↓
                  GetActualMonitorPath()     FileMonitor
                          ↓                       ↓
                  SystemMonitor ←────────────→ AudioPlayer
                          ↓
                  后台线程循环 (500ms)
                          ↓
              OnDataUpdated 事件 ──> Form1 UI 更新
```

### 关键设计

- **事件驱动**: `MonitoringService.OnDataUpdated` 事件推送数据到 UI，避免轮询阻塞
- **后台异步**: 所有资源查询在 `Task.Run` 线程池执行，UI 线程仅负责渲染
- **配置热更新**: 每 500ms 重新读取 `config.json`，路径变化立即生效
- **路径智能化**: 优先监控 `配置路径\yyyy-MM-dd` 子目录，不存在则回退到配置路径
- **极简抽象**: 删除中间管理层，单函数控制路径获取，静态配置管理

## 使用方法

### 1. 配置

编辑 `config.json`：
```json
{
  "MonitoringPath": "C:\\stable-diffusion-webui\\outputs",
  "AudioPath": "alarm.wav",
  "AutoDetect": true
}
```

### 2. 编译运行
```powershell
dotnet build -c Release
dotnet run
```

### 3. 发布独立可执行文件
```powershell
dotnet publish -c Release -o publish
```

发布后在 `publish` 文件夹中可找到可执行文件及所有依赖。

## 代码统计

| 文件 | 行数 | 功能 |
|------|------|------|
| MonitoringService.cs | 190 | 后台监控服务与数据聚合 |
| Form1.cs | 181 | UI 界面与事件驱动更新 |
| GpuVramHelper.cs | 98 | GPU 显存查询（PowerShell） |
| FileMonitor.cs | 71 | 文件数量监控与警报逻辑 |
| SystemMonitor.cs | 54 | 系统资源监控（CPU/内存/GPU）|
| AudioPlayer.cs | 35 | 音频循环播放 |
| ConfigManager.cs | 26 | 静态配置管理（2 个方法） |
| Program.cs | 16 | 程序入口 |
| **总计** | **671** | **8 个核心模块** |

**代码简化成果**:
- 删除 `MonitoringPathManager.cs` (28 行) - 逻辑合并到 `MonitoringService`
- 删除 `Config.cs` - 合并到 `ConfigManager.cs`
- `ConfigManager` 从 174 行压缩到 26 行 (85% 减少)
- `SystemMonitor` 从 147 行压缩到 54 行 (63% 减少)
- `Form1` 从 238 行压缩到 181 行 (24% 减少)
- **总代码量从 1052 行减少到 671 行 (36% 减少)**

## 监控逻辑

### 警报触发条件
- 文件数量 30 秒内未增加 → 触发警报
- 警报状态下循环播放音频，直到文件数量增加

### 路径优先级
1. 检查 `配置路径\yyyy-MM-dd` 是否存在
2. 存在 → 监控该日期子目录
3. 不存在 → 监控配置的基础路径

### 状态转换

| 前状态 | 文件变化 | 新状态 | UI | 音频 |
|--------|---------|--------|-----|------|
| 正常 | 文件增加 | 正常 | ✓ 正在出图 (绿色) | 停止 |
| 正常 | 30秒无变化 | 警报 | ⚠️ 已停止 (红色) | 循环播放 |
| 警报 | 文件增加 | 正常 | ✓ 正在出图 (绿色) | 停止 |

## 依赖

- .NET 10.0 (Windows Desktop)
- Windows Forms
- System.Media (SoundPlayer)
- System.Diagnostics (PerformanceCounter)
- System.Management (WMI)

## 技术亮点

- ✅ **异步非阻塞**: 所有耗时操作在后台线程执行
- ✅ **事件驱动**: UI 订阅数据更新事件，避免轮询
- ✅ **配置热更新**: 运行时修改 `config.json` 立即生效
- ✅ **精简代码**: 8 个模块共 671 行，职责清晰
- ✅ **零外部依赖**: 仅使用 .NET BCL
- ✅ **激进简化**: 36% 代码减少，删除所有中间抽象层

## 开发备注

**设计理念**:
- 单一职责原则（每个类只做一件事）
- 事件驱动架构（数据推送而非拉取）
- 配置即代码（所有行为由 `config.json` 控制）
- 极简主义（删除不必要的抽象层和防御性代码）

**性能优化**:
- 后台线程 500ms 采样间隔（平衡实时性与性能）
- UI 更新使用 `BeginInvoke`（异步避免死锁）
- 文件监控 3 秒检查间隔（减少磁盘 I/O）

**可扩展性**:
- 新增监控指标：在 `MonitoringData` 添加属性
- 新增警报条件：修改 `FileMonitor.CheckFileCount()`
- 自定义音频行为：扩展 `AudioPlayer` 类

## 许可证

MIT
