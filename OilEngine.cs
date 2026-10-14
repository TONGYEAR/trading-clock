using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;

public class ClockSettings {
    public int X = -1, Y = 80, Layout = 0, Scale = 100, Opacity = 100;
    public bool Floating = true, Pinned = true, Locked = false, MarketAlerts = true, Sound = true;
    public int LeadMinutes = 5;
    public string Market = "WTI";
    public string CustomZone = "Eastern Standard Time";
    public string CustomOpen = "18:00", CustomClose = "17:00";
    public string CustomDays = "0,1,2,3,4";
    public List<int> Cities = new List<int>();
    public List<ClockEvent> Events = new List<ClockEvent>();
    public List<MarketOverride> Overrides = new List<MarketOverride>();
    public List<string> Fired = new List<string>();
    public bool EiaSeeded;
}
public class ClockEvent {
    public string Id = Guid.NewGuid().ToString();
    public string Title;
    public DateTime Utc;
    public int Lead = 5;
    public override string ToString() { return Title + "  |  上海 " + TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(Utc, DateTimeKind.Utc), OilEngine.Zone("China Standard Time")).ToString("yyyy-MM-dd HH:mm"); }
}
public class MarketOverride {
    public string Market = "WTI", Date, Open, Close;
    public bool Closed;
    public override string ToString() { return Market + " " + Date + (Closed ? " 全天休市" : " " + Open + "—" + Close); }
}
public class MarketState {
    public bool Open;
    public DateTime Next;
    public string Action;
}
public static class OilEngine {
    public static readonly string[] Names = {"伦敦","纽约","上海","东京","香港","悉尼","芝加哥"};
    public static readonly string[] Codes = {"LONDON","NEW YORK","SHANGHAI","TOKYO","HONG KONG","SYDNEY","CHICAGO"};
    public static readonly string[] ZoneIds = {"GMT Standard Time","Eastern Standard Time","China Standard Time","Tokyo Standard Time","China Standard Time","AUS Eastern Standard Time","Central Standard Time"};
    static readonly Dictionary<string,TimeZoneInfo> zones = new Dictionary<string,TimeZoneInfo>();
    public static TimeZoneInfo Zone(string id) { if(!zones.ContainsKey(id)) zones[id] = TimeZoneInfo.FindSystemTimeZoneById(id); return zones[id]; }
    public static DateTime ToUtc(DateTime local, string id) {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        var z = Zone(id);
        if(z.IsInvalidTime(local) || z.IsAmbiguousTime(local)) throw new ArgumentException("该时间位于夏令时跳变或重复区间，请选择明确的时间。");
        return TimeZoneInfo.ConvertTimeToUtc(local,z);
    }
    public static string MarketZone(ClockSettings s) { return s.Market == "WTI" ? "Central Standard Time" : s.Market == "BRENT" ? "GMT Standard Time" : s.CustomZone; }
    public static MarketState State(DateTime utc, ClockSettings s) {
        string zid = MarketZone(s);
        DateTime today = TimeZoneInfo.ConvertTimeFromUtc(utc,Zone(zid)).Date;
        var intervals = new List<Tuple<DateTime,DateTime>>();
        for(int n=-2;n<16;n++) {
            DateTime day = today.AddDays(n); int dow = (int)day.DayOfWeek;
            TimeSpan start, end; bool active;
            if(s.Market == "WTI") { active = dow <= 4; start = TimeSpan.FromHours(17); end = TimeSpan.FromHours(16); }
            else if(s.Market == "BRENT") {
                // Monday's session starts Sunday at 23:00 London; Tue-Fri 01:00-23:00.
                active = dow == 0 || dow >= 2 && dow <= 5;
                start = TimeSpan.FromHours(dow == 0 ? 23 : 1); end = TimeSpan.FromHours(23);
            } else {
                active = s.CustomDays.Split(',').Contains(dow.ToString());
                start = TimeSpan.Parse(s.CustomOpen); end = TimeSpan.Parse(s.CustomClose);
            }
            var special = s.Overrides.LastOrDefault(o=>o.Market == s.Market && o.Date == day.ToString("yyyy-MM-dd"));
            if(special != null) { active = !special.Closed; if(active) { start = TimeSpan.Parse(special.Open); end = TimeSpan.Parse(special.Close); } }
            if(!active) continue;
            DateTime a = day.Add(start), b = day.Add(end);
            if(b <= a) b = b.AddDays(1);
            try { intervals.Add(Tuple.Create(ToUtc(a,zid),ToUtc(b,zid))); } catch(ArgumentException) { }
        }
        intervals.Sort((a,b)=>a.Item1.CompareTo(b.Item1));
        // Merge overlapping intervals so a continuous market never produces a false close.
        var merged = new List<Tuple<DateTime,DateTime>>();
        foreach(var p in intervals) {
            if(merged.Count > 0 && p.Item1 <= merged[merged.Count-1].Item2) {
                var prev = merged[merged.Count-1]; merged[merged.Count-1] = Tuple.Create(prev.Item1, p.Item2 > prev.Item2 ? p.Item2 : prev.Item2);
            } else merged.Add(p);
        }
        foreach(var p in merged) {
            if(utc >= p.Item1 && utc < p.Item2) return new MarketState {Open=true, Next=p.Item2, Action="休市"};
            if(p.Item1 > utc) return new MarketState {Open=false, Next=p.Item1, Action="开市"};
        }
        return new MarketState {Open=false, Next=DateTime.MaxValue, Action="待设置"};
    }
    public static bool BusinessHours(DateTime utc, string zone) {
        var t = TimeZoneInfo.ConvertTimeFromUtc(utc,Zone(zone));
        return t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday && t.Hour >= 8 && t.Hour < 17;
    }
    public static DateTime NextWeekdayTime(DateTime utc,string zone,int hour) {
        DateTime local=TimeZoneInfo.ConvertTimeFromUtc(utc,Zone(zone));
        for(int n=0;n<8;n++) {
            DateTime day=local.Date.AddDays(n);
            if(day.DayOfWeek==DayOfWeek.Saturday || day.DayOfWeek==DayOfWeek.Sunday) continue;
            DateTime target=ToUtc(day.AddHours(hour),zone);
            if(target>utc) return target;
        }
        throw new InvalidOperationException("No weekday found");
    }
    public static string Countdown(TimeSpan span) {
        if(span.TotalSeconds < 0) return "已到时间";
        return (span.Days > 0 ? span.Days + "天 " : "") + span.Hours.ToString("00") + ":" + span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00");
    }
    public static bool Due(DateTime now, DateTime at, int lead) { return now >= at.AddMinutes(-lead) && now < at.AddMinutes(1); }
    public static void SeedEia(ClockSettings s, DateTime now) {
        if(s.EiaSeeded) return;
        // Bounded official 2026 schedule snapshot; no extrapolation into future years.
        var exceptions = new Dictionary<string,string> {
            {"2026-01-21","2026-01-22 12:00"},{"2026-02-18","2026-02-19 12:00"},
            {"2026-05-27","2026-05-28 12:00"},{"2026-09-09","2026-09-10 12:00"},
            {"2026-10-14","2026-10-15 12:00"},{"2026-11-11","2026-11-12 12:00"}
        };
        for(DateTime d = new DateTime(2026,1,7); d <= new DateTime(2026,12,30); d = d.AddDays(7)) {
            string k = d.ToString("yyyy-MM-dd");
            DateTime local = exceptions.ContainsKey(k) ? DateTime.ParseExact(exceptions[k],"yyyy-MM-dd HH:mm",System.Globalization.CultureInfo.InvariantCulture) : d.AddHours(10).AddMinutes(30);
            var utc = ToUtc(local,"Eastern Standard Time");
            if(utc > now) s.Events.Add(new ClockEvent { Id="eia-"+k, Title="EIA 原油库存（2026 官方日程快照）", Utc=utc, Lead=10 });
        }
        s.EiaSeeded = true;
    }
}
