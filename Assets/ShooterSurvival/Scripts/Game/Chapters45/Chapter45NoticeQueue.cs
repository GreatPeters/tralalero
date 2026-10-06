using System;
using System.Collections.Generic;
using System.Linq;

// Chapter-local display arbitration. Gameplay effects happen before these notices;
// delaying a notice must never delay danger, rewards, movement or damage.
public sealed class Chapter45NoticeQueue
{
    public enum Kind { Information, Reward, Warning }
    public readonly struct Notice
    {
        public readonly string Title, Detail;
        public readonly float Seconds;
        public readonly Kind Type;
        public Notice(string title, string detail, float seconds, Kind type)
        { Title = title; Detail = detail; Seconds = Math.Max(0, seconds); Type = type; }
    }
    private readonly List<Notice> rewards = new();
    private readonly List<(Notice notice, float until)> warnings = new();
    private Notice current;
    private float until;
    private bool visible, suspended;

    public void Information(string title, string detail, float seconds, float now)
    {
        Advance(now);
        if (suspended || visible && current.Type != Kind.Information) return;
        Show(new Notice(title, detail, seconds, Kind.Information), now);
    }
    public void Reward(string title, string detail, float seconds, float now)
    {
        Advance(now);
        var reward = new Notice(title, detail, seconds, Kind.Reward);
        if (suspended || visible && current.Type != Kind.Information) rewards.Add(reward);
        else Show(reward, now);
    }
    public void Warning(string title, string detail, float seconds, float now)
    {
        Advance(now);
        if (suspended) return;
        PreserveReward();
        float previousWarningUntil = visible && current.Type == Kind.Warning ? until : now;
        if (!visible || current.Type != Kind.Warning) warnings.Clear();
        // Keep each concurrent danger readable until its own wind-up ends. A
        // nearby order call must not erase an already announced plate throw.
        warnings.RemoveAll(w => w.notice.Title == title && w.notice.Detail == detail);
        warnings.Add((new Notice(title, detail, seconds, Kind.Warning), now + Math.Max(0, seconds)));
        Show(new Notice(title, detail, seconds, Kind.Warning), now);
        until = Math.Max(until, previousWarningUntil);
        RefreshWarningDetails(now);
    }
    public bool TryRead(float now, out Notice notice)
    {
        Advance(now); notice = current;
        return visible && !suspended;
    }
    public void Suspend()
    {
        if (suspended) return;
        PreserveReward(); warnings.Clear(); visible = false; suspended = true;
    }
    public void Resume(float now)
    {
        if (!suspended) return;
        suspended = false; Advance(now);
    }
    public void Clear()
    { rewards.Clear(); warnings.Clear(); current = default; until = 0; visible = suspended = false; }
    private void PreserveReward()
    {
        // A preempted message gets its complete reading duration on redisplay.
        if (visible && current.Type == Kind.Reward) rewards.Insert(0, current);
    }
    private void Show(Notice notice, float now)
    { current = notice; until = now + notice.Seconds; visible = true; }
    private void RefreshWarningDetails(float now)
    {
        warnings.RemoveAll(w => now >= w.until);
        if (!visible || current.Type != Kind.Warning || warnings.Count == 0) return;
        var details = warnings.Select(w => warnings.Count == 1 || w.notice.Title == "행동 예고" || w.notice.Title == "위험 예고"
            ? w.notice.Detail : w.notice.Title + " · " + w.notice.Detail).Distinct();
        current = new Notice(current.Title, string.Join("\n", details), Math.Max(0, until - now), Kind.Warning);
    }
    private void Advance(float now)
    {
        if (suspended) return;
        if (visible && now >= until) { visible = false; warnings.Clear(); }
        RefreshWarningDetails(now);
        if (!visible && rewards.Count > 0)
        {
            var next = rewards[0]; rewards.RemoveAt(0); Show(next, now);
        }
    }
}
