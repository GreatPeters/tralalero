using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Local branch only. Existing SR18 turns and authored encounters still own the rest of the course.
public sealed class NoryangjinMarketBranch : NoryangjinRevampEvent
{
    public float length = 378, outsideOffset = 25, transition = 32, choiceSeconds = 5;
    public float earliestChoice = 60, travelSpeed = 4.8f;
    public HighwayChapter2UI choiceUI;
    public Sprite indoorPicture, outdoorPicture;
    public bool Selected { get; private set; }
    public bool Outside { get; private set; }
    public bool Driving { get; private set; }
    public bool Complete { get; private set; }
    public float Distance { get; private set; }
    public bool Waiting => Triggered && !Selected;
    private bool opened;

    public override void ResetForRun()
    {
        base.ResetForRun();
        Selected = Outside = Driving = Complete = opened = false; Distance = 0;
        if (choiceUI != null)
        {
            choiceUI.ResetForRun(); choiceUI.Chosen -= Choose; choiceUI.Chosen += Choose;
            choiceUI.Cancelled -= CancelChoice; choiceUI.Cancelled += CancelChoice;
        }
    }
    protected override void OnTriggered() { }
    protected override void OnTick(float dt)
    {
        if (!Selected && !opened && Director.Elapsed >= earliestChoice)
        {
            opened = true;
            if (choiceUI != null) choiceUI.OpenCustom("어디로 갈까?", "시장 안쪽", "바깥 부두",
                "상인들을 피해 안쪽으로", "파도를 피해 바깥으로", indoorPicture, outdoorPicture, choiceSeconds);
            else Choose(0);
            Director.Timeline.Add($"fork popup at {Director.Elapsed:F1}");
        }
        if (Selected && !Driving && !Complete && Ahead(Player.transform.position) <= .15f)
        {
            Driving = true; Distance = Mathf.Clamp(-Ahead(Player.transform.position), 0, length);
        }
    }
    public void Choose(int index)
    {
        if (Selected || !Triggered || index < 0 || index > 1) return;
        Selected = true; Outside = index == 1;
        Director.Hud?.Publish(Outside ? "바깥 부두! 파도 경고를 보세요" : "시장 안쪽! 상인들을 뚫으세요", 2);
        Director.Timeline.Add($"fork {(Outside ? "outside" : "inside")} at {Director.Elapsed:F1}");
        Director.QuietFor(5);
    }
    public Vector3 Point(float distance, bool outside)
    {
        float offset = outside ? HighwayRoute.SeparatedBranchOffset(distance, 0, length, outsideOffset, transition) : 0;
        return transform.position + transform.forward * distance + transform.right * offset + Vector3.up * Height(distance);
    }

    [Header("Grade separation: both routes climb over the S3 east pier")]
    public float liftHeight;
    public float liftRiseStart = 2, liftTopStart = 40, liftTopEnd = 64, liftFallEnd = 100, liftEase = 6;

    // Deck height above the fork for a branch distance: linear ramps with eased ends.
    public float Height(float distance)
        => RampProfile(distance, liftHeight, liftRiseStart, liftTopStart, liftTopEnd, liftFallEnd, liftEase);
    public float HeightAt(Vector3 world) => Height(Vector3.Dot(world - transform.position, transform.forward));
    // True over the indoor market floor (not the outside pier), where the raised floor applies.
    public bool OnMarketFloor(Vector3 world)
    {
        float d = Vector3.Dot(world - transform.position, transform.forward);
        return d >= 0 && d <= length && Mathf.Abs(Vector3.Dot(world - transform.position, transform.right)) < 9;
    }

    // On the ramps the shark (and its straight-flying shots) aim at the highest floor within 30 m
    // instead of the slope under its feet: climbing it looks up at the level deck where enemies wait,
    // on the deck and going down it stays level so shots never dive under enemies.
    public const float AimLookAhead = 30;
    public Vector3 AimForward(float distance, Vector3 tangent)
    {
        var flat = Vector3.ProjectOnPlane(tangent, Vector3.up);
        if (liftHeight <= 0 || flat.sqrMagnitude < .0001f) return tangent;
        float here = Height(distance), highest = here;
        for (float s = 2; s <= AimLookAhead; s += 2) highest = Mathf.Max(highest, Height(Mathf.Min(length, distance + s)));
        return (flat.normalized * AimLookAhead + Vector3.up * (highest - here)).normalized;
    }

    public static float RampProfile(float d, float height, float riseStart, float topStart, float topEnd, float fallEnd, float ease)
    {
        if (height <= 0 || d <= riseStart || d >= fallEnd) return 0;
        if (d >= topStart && d <= topEnd) return height;
        bool rising = d < topStart;
        float length = rising ? topStart - riseStart : fallEnd - topEnd;
        float s = rising ? d - riseStart : fallEnd - d;
        float e = Mathf.Clamp(ease, 0, length * .45f);
        float slope = height / Mathf.Max(.01f, length - e);
        if (s < e) return slope * s * s / (2 * e);
        if (s < length - e) return slope * (s - e * .5f);
        float r = length - s;
        return height - slope * r * r / (2 * e);
    }
    public void Sample(float distance, bool outside, out Vector3 center, out Vector3 forward)
    {
        center = Point(distance, outside);
        forward = (Point(Mathf.Min(length, distance + .5f), outside) - Point(Mathf.Max(0, distance - .5f), outside)).normalized;
        if (forward.sqrMagnitude < .1f) forward = transform.forward;
    }
    public bool Advance(PlayerScript player, float seconds, float multiplier)
    {
        if (!Driving || player != Player) return false;
        Sample(Distance, Outside, out var previous, out var heading);
        float lane = Vector3.Dot(player.transform.position - previous, Vector3.Cross(Vector3.up, heading).normalized);
        Distance = Mathf.Min(length, Distance + travelSpeed * seconds * Mathf.Max(0, multiplier));
        Sample(Distance, Outside, out var center, out var forward);
        player.ApplyContinuousRoutePose(center + Vector3.up * .12f, AimForward(Distance, forward), Mathf.Clamp(lane, -2.7f, 2.7f));
        if (Distance >= length)
        {
            Driving = false; Complete = true;
            Director.QuietFor(4);
            Director.Hud?.Publish("앞쪽 활어 경매장으로 이동하세요", 2.5f);
            Director.Timeline.Add($"fork merged at {Director.Elapsed:F1}");
        }
        return true;
    }
    private void CancelChoice() { if (!Selected) opened = false; }
    private void OnDestroy()
    {
        if (choiceUI != null) { choiceUI.Chosen -= Choose; choiceUI.Cancelled -= CancelChoice; }
    }
}
