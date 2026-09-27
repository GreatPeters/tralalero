using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Rest-stop dodge gimmicks that complement HighwayHazard's roadblock/crossing/toll rules.
// The station's forward axis is the route direction; lanes are lateral offsets from its origin.
// Contact costs a share of max health instead of an instant death, so a missed read is survivable.
public sealed class RestStopRouteGimmick : MonoBehaviour
{
    public enum Kind
    {
        [InspectorName("굴러오는 물체(캐리어·캔·트레이)")] Rolling = 0,
        [InspectorName("뒤에서 추월하는 순찰차")] ChaseCar = 1,
    }

    public Kind kind;
    [Tooltip("굴러오는 물체 원본 또는 추월 차량 외형(비활성 템플릿)")]
    public GameObject visualTemplate;
    public float[] lanes = { -2.8f, 0f, 2.8f };
    public float triggerDistance = 40f, warningSeconds = 1.2f, speed = 9f, travel = 60f, damageFraction = .2f, hitRadius = 1.3f;
    [Tooltip("굴러오는 물체: 레인 순서대로 나오는 간격(초)")]
    public float stagger = .45f;
    public GameObject[] laneWarnings = System.Array.Empty<GameObject>();

    private PlayerScript player;
    private ChapterProgression chapter;
    private readonly List<(Transform t, int lane, float start)> movers = new();
    private float activatedAt = -1;
    private bool hitPlayer;

    public static float MoverOffset(float elapsed, float start, float warning, float speed, float travel)
        => Mathf.Clamp((elapsed - start - warning) * speed, 0, travel);

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerScript>();
        chapter = FindFirstObjectByType<ChapterProgression>();
        ResetForRun();
    }

    public void ResetForRun()
    {
        foreach (var m in movers) if (m.t != null) Destroy(m.t.gameObject);
        movers.Clear(); activatedAt = -1; hitPlayer = false;
        foreach (var w in laneWarnings) if (w != null) w.SetActive(false);
    }

    private void Update()
    {
        if (player == null || chapter == null || !TimeManager.isGameRunning) return;
        float clock = chapter.Elapsed;
        if (clock < 0.05f && activatedAt >= 0) ResetForRun();
        Vector3 local = transform.InverseTransformPoint(player.transform.position);
        if (activatedAt < 0)
        {
            // Rolling objects come from ahead; the chase car arrives from behind once the player passes.
            bool trigger = kind == Kind.Rolling ? local.z > -triggerDistance && local.z < 0 : local.z > 0 && local.z < 6;
            if (!trigger) return;
            Activate(clock);
        }
        float t = clock - activatedAt;
        foreach (var w in laneWarnings) if (w != null) w.SetActive(t < warningSeconds);
        for (int i = 0; i < movers.Count; i++)
        {
            var (mover, lane, start) = movers[i];
            if (mover == null) continue;
            float offset = MoverOffset(t, start, warningSeconds, speed, travel);
            mover.gameObject.SetActive(t >= start);
            float along = kind == Kind.Rolling ? -offset : offset - 14f;
            mover.position = transform.TransformPoint(new Vector3(lanes[lane], 0, along));
            if (kind == Kind.Rolling) mover.Rotate(Vector3.right, -speed * 90f * Time.deltaTime, Space.Self);
            if (!hitPlayer && offset > 0 && offset < travel)
            {
                Vector3 d = player.transform.position - mover.position; d.y = 0;
                if (d.sqrMagnitude < hitRadius * hitRadius)
                {
                    hitPlayer = true;
                    player.ApplyDamage(player.MaxHealth * damageFraction, kind == Kind.ChaseCar ? PlayerDamageCause.Traffic : PlayerDamageCause.Other);
                }
            }
        }
    }

    private void Activate(float clock)
    {
        activatedAt = clock;
        GameAudioService.Play(GameSound.Warning);
        if (visualTemplate == null) return;
        int count = kind == Kind.Rolling ? lanes.Length : 1;
        // The chase car takes the lane the player is not in, so reading the warning always offers an escape.
        int chaseLane = 0;
        if (kind == Kind.ChaseCar)
        {
            float x = transform.InverseTransformPoint(player.transform.position).x;
            chaseLane = Mathf.Abs(x - lanes[0]) < Mathf.Abs(x - lanes[lanes.Length - 1]) ? lanes.Length - 1 : 0;
        }
        for (int i = 0; i < count; i++)
        {
            // Rolling objects leave one lane open in rotation so there is always a gap.
            int lane = kind == Kind.Rolling ? i : chaseLane;
            if (kind == Kind.Rolling && lanes.Length > 1 && i == (Mathf.FloorToInt(clock) % lanes.Length)) continue;
            var go = Instantiate(visualTemplate, transform);
            go.SetActive(false);
            movers.Add((go.transform, lane, kind == Kind.Rolling ? i * stagger : 0));
        }
    }
}
