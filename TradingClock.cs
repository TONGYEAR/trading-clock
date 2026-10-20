using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;

public class TradingClock : Form {
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h,int i);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h,int i,int v);
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h,int id);
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly Color bg=Color.FromArgb(14,20,29), muted=Color.FromArgb(133,152,169), green=Color.FromArgb(50,229,177), gold=Color.FromArgb(255,199,92);
    readonly string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TradingClock");
    ClockSettings cfg=new ClockSettings();
    ContextMenuStrip menu;
    NotifyIcon tray;
    bool testing, quitting, passThrough, hotkey;
    float scale=1;
    int canvasW=404,canvasH;
    readonly FiveMinuteTimer focusTimer=new FiveMinuteTimer();
    const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run", StartupName="ThreeCityTradingClock";
    static string StartupCommand { get { return "\""+Application.ExecutablePath+"\""; } }
    static bool StartupEnabled() { using(var k=Registry.CurrentUser.OpenSubKey(RunKey)) return k!=null && string.Equals(k.GetValue(StartupName) as string,StartupCommand,StringComparison.OrdinalIgnoreCase); }
    static void SetStartup(bool value) { using(var k=Registry.CurrentUser.CreateSubKey(RunKey)) { if(value) k.SetValue(StartupName,StartupCommand); else k.DeleteValue(StartupName,false); } }
    public TradingClock(bool test=false) {
        testing=test; Text="Oil Desk · 三城交易时钟"; Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if(!test) LoadSettings();
        if(cfg.Cities.Count==0) cfg.Cities.AddRange(new int[]{0,1,2});
        OilEngine.SeedEia(cfg,DateTime.UtcNow);
        BackColor=bg; ForeColor=Color.White; DoubleBuffered=true; AutoScaleMode=AutoScaleMode.None;
        StartPosition=FormStartPosition.Manual;
        Location=new Point(cfg.X < 0 ? Screen.PrimaryScreen.WorkingArea.Right-440 : cfg.X,cfg.Y);
        ApplyLayout();
        menu=BuildMenu(); ContextMenuStrip=menu;
        if(!test) {
            tray=new NotifyIcon {Icon=Icon,Text="Oil Desk · 三城交易时钟",Visible=true,ContextMenuStrip=menu};
            tray.DoubleClick+=delegate { RestoreClock(); };
            tray.BalloonTipClicked+=delegate { RestoreClock(); };
        }
        timer.Interval=500; timer.Tick+=delegate { Invalidate(); }; timer.Start();
        MouseDown+=delegate(object sender,MouseEventArgs e) {
            if(e.Button!=MouseButtons.Left) return;
            float x=e.X/scale,y=e.Y/scale;
            if(y>=canvasH-67 && y<=canvasH-34) {
                if(x>=canvasW-154 && x<canvasW-82) { focusTimer.Toggle();Invalidate();return; }
                if(x>=canvasW-78 && x<=canvasW-14) { focusTimer.Reset();Invalidate();return; }
            }
            if(y<36 && x>canvasW-35) { Hide(); return; }
            if(y<36 && x>canvasW-78) { menu.Show(this,e.Location); return; }
            if(!cfg.Locked) { ReleaseCapture(); SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero); }
        };
        MouseUp+=delegate { SaveSettings(); };
        FormClosing+=delegate(object sender,FormClosingEventArgs e) {
            if(!quitting && !testing && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; SaveSettings(); Hide(); }
        };
        FormClosed+=delegate { SaveSettings(); timer.Dispose(); if(tray!=null) tray.Dispose(); };
        Shown+=delegate { if(!testing) SaveSettings(); };
    }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); if(!testing) hotkey=RegisterHotKey(Handle,1,0x0002|0x0004|0x4000,(uint)Keys.T); }
    protected override void OnHandleDestroyed(EventArgs e) { if(hotkey) UnregisterHotKey(Handle,1); base.OnHandleDestroyed(e); }
    protected override void WndProc(ref Message m) { if(m.Msg==0x0312) { RestoreClock(); return; } if(m.Msg==0x0112 && (m.WParam.ToInt64()&0xFFF0)==0xF010 && cfg.Locked) return; base.WndProc(ref m); }
    void LoadSettings() {
        try { using(var f=File.OpenRead(Path.Combine(folder,"settings.xml"))) cfg=(ClockSettings)new XmlSerializer(typeof(ClockSettings)).Deserialize(f); }
        catch {
            try { var p=File.ReadAllText(Path.Combine(folder,"position.txt")).Split(','); cfg.X=int.Parse(p[0]); cfg.Y=int.Parse(p[1]); if(p.Length>2) cfg.Pinned=cfg.Floating=bool.Parse(p[2]); } catch { }
        }
        cfg.Scale=Math.Max(75,Math.Min(175,cfg.Scale)); cfg.Opacity=Math.Max(60,Math.Min(100,cfg.Opacity)); cfg.Layout=Math.Max(0,Math.Min(2,cfg.Layout));
        cfg.Cities=cfg.Cities.Where(i=>i>=0 && i<OilEngine.Names.Length).Distinct().ToList(); if(cfg.Cities.Count==0) cfg.Cities.AddRange(new int[]{0,1,2});
    }
    void SaveSettings() {
        if(testing) return;
        try {
            Point p=WindowState==FormWindowState.Normal ? Location : RestoreBounds.Location; cfg.X=p.X; cfg.Y=p.Y;
            Directory.CreateDirectory(folder); string file=Path.Combine(folder,"settings.xml"),temp=file+".tmp";
            using(var f=File.Create(temp)) new XmlSerializer(typeof(ClockSettings)).Serialize(f,cfg);
            if(File.Exists(file)) File.Replace(temp,file,null); else File.Move(temp,file);
        } catch(Exception ex) { if(tray!=null) tray.Text="时钟：设置保存失败，请检查磁盘权限"; Debug.WriteLine(ex.Message); }
    }
    void ApplyLayout() {
        canvasW=cfg.Layout==2 ? Math.Max(600,Math.Min(4,cfg.Cities.Count)*200+24) : 404;
        canvasH=cfg.Layout==2 ? 40+(int)Math.Ceiling(cfg.Cities.Count/4.0)*90+270 : 40+cfg.Cities.Count*(cfg.Layout==1?53:78)+270;
        scale=cfg.Scale/100f;
        var area=Screen.FromPoint(Location).WorkingArea;
        scale=Math.Min(scale,Math.Min((area.Width-16f)/canvasW,(area.Height-16f)/canvasH));
        FormBorderStyle=cfg.Floating?FormBorderStyle.None:FormBorderStyle.FixedSingle; MaximizeBox=false;
        TopMost=cfg.Pinned; Opacity=cfg.Opacity/100.0;
        ClientSize=new Size((int)(canvasW*scale),(int)(canvasH*scale));
        Location=new Point(Math.Max(area.Left,Math.Min(Left,area.Right-Width)),Math.Max(area.Top,Math.Min(Top,area.Bottom-Height)));
        Invalidate();
    }
    void SetPassThrough(bool value) {
        if(value && !cfg.Floating) { cfg.Floating=true; ApplyLayout(); }
        passThrough=value;
        int style=GetWindowLong(Handle,-20);
        SetWindowLong(Handle,-20,value?style|0x80000|0x20:style&~0x20);
        Invalidate();
        if(value && tray!=null) tray.ShowBalloonTip(4000,"已开启鼠标穿透", "双击右下角托盘图标可恢复操作"+(hotkey?"，或按 Ctrl+Alt+T":""),ToolTipIcon.Info);
    }
    void RestoreClock() { SetPassThrough(false); Show(); WindowState=FormWindowState.Normal; Activate(); }
    ToolStripMenuItem Item(string title,Action act) { var x=new ToolStripMenuItem(title); x.Click+=delegate { try { act(); SaveSettings(); Invalidate(); } catch(Exception e) { MessageBox.Show(this,e.Message,"Oil Desk"); } }; return x; }
    ToolStripMenuItem Check(string title,Func<bool> get,Action act) { var x=Item(title,act); menuChecks+=delegate { x.Checked=get(); }; return x; }
    Action menuChecks=delegate{};
    ContextMenuStrip BuildMenu() {
        var m=new ContextMenuStrip();
        m.Items.Add(Item("显示窗口 / 恢复鼠标操作",RestoreClock));
        m.Items.Add(Check("桌面浮窗",()=>cfg.Floating,()=>{cfg.Floating=true; cfg.Pinned=true; ApplyLayout();}));
        m.Items.Add(Check("普通窗口",()=>!cfg.Floating,()=>{SetPassThrough(false); cfg.Floating=false;cfg.Pinned=false;ApplyLayout();}));
        m.Items.Add(Check("始终置顶",()=>cfg.Pinned,()=>{cfg.Pinned=!cfg.Pinned;TopMost=cfg.Pinned;}));
        var layout=new ToolStripMenuItem("布局");
        string[] layouts={"标准竖版","紧凑竖版","屏幕横条"};
        for(int i=0;i<3;i++) { int n=i; layout.DropDownItems.Add(Check(layouts[i],()=>cfg.Layout==n,()=>{cfg.Layout=n;ApplyLayout();})); }
        layout.DropDownItems.Add(Item("贴到当前屏幕顶部",()=>{cfg.Layout=2;ApplyLayout();var a=Screen.FromControl(this).WorkingArea;Location=new Point(a.Left+(a.Width-Width)/2,a.Top);}));
        m.Items.Add(layout);
        var sizes=new ToolStripMenuItem("字体 / 整体缩放"); foreach(int v in new int[]{75,100,125,150,175}) { int n=v;sizes.DropDownItems.Add(Check(v+"%",()=>cfg.Scale==n,()=>{cfg.Scale=n;ApplyLayout();})); } m.Items.Add(sizes);
        var opacity=new ToolStripMenuItem("透明度"); foreach(int v in new int[]{100,90,80,70}) { int n=v;opacity.DropDownItems.Add(Check(v+"%",()=>cfg.Opacity==n,()=>{cfg.Opacity=n;ApplyLayout();})); } m.Items.Add(opacity);
        var cities=new ToolStripMenuItem("显示城市"); for(int i=0;i<OilEngine.Names.Length;i++) { int n=i;cities.DropDownItems.Add(Check(OilEngine.Names[n],()=>cfg.Cities.Contains(n),()=>{if(cfg.Cities.Contains(n)){if(cfg.Cities.Count==1)throw new Exception("至少保留一个城市。");cfg.Cities.Remove(n);}else cfg.Cities.Add(n);ApplyLayout();})); } m.Items.Add(cities);
        m.Items.Add(Check("锁定窗口位置",()=>cfg.Locked,()=>cfg.Locked=!cfg.Locked));
        m.Items.Add(Check("鼠标穿透（托盘可恢复）",()=>passThrough,()=>SetPassThrough(!passThrough)));
        m.Items.Add(new ToolStripSeparator());
        var market=new ToolStripMenuItem("石油交易时段");
        foreach(string v in new string[]{"WTI","BRENT","CUSTOM"}) { string n=v;market.DropDownItems.Add(Check(v=="WTI"?"WTI · CME 常规时段参考":v=="BRENT"?"Brent · ICE 常规时段参考":"CFD / 自定义时段",()=>cfg.Market==n,()=>{if(n=="CUSTOM") {EditCustom();}else cfg.Market=n;})); }
        market.DropDownItems.Add(Item("特殊休市 / 提前收市设置…",EditOverrides));
        market.DropDownItems.Add(Item("CME 官方节假日与交易时间",()=>Process.Start("https://www.cmegroup.com/trading-hours.html")));
        market.DropDownItems.Add(Item("ICE 官方交易时间",()=>Process.Start("https://www.ice.com/products/219/Brent-Crude-Futures")));
        m.Items.Add(market);
        m.Items.Add(Item("编辑 EIA 公布时间…",EditEvents));
        m.Items.Add(Item("5 分钟计时：开始 / 暂停",()=>focusTimer.Toggle()));
        m.Items.Add(Item("5 分钟计时：重置",()=>focusTimer.Reset()));
        m.Items.Add(Check("开机自动启动（登录 Windows 后）",StartupEnabled,()=>SetStartup(!StartupEnabled())));
        m.Items.Add(Item("隐藏到系统托盘",()=>Hide()));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Item("退出程序",()=>{quitting=true;Close();}));
        m.Opening+=delegate {try{menuChecks();}catch{}};
        return m;
    }
    void Label(Graphics g,string text,float size,Color color,float x,float y,bool bold=false,string face="Microsoft YaHei UI",float width=0) {
        using(var f=new Font(face,size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))
        using(var b=new SolidBrush(color)) {
            if(width>0) { using(var fmt=new StringFormat {Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap}) g.DrawString(text,f,b,new RectangleF(x,y,width,size+10),fmt); }
            else g.DrawString(text,f,b,x,y);
        }
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e); var g=e.Graphics;g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;
        g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using(var p=new Pen(Color.FromArgb(45,62,78))) g.DrawRectangle(p,0,0,canvasW-1,canvasH-1);
        Label(g,"●  OIL DESK / CLOCK",13,green,15,10,true,"Segoe UI");
        Label(g,passThrough?"穿透":cfg.Locked?"锁定":cfg.Floating?"浮窗":"窗口",11,muted,canvasW-125,12);
        Label(g,"设置",12,muted,canvasW-76,10);Label(g,"−",20,muted,canvasW-29,3);
        var utc=DateTime.UtcNow;
        for(int j=0;j<cfg.Cities.Count;j++) {
            int i=cfg.Cities[j]; bool horizontal=cfg.Layout==2;
            int h=cfg.Layout==1?53:78;
            float x=horizontal?12+(j%4)*200:12,y=40+(horizontal?(j/4)*90:j*h),w=horizontal?192:380;
            using(var b=new SolidBrush(Color.FromArgb(20,29,40))) g.FillRectangle(b,x,y,w,h-5);
            var local=TimeZoneInfo.ConvertTimeFromUtc(utc,OilEngine.Zone(OilEngine.ZoneIds[i]));
            var off=OilEngine.Zone(OilEngine.ZoneIds[i]).GetUtcOffset(utc).TotalHours;
            string offset="UTC"+(off>=0?"+":"")+off.ToString("0.#");
            Label(g,OilEngine.Names[i],15,Color.White,x+12,y+5,true);
            if(horizontal) {
                Label(g,offset,10,muted,x+108,y+8);
                Label(g,local.ToString("HH:mm:ss"),29,Color.White,x+10,y+22,true,"Consolas");
                Label(g,local.ToString("MM/dd ddd",System.Globalization.CultureInfo.InvariantCulture),10,muted,x+12,y+56);
            } else {
                Label(g,offset,11,muted,x+84,y+9,false,"Consolas");
                Label(g,local.ToString("HH:mm:ss"),cfg.Layout==1?29:34,Color.White,x+187,y+1,true,"Consolas");
                if(cfg.Layout!=1) { Label(g,OilEngine.Codes[i],11,muted,x+12,y+36,false,"Consolas");Label(g,local.ToString("MM/dd ddd",System.Globalization.CultureInfo.InvariantCulture),12,muted,x+255,y+45,false,"Consolas");Label(g,OilEngine.Zone(OilEngine.ZoneIds[i]).IsDaylightSavingTime(local)?"DST":"STD",10,muted,x+12,y+53,false,"Consolas"); }
            }
        }
        float fy=canvasH-266;var st=OilEngine.State(utc,cfg);
        Label(g,"重要时间 / COUNTDOWN",12,green,16,fy,true);
        CountdownRow(g,cfg.Market+" "+st.Action+" · 常规",st.Next,utc,fy+24,st.Open?green:gold);
        var next=cfg.Events.Where(v=>v.Id.StartsWith("eia-") && v.Utc>utc).OrderBy(v=>v.Utc).FirstOrDefault();
        CountdownRow(g,"EIA 原油库存",next==null?DateTime.MaxValue:next.Utc,utc,fy+60,gold);
        CountdownRow(g,"纽约 09:00 · 关注",OilEngine.NextWeekdayTime(utc,"Eastern Standard Time",9),utc,fy+96,Color.White);
        CountdownRow(g,"伦敦 08:00 · 关注",OilEngine.NextWeekdayTime(utc,"GMT Standard Time",8),utc,fy+132,Color.White);
        float timerY=canvasH-91;
        using(var brush=new SolidBrush(Color.FromArgb(23,37,48)))g.FillRectangle(brush,12,timerY,canvasW-24,61);
        Label(g,"5 分钟倒计时",12,muted,22,timerY+4);
        int seconds=(int)Math.Ceiling(focusTimer.Seconds);
        Label(g,(seconds/60).ToString("00")+":"+(seconds%60).ToString("00"),27,seconds==0?gold:green,22,timerY+23,true,"Consolas");
        if(seconds==0)Label(g,"已结束",11,gold,115,timerY+32);
        using(var brush=new SolidBrush(Color.FromArgb(39,59,73))) {
            g.FillRectangle(brush,canvasW-154,canvasH-67,72,30);
            g.FillRectangle(brush,canvasW-78,canvasH-67,64,30);
        }
        Label(g,focusTimer.Running?"暂停":seconds==0?"再开始":seconds<300?"继续":"开始",13,green,canvasW-144,canvasH-61);
        Label(g,"重置",13,Color.White,canvasW-66,canvasH-61);
        Label(g,"时段为常规参考；EIA 为日程快照；日期按上海显示",9,muted,16,canvasH-26);
        Label(g,"右键设置 · 托盘恢复",9,muted,16,canvasH-13);
    }
    void CountdownRow(Graphics g,string name,DateTime target,DateTime utc,float y,Color color) {
        Label(g,name,13,color,16,y,true);
        string date=target==DateTime.MaxValue?"待更新日程":TimeZoneInfo.ConvertTimeFromUtc(target,OilEngine.Zone("China Standard Time")).ToString("MM/dd HH:mm");
        Label(g,date,10,muted,16,y+18,false,"Consolas");
        string value=target==DateTime.MaxValue?"--:--:--":OilEngine.Countdown(target-utc);
        Label(g,value,22,color,canvasW-197,y+3,true,"Consolas");
    }
    Form Dialog(string title,int width,int height) { return new Form {Text=title,ClientSize=new Size(width,height),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,TopMost=true,Font=new Font("Microsoft YaHei UI",9),BackColor=Color.FromArgb(240,244,248),AutoScaleMode=AutoScaleMode.None}; }
    Label TextAt(Form f,string text,int x,int y,int w) { var c=new Label {Text=text,Left=x,Top=y,Width=w,Height=40};f.Controls.Add(c);return c; }
    Button ButtonAt(Form f,string text,int x,int y,Action action) { var c=new Button {Text=text,Left=x,Top=y,Width=105,Height=30};c.Click+=delegate {try{action();}catch(Exception e){MessageBox.Show(f,e.Message,"请检查输入");}};f.Controls.Add(c);return c; }
    ComboBox ZoneBox(Form f,int x,int y) { var z=new ComboBox {Left=x,Top=y,Width=180,DropDownStyle=ComboBoxStyle.DropDownList};z.Items.AddRange(new object[]{"上海 / 香港","纽约","伦敦","芝加哥","东京","悉尼"});z.SelectedIndex=0;f.Controls.Add(z);return z; }
    static readonly string[] EditorZones={"China Standard Time","Eastern Standard Time","GMT Standard Time","Central Standard Time","Tokyo Standard Time","AUS Eastern Standard Time"};
    void EditEvents() {
        SetPassThrough(false);
        using(var f=Dialog("EIA 公布时间 · 仅倒计时，不弹窗",740,490)) {
            var list=new ListBox {Left=16,Top=16,Width=708,Height=190,HorizontalScrollbar=true};f.Controls.Add(list);
            Action refresh=()=>{list.Items.Clear();foreach(var e in cfg.Events.Where(v=>v.Id.StartsWith("eia-")).OrderBy(e=>e.Utc))list.Items.Add(e);};refresh();
            TextAt(f,"EIA 日程名称",16,218,680);
            var title=new TextBox {Left=16,Top=242,Width=708,MaxLength=100};f.Controls.Add(title);
            TextAt(f,"公布时间",16,279,180);TextAt(f,"输入时区",282,279,180);
            var when=new DateTimePicker {Left=16,Top=304,Width=246,Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd HH:mm",Value=TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,OilEngine.Zone("China Standard Time")).AddHours(1)};f.Controls.Add(when);
            var zone=ZoneBox(f,282,304);
            list.SelectedIndexChanged+=delegate {var ev=list.SelectedItem as ClockEvent;if(ev==null)return;title.Text=ev.Title;zone.SelectedIndex=0;when.Value=TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(ev.Utc,DateTimeKind.Utc),OilEngine.Zone(EditorZones[0]));};
            Action<bool> save=update=>{
                if(string.IsNullOrWhiteSpace(title.Text))throw new Exception("请输入事件名称。");
                DateTime utc=OilEngine.ToUtc(when.Value,EditorZones[zone.SelectedIndex]);if(utc<=DateTime.UtcNow)throw new Exception("请选择未来的时间。");
                var ev=update?list.SelectedItem as ClockEvent:null;if(update && ev==null)throw new Exception("请选择要修改的事件。");
                if(ev==null){ev=new ClockEvent();ev.Id="eia-"+ev.Id;cfg.Events.Add(ev);}ev.Title=title.Text.Trim();ev.Utc=utc;SaveSettings();refresh();
            };
            ButtonAt(f,"新增",16,350,()=>save(false));ButtonAt(f,"保存修改",133,350,()=>save(true));ButtonAt(f,"删除选中",250,350,()=>{var ev=list.SelectedItem as ClockEvent;if(ev!=null)cfg.Events.Remove(ev);SaveSettings();refresh();});
            ButtonAt(f,"EIA 官方日程",367,350,()=>Process.Start("https://www.eia.gov/petroleum/supply/weekly/schedule.php"));
            ButtonAt(f,"清理过期事件",484,350,()=>{cfg.Events.RemoveAll(v=>v.Utc<DateTime.UtcNow);SaveSettings();refresh();});
            TextAt(f,"EIA 已载入 2026 年官方日程快照（核对日期：2026-10-14，含已公布延期）。\n此处仅维护 EIA 倒计时日期，不弹窗、不播放声音，也不自动下载日程。",16,402,710).Height=60;
            f.ShowDialog(this);
        }
    }
    void EditCustom() {
        using(var f=Dialog("CFD / 自定义时段",530,355)) {
            TextAt(f,"请按交易平台公告输入；每天一个时段，收市≤开市代表跨日。",16,15,500);
            var z=ZoneBox(f,16,66);z.SelectedIndex=Math.Max(0,Array.IndexOf(EditorZones,cfg.CustomZone));
            TextAt(f,"开市 HH:mm",220,42,125);TextAt(f,"收市 HH:mm",365,42,130);
            var open=new TextBox {Left=220,Top=66,Width=125,Text=cfg.CustomOpen};var close=new TextBox {Left=365,Top=66,Width=125,Text=cfg.CustomClose};f.Controls.AddRange(new Control[]{open,close});
            TextAt(f,"按开市所在日期勾选（不是收市日期）",16,112,500);
            var days=new CheckedListBox {Left=16,Top=141,Width=480,Height=110,MultiColumn=true,ColumnWidth=115};
            string[] labels={"周日","周一","周二","周三","周四","周五","周六"};for(int i=0;i<7;i++)days.Items.Add(labels[i],cfg.CustomDays.Split(',').Contains(i.ToString()));f.Controls.Add(days);
            ButtonAt(f,"保存并启用",16,284,()=>{
                ParseTime(open.Text);ParseTime(close.Text);if(days.CheckedIndices.Count==0)throw new Exception("至少选择一天。");
                cfg.CustomZone=EditorZones[z.SelectedIndex];cfg.CustomOpen=open.Text;cfg.CustomClose=close.Text;cfg.CustomDays=string.Join(",",days.CheckedIndices.Cast<int>());cfg.Market="CUSTOM";SaveSettings();f.Close();
            });f.ShowDialog(this);
        }
    }
    static TimeSpan ParseTime(string t) {DateTime d;if(!DateTime.TryParseExact(t,"HH:mm",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out d))throw new Exception("时间格式应为 HH:mm，例如 17:00。");return d.TimeOfDay;}
    void EditOverrides() {
        using(var f=Dialog("特殊时段 · 替换所选日期开始的整个交易时段",680,430)) {
            var list=new ListBox {Left=16,Top=15,Width=648,Height=145};f.Controls.Add(list);Action refresh=()=>{list.Items.Clear();foreach(var o in cfg.Overrides)list.Items.Add(o);};refresh();
            TextAt(f,"当前品种："+cfg.Market+"；时区："+OilEngine.MarketZone(cfg)+"\nWTI 若周一提前收市，应修改周日开始的跨夜时段。",16,170,648).Height=50;
            var date=new DateTimePicker {Left=16,Top=230,Width=150,Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd"};f.Controls.Add(date);
            var closed=new CheckBox {Left=185,Top=230,Width=120,Text="该时段休市"};f.Controls.Add(closed);
            var open=new TextBox {Left=320,Top=230,Width=90,Text="17:00"};var close=new TextBox {Left=440,Top=230,Width=90,Text="12:00"};f.Controls.AddRange(new Control[]{open,close});
            TextAt(f,"日期 = 开市日期          勾选则跳过该时段            开市 HH:mm     收市 HH:mm",16,266,645);
            list.SelectedIndexChanged+=delegate {var o=list.SelectedItem as MarketOverride;if(o==null)return;date.Value=DateTime.Parse(o.Date);closed.Checked=o.Closed;open.Text=o.Open;close.Text=o.Close;};
            ButtonAt(f,"保存 / 替换",16,312,()=>{if(!closed.Checked){ParseTime(open.Text);ParseTime(close.Text);}string d=date.Value.ToString("yyyy-MM-dd");cfg.Overrides.RemoveAll(o=>o.Market==cfg.Market&&o.Date==d);cfg.Overrides.Add(new MarketOverride {Market=cfg.Market,Date=d,Closed=closed.Checked,Open=open.Text,Close=close.Text});SaveSettings();refresh();});
            ButtonAt(f,"删除选中",135,312,()=>{var o=list.SelectedItem as MarketOverride;if(o!=null)cfg.Overrides.Remove(o);SaveSettings();refresh();});
            TextAt(f,"未设置的日期沿用常规时段。请以交易所和经纪商临时公告为准。",16,368,640);
            f.ShowDialog(this);
        }
    }
    [STAThread] public static void Main(string[] args) {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length>0 && (args[0]=="--enable-startup"||args[0]=="--disable-startup")) {try{SetStartup(args[0]=="--enable-startup");}catch{Environment.ExitCode=1;}return;}
        if(args.Length>0 && args[0]=="--self-test") {try{RunTests(args.Length>1?args[1]:".");}catch(Exception e){File.WriteAllText(Path.Combine(args[1],"test-failure.txt"),e.ToString());Environment.ExitCode=1;}return;}
        bool first;
        using(var mutex=new Mutex(true,"Local\\ThreeCityTradingClockV2",out first)) {
            if(!first) {MessageBox.Show("时钟已经运行，请双击右下角托盘图标显示。","Oil Desk");return;}
            Application.Run(new TradingClock());
        }
    }
    static void Assert(bool value,string message) {if(!value)throw new Exception(message);}
    static void RunTests(string dir) {
        double time=0;var focus=new FiveMinuteTimer(()=>time);
        Assert(focus.Seconds==300&&!focus.Running,"Timer initial");
        focus.Toggle();time=62;Assert(focus.Seconds==238&&focus.Running,"Timer elapsed");
        focus.Toggle();time=120;Assert(focus.Seconds==238&&!focus.Running,"Timer pause");
        focus.Toggle();time=358;Assert(focus.Seconds==0&&!focus.Running,"Timer complete");
        time=500;Assert(focus.Seconds==0,"Timer clamps zero");
        focus.Toggle();Assert(focus.Seconds==300&&focus.Running,"Timer restart");
        focus.Reset();Assert(focus.Seconds==300&&!focus.Running,"Timer reset");
        var s=new ClockSettings();
        Func<string,DateTime> utc=t=>DateTime.Parse(t,null,System.Globalization.DateTimeStyles.AdjustToUniversal|System.Globalization.DateTimeStyles.AssumeUniversal);
        Assert(OilEngine.State(utc("2026-07-13T20:59:59Z"),s).Open,"WTI before maintenance");
        Assert(!OilEngine.State(utc("2026-07-13T21:00:00Z"),s).Open,"WTI maintenance boundary");
        Assert(OilEngine.State(utc("2026-07-13T22:00:00Z"),s).Open,"WTI reopen");
        Assert(!OilEngine.State(utc("2026-07-11T12:00:00Z"),s).Open,"WTI weekend");
        Assert(OilEngine.State(utc("2026-01-11T23:00:00Z"),s).Open,"WTI winter Sunday");
        Assert(!OilEngine.State(utc("2026-01-11T22:59:59Z"),s).Open,"WTI winter before Sunday");
        Assert(OilEngine.State(utc("2026-03-08T22:00:00Z"),s).Open,"WTI US spring DST Sunday");
        Assert(OilEngine.State(utc("2026-11-01T23:00:00Z"),s).Open,"WTI US fall DST Sunday");
        s.Overrides.Add(new MarketOverride {Date="2026-07-12",Market="WTI",Closed=true});
        Assert(!OilEngine.State(utc("2026-07-13T12:00:00Z"),s).Open,"Closed override");s.Overrides.Clear();
        s.Overrides.Add(new MarketOverride {Date="2026-07-12",Market="WTI",Open="17:00",Close="12:00"});
        Assert(!OilEngine.State(utc("2026-07-13T17:00:00Z"),s).Open,"Early close override");
        s.Market="BRENT";
        Assert(OilEngine.State(utc("2026-07-12T22:00:00Z"),s).Open,"Brent Sunday open");
        Assert(OilEngine.State(utc("2026-07-13T00:00:00Z"),s).Open,"Brent Monday continuity");
        Assert(!OilEngine.State(utc("2026-07-13T22:00:00Z"),s).Open,"Brent maintenance");
        s.Market="CUSTOM";s.CustomOpen="09:00";s.CustomClose="17:00";s.CustomDays="1,2,3,4,5";s.CustomZone="China Standard Time";
        Assert(OilEngine.State(utc("2026-07-13T01:00:00Z"),s).Open,"Custom session");
        Assert(!OilEngine.State(utc("2026-07-13T09:00:00Z"),s).Open,"Custom close");
        var events=new ClockSettings();OilEngine.SeedEia(events,utc("2026-10-14T00:00:00Z"));
        Assert(events.Events.First().Utc==utc("2026-10-15T16:00:00Z"),"EIA holiday postponement");
        Assert(OilEngine.Due(utc("2026-10-15T15:51:00Z"),events.Events.First().Utc,10),"Reminder lead");
        Assert(!OilEngine.Due(utc("2026-10-15T17:00:00Z"),events.Events.First().Utc,10),"No stale reminder");
        Assert(OilEngine.NextWeekdayTime(utc("2026-07-10T14:00:00Z"),"Eastern Standard Time",9)==utc("2026-07-13T13:00:00Z"),"Focus countdown skips weekend");
        Assert(OilEngine.NextWeekdayTime(utc("2026-10-23T16:00:00Z"),"GMT Standard Time",8)==utc("2026-10-26T08:00:00Z"),"London focus across DST");
        using(var f=new TradingClock(true)) {
            f.Show();
            for(int n=0;n<3;n++){f.cfg.Layout=n;f.ApplyLayout();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(dir,"oil-layout-"+n+".png"));}}
            f.cfg.Cities=Enumerable.Range(0,7).ToList();f.cfg.Scale=175;f.ApplyLayout();Assert(f.Width<=Screen.FromControl(f).WorkingArea.Width,"Fit screen");
            f.SetPassThrough(true);Assert((GetWindowLong(f.Handle,-20)&0x20)!=0,"Click through on");f.SetPassThrough(false);Assert((GetWindowLong(f.Handle,-20)&0x20)==0,"Click through off");
            f.cfg.Floating=false;f.cfg.Pinned=false;f.ApplyLayout();Assert(f.FormBorderStyle==FormBorderStyle.FixedSingle&&!f.TopMost,"Normal window mode");
            Action<Action,string> captureDialog=(open,name)=>{
                using(var capture=new System.Windows.Forms.Timer()) {
                    capture.Interval=200;
                    capture.Tick+=delegate {var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(v=>v!=f);if(dialog!=null){capture.Stop();using(var b=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(b,new Rectangle(Point.Empty,dialog.Size));b.Save(Path.Combine(dir,name+".png"));}dialog.Close();}};
                    capture.Start();open();
                }
            };
            captureDialog(f.EditEvents,"oil-events");captureDialog(f.EditCustom,"oil-custom");captureDialog(f.EditOverrides,"oil-overrides");
            using(var sw=new StringWriter()){new XmlSerializer(typeof(ClockSettings)).Serialize(sw,f.cfg);using(var sr=new StringReader(sw.ToString())){var back=(ClockSettings)new XmlSerializer(typeof(ClockSettings)).Deserialize(sr);Assert(back.Cities.Count==7 && back.Events.Count>0,"Settings round trip");}}
            f.quitting=true;f.Close();
        }
        File.WriteAllText(Path.Combine(dir,"test-result.txt"),"PASS: session boundaries, DST, weekends, holiday overrides, Brent weekly open, custom sessions, EIA postponement, reminders, layouts, click-through recovery, settings serialization.");
    }
}
