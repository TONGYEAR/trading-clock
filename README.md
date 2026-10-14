# Oil Desk · 三城交易时钟

[中文说明](#使用) · [English documentation](#english)

面向 WTI 原油期货 CL / MCL 的 Windows 桌面时钟。

![图标](TradingClock-icon.png)

## 使用

下载仓库 ZIP 并解压，双击 TradingClock.exe。右键窗口、托盘图标或顶部“设置”打开菜单。

- 标准竖版、紧凑竖版、横条布局，可贴到屏幕顶部。
- 浮窗 / 普通窗口、置顶、透明度、75%–175% 缩放。超出屏幕时自动缩小。
- 自选伦敦、纽约、上海、东京、香港、悉尼、芝加哥，自动处理夏令时。
- 位置锁定和鼠标穿透；双击托盘图标恢复操作。Ctrl+Alt+T 未被占用时也可恢复。穿透不会跨重启保留。
- 关闭窗口或顶部“−”隐藏到托盘，提醒继续运行。完全关闭请选择“退出程序”。
- 开机自动启动开关仅对当前用户生效；程序路径改变后需重新开启。
- 个人设置保存在 LocalAppData/TradingClock/settings.xml，不上传至仓库。

## 时段与提醒

默认 WTI CL / MCL：芝加哥时区周日至周四 17:00 开市，次日 16:00 收市，处理周末及美国夏令时。
另有 Brent 常规时段参考和 CFD 自定义时区、开收市时间、开市星期。Brent 临时夏令时调整需按 ICE 公告设置覆盖。

“常规时段内 / 常规休市段”不是行情连接状态。未联网核验节假日、临时停市、到期合约和经纪商限制。可通过“特殊休市 / 提前收市设置”覆盖；日期按时段开市日，例如 WTI 周一提前收市需覆盖周日开始的跨夜时段。

伦敦 / 纽约关注窗仅为各地工作日 08:00–17:00 的参考，重叠时显示标记，不代表交易所开盘或保证流动性。

- 开市 / 休市默认提前 5 分钟提醒，可选 1、5、10、15、30 分钟和声音开关。
- 事件支持增删改、清理过期、输入时区及单独提前量，可填写 API 库存、OPEC、CPI、非农等。
- 预载 2026 年剩余 EIA 日程，含截至 2026-10-14 官方公布的延期。2026-10-15 纽约 12:00 对应上海 2026-10-16 00:00。
- EIA 为静态日程快照，不自动延展到下一年或下载临时修订，请按公告修改。
- 程序运行时才提醒，不唤醒休眠电脑。恢复时仍在提醒窗口内的事件会提醒，过期事件不会集中弹出。通知受 Windows 勿扰设置影响。

## 官方来源（核对：2026-10-14）

- [CME WTI](https://www.cmegroup.com/education/courses/event-contracts-underlying-markets/wti-overview)
- [CME CL / MCL](https://www.cmegroup.com/cn-s/trading/energy/micro-wti-crude-oil-futures.html)
- [CME 节假日](https://www.cmegroup.com/trading-hours.html)
- [ICE Brent](https://www.ice.com/products/219/Brent-Crude-Futures)
- [EIA 日程](https://www.eia.gov/petroleum/supply/weekly/schedule.php)

## 构建与验证

从托盘退出时钟，在 Windows PowerShell 运行 ./build.ps1，使用 Windows .NET Framework C# 编译器，无第三方依赖。
源码：TradingClock.cs（界面、提醒、设置）及 OilEngine.cs（时段与事件）。make-icon.ps1 生成图标。

自检：TradingClock.exe --self-test <已存在的绝对输出目录>。覆盖维护边界、周末、春秋夏令时、特殊休市、提前收市、Brent 周日开盘、自定义时段、EIA 延期、提醒窗口、穿透恢复、设置序列化与界面渲染，不修改个人设置和开机启动。

## English

Oil Desk is a Windows desktop world clock designed for traders following WTI crude oil futures (CL / MCL). It combines local times, reference trading sessions, countdowns, and event reminders. The application interface is currently in Chinese; this section explains its controls in English.

### Getting started

Download the repository ZIP, extract it, and double-click `TradingClock.exe`. No installation is required. Right-click the window or system tray icon, or click **设置** (Settings) in the header.

- **Layouts:** standard vertical, compact vertical, and horizontal strip, with an option to place the strip at the top of the current screen.
- **Window controls:** floating or standard window, always on top, opacity, and 75%–175% scaling. The window scales down if necessary to fit the screen.
- **Cities:** London, New York, Shanghai, Tokyo, Hong Kong, Sydney, and Chicago, using Windows time-zone and daylight-saving rules.
- **Position lock and click-through:** double-click the system tray icon to restore mouse interaction. `Ctrl+Alt+T` also restores the window when that hotkey is available. Click-through is not retained after a restart.
- **System tray:** closing the window or clicking the header's minus sign hides it to the tray while reminders keep running. Choose **退出程序** (Exit application) to quit completely.
- **Start with Windows:** the optional startup setting applies to the current user at sign-in. If you move the executable, turn startup off and on again to update its path.
- **Settings:** personal preferences and events are stored in `%LOCALAPPDATA%\TradingClock\settings.xml`, outside the repository.

The clock follows your computer's system time. Enable automatic time synchronization in Windows for accurate display.

### Trading sessions and countdowns

The default WTI CL / MCL reference schedule uses Chicago time: sessions start at 17:00 Sunday through Thursday and end at 16:00 the following day. Weekends and US daylight saving are handled automatically.

The app also includes a Brent reference schedule and a custom schedule for broker-specific instruments, with configurable time zone, opening time, closing time, and opening weekdays. Temporary ICE daylight-saving adjustments must be entered as overrides.

Session indicators describe the configured schedule, not a live connection to an exchange. Exchange holidays, emergency halts, expiring contracts, and broker restrictions are not checked online. Use **特殊休市 / 提前收市设置** (Special closures / early closes) to override a session. The date is the session's opening date: for a WTI session that closes early on Monday, override the session that starts on Sunday.

The London and New York focus windows are simply weekdays from 08:00 to 17:00 in each city's local time. Their overlap is highlighted; these are not oil exchange opening times or guarantees of liquidity.

### Reminders and economic events

- Opening and closing alerts default to five minutes in advance. Available lead times are 1, 5, 10, 15, or 30 minutes, with optional sound.
- **事件提醒** (Event reminders) lets you add, edit, delete, and clear past events. Select the event's input time zone and an individual alert lead time. Examples include API inventories, OPEC announcements, CPI, and nonfarm payrolls; these events are entered manually.
- Remaining 2026 EIA Weekly Petroleum Status Report dates are preloaded from the official schedule checked on October 14, 2026, including announced postponements. The October 15 release at 12:00 New York time corresponds to October 16 at 00:00 in Shanghai.
- The EIA calendar is a static snapshot. It does not automatically fetch revisions or extend into the next year. Check the official schedule and edit events when necessary.
- Reminders require the app to be running and do not wake a sleeping computer. After resuming, events still within their reminder window can notify; stale events do not generate a backlog of alerts. Windows Do Not Disturb may suppress notification banners.

### Official references

Schedule information was checked on October 14, 2026:

- [CME WTI overview](https://www.cmegroup.com/education/courses/event-contracts-underlying-markets/wti-overview)
- [CME Micro WTI futures FAQ](https://www.cmegroup.com/education/articles-and-reports/micro-wti-crude-oil-futures-faq)
- [CME holiday and trading hours](https://www.cmegroup.com/trading-hours.html)
- [ICE Brent contract specifications](https://www.ice.com/products/219/Brent-Crude-Futures)
- [EIA release schedule](https://www.eia.gov/petroleum/supply/weekly/schedule.php)

### Building and testing

Requires Windows with the .NET Framework C# compiler and Windows Forms. No third-party packages are required. Exit the running app from its tray menu before building, then run in PowerShell:

```powershell
./build.ps1
```

Source files:

- `TradingClock.cs`: UI, notifications, tray controls, and settings.
- `OilEngine.cs`: trading sessions, time-zone conversion, and event schedule.
- `make-icon.ps1`: icon generation.

To run the built-in checks, provide an existing absolute output directory:

```powershell
Start-Process -FilePath .\TradingClock.exe -ArgumentList '--self-test C:\Temp\OilDeskTests' -Wait
```

Create that directory first or replace it with your own existing directory. Results and UI preview images are written there. Checks cover maintenance boundaries, weekends, spring and autumn DST, special closures, early closes, Brent's Sunday opening, custom sessions, EIA postponements, reminder windows, click-through recovery, settings serialization, and UI rendering. Self-tests do not modify personal settings or Windows startup registration.