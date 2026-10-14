# 三城交易时钟

Windows 桌面时钟，同时显示伦敦、纽约、上海时间，采用深色交易终端风格。

![图标](TradingClock-icon.png)

## 使用

下载本仓库 ZIP 并解压，双击 `TradingClock.exe`，无需安装。

- 自动依据 Windows 时区规则处理伦敦、纽约夏令时。
- 点击顶部“浮窗 / 窗口”切换模式，退出后保留位置和模式。
- 右键调节置顶、透明度、紧凑布局，以及开机自动启动。
- 自动启动在当前用户登录 Windows 后运行，无需管理员权限。
- 时间来自系统时钟，建议启用 Windows 自动设置时间。

本程序显示当地时间，不判断交易所开市或休市状态。启用自动启动后请保留程序路径；移动程序后可重新关闭、开启自动启动。

## 构建

在 Windows PowerShell 中运行 `./build.ps1`。使用 Windows 自带的 .NET Framework C# 编译器及 Windows Forms，不需要第三方依赖。

`TradingClock.cs` 为源码，`TradingClock.ico` 为程序图标，`make-icon.ps1` 可重新生成图标。
