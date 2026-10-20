# Oil Desk · 三城交易时钟

[中文](#使用) · [English](#english)

面向 WTI 原油期货 CL / MCL 的 Windows 桌面时钟。

![图标](TradingClock-icon.png)

## 使用

下载仓库 ZIP 并解压，双击 TradingClock.exe。右键窗口、托盘图标或顶部“设置”打开菜单。

- 标准竖版、紧凑竖版、横条；可贴到屏幕顶部。
- 浮窗 / 普通窗口、置顶、透明度、75%–175% 缩放，超出屏幕时自动缩小。
- 自选伦敦、纽约、上海、东京、香港、悉尼、芝加哥，自动适配夏令时。
- 位置锁定与鼠标穿透；双击托盘图标恢复。Ctrl+Alt+T 未被占用时也可恢复，穿透不会跨重启保留。
- 关闭窗口隐藏到托盘；彻底关闭请选择“退出程序”。
- 可选当前用户登录 Windows 后自动启动。移动程序后需重新开启此设置。
- 个人设置保存在 %LOCALAPPDATA%/TradingClock/settings.xml，不上传仓库。

## 重要时间点倒计时

底部显示 **5 分钟 K 线收盘倒计时**：自动对齐每小时 00、05、10…25、30、35 分钟，显示分秒和下一收盘的上海时间。到点自动进入下一根，无需开始、暂停或重置；最后 30 秒变为金色，不弹窗、不响铃。它按电脑系统时钟推算，不连接行情平台；休市时仍按时间边界计时，平台实际 K 线以行情软件为准。

底部显示四项，附上海时间对应的日期和时刻：

1. WTI 下一次开市或休市（依据常规时段）。
2. 下一次 EIA 原油库存报告公布。
3. 纽约工作日 09:00 关注时间。
4. 伦敦工作日 08:00 关注时间。

时间到达后自动显示下一次。没有事件提醒、通知弹窗或声音。伦敦与纽约关注时间跳过周末，但不是交易所开市时间，不自动排除节假日。

默认 WTI 常规时段为芝加哥时间周日至周四 17:00 开市、次日 16:00 收市，处理美国夏令时。另有 Brent 和自定义平台时段。未联网核验节假日、临时停市、到期合约及经纪商限制。特殊时段可手动覆盖；日期按开市日，例如 WTI 周一提前收市应覆盖周日开始的时段。Brent 临时夏令时调整也需按公告覆盖。

EIA 使用截至 2026-10-14 核对的 2026 年官方日程快照，含已公布延期，不自动获取临时修订或下一年日程。右键“编辑 EIA 公布时间”可增删改。其他旧版事件不显示。时钟依赖电脑系统时间，请启用 Windows 自动校时。

## 构建与验证

从托盘退出程序，Windows PowerShell 运行 ./build.ps1。使用 .NET Framework C# 编译器和 Windows Forms，无第三方包。

源码：TradingClock.cs（界面与设置）、OilEngine.cs（时区和日程）。make-icon.ps1 生成图标。
自检：TradingClock.exe --self-test <已存在的绝对输出目录>，输出结果与界面图片，不修改个人设置或开机启动。

## English

Oil Desk is a Windows desktop world clock for traders following WTI crude oil futures (CL / MCL). The application interface is currently in Chinese.

### Getting started

Download the repository ZIP, extract it, and launch TradingClock.exe. No installation is required. Right-click the window or tray icon, or click 设置 (Settings).

- Standard vertical, compact vertical, or horizontal strip layouts, with a top-of-screen placement option.
- Floating or standard windows, always on top, opacity, and 75%–175% scaling. The window scales down to fit the screen when needed.
- London, New York, Shanghai, Tokyo, Hong Kong, Sydney, and Chicago, using Windows time zones and DST rules.
- Position lock and mouse click-through. Double-click the tray icon to restore interaction; Ctrl+Alt+T also works if available. Click-through does not persist across restarts.
- Closing the window hides it to the tray. Choose 退出程序 (Exit application) to quit completely.
- Optional startup at Windows sign-in for the current user. Re-enable it after moving the executable.
- Personal settings are stored in %LOCALAPPDATA%/TradingClock/settings.xml, outside this repository.

### Key-time countdowns

The **five-minute candle-close countdown** automatically aligns to minutes 00, 05, 10, …25, 30, 35 of each hour. It shows minutes and seconds remaining, plus the next boundary in Shanghai time, and rolls into the next candle automatically. No manual controls are needed. The last 30 seconds turn gold, without sounds or notifications. It uses your computer's clock, not a broker feed, and continues during market closures; actual candle availability depends on your trading platform.

The bottom panel displays four countdowns, each with its target date and time in Shanghai:

1. The next WTI session opening or closure, based on the regular schedule.
2. The next EIA Weekly Petroleum Status Report release.
3. The next weekday at 09:00 New York time, as a focus point.
4. The next weekday at 08:00 London time, as a focus point.

After a target is reached, the next occurrence is shown. Event alerts, notification popups, and sounds have been removed. London and New York focus points skip weekends, not holidays; they are not exchange opening times.

The default WTI reference schedule opens at 17:00 Chicago time Sunday through Thursday and closes at 16:00 the next day, with US DST handled automatically. Brent and custom broker schedules are also available. Holidays, emergency halts, expiring contracts, and broker restrictions are not checked online. Enter overrides under 特殊休市 / 提前收市设置 (Special closures / early closes), using the session opening date. For a WTI early close on Monday, override Sunday's opening session. Temporary ICE DST adjustments also require manual overrides.

EIA dates are a static 2026 official schedule snapshot checked on October 14, 2026, including announced postponements. Updates and future years are not downloaded automatically. Use 编辑 EIA 公布时间 (Edit EIA release times) to add, edit, or delete dates. Other legacy events are not displayed. The clock uses your system time; enable Windows automatic time synchronization.

### Build and test

Exit the app from the tray, then run ./build.ps1 in Windows PowerShell. Requires the .NET Framework C# compiler and Windows Forms, with no third-party packages.

TradingClock.cs contains the UI and settings; OilEngine.cs contains time-zone and schedule logic. make-icon.ps1 generates the icon.

Run TradingClock.exe --self-test with an existing absolute output directory to generate test results and UI previews. Tests cover session boundaries, weekends, DST, overrides, EIA dates, focus countdown rollover, click-through recovery, settings serialization, and rendering. Self-tests do not modify personal settings or Windows startup registration.

## Official sources / 官方来源

Schedule sources checked on 2026-10-14:

- [CME WTI](https://www.cmegroup.com/education/courses/event-contracts-underlying-markets/wti-overview)
- [CME Micro WTI FAQ](https://www.cmegroup.com/education/articles-and-reports/micro-wti-crude-oil-futures-faq)
- [CME holiday hours](https://www.cmegroup.com/trading-hours.html)
- [ICE Brent](https://www.ice.com/products/219/Brent-Crude-Futures)
- [EIA release schedule](https://www.eia.gov/petroleum/supply/weekly/schedule.php)
