# WebUI 文件监控工具 - 配置说明

## 配置文件简介

本应用使用 `config.json` 文件来管理配置。配置文件位于应用程序所在的目录中。

## 配置文件位置

```
WebUIMonitor.exe 同级目录中的 config.json
```

## 配置文件格式

```json
{
  "MonitoringPath": "C:\\stable-diffusion-webui\\outputs",
  "AudioPath": "alarm.wav",
  "AutoDetect": true
}
```

## 配置参数说明

### MonitoringPath（监控路径）
- **类型**: 字符串
- **默认值**: `C:\stable-diffusion-webui\outputs`
- **说明**: 应用监控的根文件夹路径
  - 应用会自动在此路径下查找 `txt2img-images` 文件夹
  - 然后进一步查找当前日期的子文件夹（格式: YYYY-MM-DD）
  - 示例: `C:\stable-diffusion-webui\outputs\txt2img-images\2025-01-15`
- **编辑方法**: 直接修改此路径为你的 Stable Diffusion 输出目录

### AudioPath（警报音文件路径）
- **类型**: 字符串
- **默认值**: `alarm.wav`
- **说明**: 当文件数量未更新超过30秒时播放的音频文件
  - 使用相对路径（相对于应用所在目录）
  - 或使用绝对路径: `C:\\sounds\\alarm.wav`

### AutoDetect（自动检测）
- **类型**: 布尔值
- **默认值**: `true`
- **说明**: 
  - 如果设为 `true`: 如果 MonitoringPath 不存在，自动扫描系统磁盘寻找 Stable Diffusion
  - 如果设为 `false`: 直接使用 MonitoringPath，不存在则报错

## 使用示例

### 示例 1: 标准配置
```json
{
  "MonitoringPath": "C:\\stable-diffusion-webui\\outputs",
  "AudioPath": "alarm.wav",
  "AutoDetect": true
}
```

### 示例 2: 自定义路径
```json
{
  "MonitoringPath": "D:\\SD\\outputs",
  "AudioPath": "C:\\sounds\\alert.wav",
  "AutoDetect": false
}
```

### 示例 3: 网络路径
```json
{
  "MonitoringPath": "\\\\server\\shared\\sd-output",
  "AudioPath": "alarm.wav",
  "AutoDetect": false
}
```

## 修改配置步骤

1. **关闭应用程序**（可选，但建议关闭）
2. **打开 config.json 文件**
   - 使用记事本或任何文本编辑器
   - 右键 → 打开方式 → 记事本
3. **修改所需的配置参数**
   - 确保 JSON 格式正确（所有路径使用 `\\` 或 `/`）
   - 字符串值必须用双引号 `""` 括起来
4. **保存文件**
5. **重启应用程序**
   - 新配置会在应用启动时生效

## 配置文件验证

编辑后，确保：
- ✅ JSON 格式有效（可在线使用 JSON 验证工具）
- ✅ 路径使用正确的分隔符：
  - Windows: `C:\\folder\\path` 或 `C:/folder/path`
  - 网络路径: `\\\\server\\share`
- ✅ 布尔值为小写: `true` 或 `false`
- ✅ 字符串用双引号括起来

## 常见问题

### Q: 修改了路径但应用没有生效？
**A**: 需要重启应用程序。新配置在应用启动时读取。

### Q: 路径包含中文字符可以吗？
**A**: 可以。确保文件是以 UTF-8 编码保存的。

### Q: 如果删除了 config.json 会怎样？
**A**: 应用会自动创建一个新的 config.json，使用默认值或扫描磁盘自动检测。

### Q: AutoDetect 为 false 时，路径不存在会怎样？
**A**: 应用会提示错误或使用默认路径。建议使用 AutoDetect: true。

### Q: 可以使用相对路径吗？
**A**: MonitoringPath 建议使用绝对路径。AudioPath 可以使用相对路径（相对于应用目录）。

## 路径示例参考

### Windows 本地路径
```
C:\stable-diffusion-webui\outputs
D:\AI\SD\output
E:\Projects\stable-diffusion\outputs
```

### 网络共享路径
```
\\192.168.1.100\shared\sd-output
\\server-name\share\outputs
```

## 需要帮助？

1. 检查 JSON 格式是否正确
2. 验证路径确实存在
3. 确保有读取权限
4. 查看应用调试信息（如有）

---

**提示**: 配置文件支持热修改。你可以在应用运行时编辑配置文件，下次应用重启时会生效。
