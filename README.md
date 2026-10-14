# Oil Desk · 三城交易时钟

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
