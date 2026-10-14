using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32;

public class TradingClock : Form {
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int m, IntPtr w, IntPtr l);
    readonly Timer timer = new Timer();
    readonly string[] names = { "伦敦", "纽约", "上海" };
    readonly string[] codes = { "LONDON", "NEW YORK", "SHANGHAI" };
    readonly TimeZoneInfo[] zones = {
        TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"),
        TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
        TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") };
    readonly Color bg = Color.FromArgb(14,20,29), muted = Color.FromArgb(123,143,163), green = Color.FromArgb(50,229,177);
    bool compact;
    bool floating = true;
    bool testing;
    readonly string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TradingClock", "position.txt");
    readonly ContextMenuStrip menu = new ContextMenuStrip();
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string StartupName = "ThreeCityTradingClock";
    static string StartupCommand { get { return "\"" + Application.ExecutablePath + "\""; } }
    static bool StartupEnabled() {
        using(var key = Registry.CurrentUser.OpenSubKey(RunKey))
            return key != null && string.Equals(key.GetValue(StartupName) as string, StartupCommand, StringComparison.OrdinalIgnoreCase);
    }
    static void SetStartup(bool enabled) {
        using(var key = Registry.CurrentUser.CreateSubKey(RunKey)) {
            if(enabled) key.SetValue(StartupName, StartupCommand, RegistryValueKind.String);
            else key.DeleteValue(StartupName, false);
        }
        if(StartupEnabled() != enabled) throw new IOException("启动设置未能保存。");
    }
    public TradingClock(bool testMode = false) {
        testing = testMode;
        Text = "Trading Clock · 三城交易时钟";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        FormBorderStyle = FormBorderStyle.None;
        BackColor = bg; ForeColor = Color.White; TopMost = true;
        DoubleBuffered = true; AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(384, 294); StartPosition = FormStartPosition.Manual;
        Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - Width - 30, 80);
        try { string[] p = File.ReadAllText(settings).Split(','); Location = new Point(int.Parse(p[0]), int.Parse(p[1])); if(p.Length > 2) SetFloating(bool.Parse(p[2])); } catch { }
        bool visible = false;
        foreach(Screen s in Screen.AllScreens) if(s.WorkingArea.IntersectsWith(new Rectangle(Location, Size))) visible = true;
        if(!visible) Location = Screen.PrimaryScreen.WorkingArea.Location;
        var pin = new ToolStripMenuItem("始终置顶", null, delegate { TopMost = !TopMost; Invalidate(); });
        pin.Checked = true;
        menu.Opening += delegate { pin.Checked = TopMost; };
        menu.Items.Add(pin);
        var floatItem = new ToolStripMenuItem("桌面浮窗", null, delegate { SetFloating(true); });
        var windowItem = new ToolStripMenuItem("普通窗口（不置顶）", null, delegate { SetFloating(false); });
        menu.Items.Insert(0, floatItem);
        menu.Items.Insert(1, windowItem);
        menu.Items.Insert(2, new ToolStripSeparator());
        menu.Opening += delegate { floatItem.Checked = floating; windowItem.Checked = !floating; };
        menu.Items.Add("切换紧凑 / 标准布局", null, delegate { compact = !compact; ClientSize = new Size(384, compact ? 210 : 294); Invalidate(); });
        var opacity = new ToolStripMenuItem("透明度");
        foreach(int percent in new int[] {100,90,80,70}) { int value = percent; opacity.DropDownItems.Add(value + "%", null, delegate { Opacity = value / 100.0; }); }
        menu.Items.Add(opacity);
        menu.Items.Add("最小化到任务栏", null, delegate { WindowState = FormWindowState.Minimized; });
        var startup = new ToolStripMenuItem("开机自动启动（登录 Windows 后）");
        startup.Click += delegate {
            try { SetStartup(!StartupEnabled()); startup.Checked = StartupEnabled(); }
            catch(Exception ex) { MessageBox.Show(this, "无法修改开机启动设置：" + ex.Message, "三城交易时钟", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        menu.Opening += delegate { try { startup.Checked = StartupEnabled(); startup.Enabled = true; } catch { startup.Checked = false; startup.Enabled = false; } };
        menu.Items.Add(startup);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, delegate { Close(); });
        ContextMenuStrip = menu;
        timer.Interval = 200; timer.Tick += delegate { Invalidate(); }; timer.Start();
        FormClosed += delegate { timer.Dispose(); SaveSettings(); };
        MouseDown += delegate(object sender, MouseEventArgs e) {
            if(e.Button != MouseButtons.Left) return;
            if(e.Y < 39 && e.X >= 238 && e.X <= 318) { SetFloating(!floating); return; }
            if(e.Y < 39 && e.X > 349) { Close(); return; }
            if(e.Y < 39 && e.X > 318) { WindowState = FormWindowState.Minimized; return; }
            ReleaseCapture(); SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
        };
        KeyDown += delegate(object sender, KeyEventArgs e) { if(e.KeyCode == Keys.Escape) Close(); };
    }
    void SaveSettings() {
        if(testing) return;
        try { Directory.CreateDirectory(Path.GetDirectoryName(settings)); Point p = WindowState == FormWindowState.Normal ? Location : RestoreBounds.Location; File.WriteAllText(settings, p.X + "," + p.Y + "," + floating); } catch { }
    }
    void SetFloating(bool value) {
        floating = value;
        FormBorderStyle = value ? FormBorderStyle.None : FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        TopMost = value;
        ClientSize = new Size(384, compact ? 210 : 294);
        Invalidate();
        SaveSettings();
    }
    void Label(Graphics g, string text, string family, float size, FontStyle style, Color color, float x, float y) {
        using(var f = new Font(family, size, style, GraphicsUnit.Pixel))
        using(var b = new SolidBrush(color)) g.DrawString(text, f, b, x, y);
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e); Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using(var p = new Pen(Color.FromArgb(43,60,75))) g.DrawRectangle(p, 0, 0, ClientSize.Width-1, ClientSize.Height-1);
        using(var b = new SolidBrush(green)) g.FillEllipse(b, 17, 18, 6, 6);
        Label(g,"GLOBAL / CLOCK", "Segoe UI", 13, FontStyle.Bold, Color.FromArgb(211,223,235), 30, 11);
        Label(g,floating ? "浮窗 ▾" : "窗口 ▾", "Microsoft YaHei UI", 12, FontStyle.Regular, green, 252, 11);
        Label(g,"−", "Segoe UI", 19, FontStyle.Regular, muted, 326, 6);
        Label(g,"×", "Segoe UI", 19, FontStyle.Regular, muted, 356, 6);
        DateTime utc = DateTime.UtcNow;
        int row = compact ? 49 : 77;
        for(int i=0;i<3;i++) {
            int y = 43 + i * row;
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, zones[i]);
            double offset = zones[i].GetUtcOffset(utc).TotalHours;
            string off = "UTC" + (offset >= 0 ? "+" : "") + offset.ToString("0");
            using(var b = new SolidBrush(Color.FromArgb(20,29,40))) g.FillRectangle(b, 12, y, 360, row - 5);
            using(var b = new SolidBrush(i == 2 ? Color.FromArgb(93,169,255) : green)) g.FillRectangle(b, 12, y, 2, row - 5);
            Label(g,names[i], "Microsoft YaHei UI", 16, FontStyle.Bold, Color.FromArgb(224,234,242), 25, y+6);
            Label(g,off, "Consolas", 11, FontStyle.Regular, muted, 100, y+10);
            Label(g,local.ToString("HH:mm:ss"), "Consolas", compact ? 29 : 34, FontStyle.Bold, Color.FromArgb(236,245,249), compact ? 218 : 197, y+2);
            if(!compact) {
                Label(g,codes[i], "Consolas", 11, FontStyle.Regular, muted, 25, y+35);
                string season = zones[i].IsDaylightSavingTime(local) ? "DST" : "STD";
                Label(g,season, "Consolas", 10, FontStyle.Regular, muted, 25, y+51);
                Label(g,local.ToString("MM/dd ddd", System.Globalization.CultureInfo.InvariantCulture), "Consolas", 12, FontStyle.Regular, muted, 258, y+43);
            }
        }
        Label(g,"拖动移动  ·  右键设置", "Microsoft YaHei UI", 10, FontStyle.Regular, muted, 15, ClientSize.Height-19);
        Label(g,"SYSTEM TIME", "Consolas", 10, FontStyle.Regular, muted, 300, ClientSize.Height-18);
    }
    [STAThread] public static void Main(string[] args) {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length > 0 && (args[0] == "--enable-startup" || args[0] == "--disable-startup")) {
            try { SetStartup(args[0] == "--enable-startup"); }
            catch { Environment.ExitCode = 1; }
            return;
        }
        if(args.Length > 0 && args[0] == "--self-test") {
            foreach(string id in new string[]{"GMT Standard Time","Eastern Standard Time","China Standard Time"}) {
                var z = TimeZoneInfo.FindSystemTimeZoneById(id);
                Console.WriteLine(id + " winter=" + z.GetUtcOffset(new DateTime(2026,1,15,12,0,0,DateTimeKind.Utc)) + " summer=" + z.GetUtcOffset(new DateTime(2026,7,15,12,0,0,DateTimeKind.Utc)));
            }
            using(var form = new TradingClock(true)) {
                form.Show();
                form.SetFloating(false);
                if(form.TopMost || form.FormBorderStyle != FormBorderStyle.FixedSingle || form.ClientSize.Height != 294) throw new Exception("Normal window failed");
                using(var bmp = new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size)); bmp.Save(args[1]); }
                form.SetFloating(true);
                if(!form.TopMost || form.FormBorderStyle != FormBorderStyle.None) throw new Exception("Floating window failed");
                form.Close();
            }
            return;
        }
        Application.Run(new TradingClock());
    }
}
