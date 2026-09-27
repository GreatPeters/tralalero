using System;
using System.Collections.Generic;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(150)]
[DisallowMultipleComponent]
public sealed class HighwayChapter2Controller : MonoBehaviour
{
    [Serializable] public sealed class MysteryGate
    {
        public Transform root;
        public TMP_Text[] questions;
        public Transform[] icons;
        [NonSerialized] public bool claimed;
    }
    public HighwayChapter2UI ui;
    public HighwayChapter2Feedback feedback;
    public Transform[] oppositeCars = Array.Empty<Transform>();
    public Transform logTruck, singleLog, workRoot;
    public Transform[] workers = Array.Empty<Transform>(), thrownCones = Array.Empty<Transform>();
    public WaterDeerCrossing deer;
    public RoadPotholeHazard hole;
    public MysteryGate[] mysteryGates = Array.Empty<MysteryGate>();
    public Transform[] uniqueCards = Array.Empty<Transform>();
    public TMP_Text[] uniqueNames = Array.Empty<TMP_Text>();
    public SpriteRenderer[] uniqueIcons = Array.Empty<SpriteRenderer>();
    public Renderer[] tollRoof = Array.Empty<Renderer>();
    public HighwayUniquePickup[] exitPickups = Array.Empty<HighwayUniquePickup>();
    public Transform shieldAura;
    public static HighwayChapter2Controller Active { get; private set; }
    public HighwayRoute Route { get; private set; }
    public PlayerScript Player { get; private set; }
    public IReadOnlyList<HighwayVehicleEnemy> Vehicles => vehicles;
    public int RoadChoice { get; private set; } = -1;
    public int ExitChoice { get; private set; } = -1;
    public HighwayUniqueBonus LeftBonus { get; private set; }
    public HighwayUniqueBonus RightBonus { get; private set; }
    public HighwayUniqueBonus? GrantedBonus { get; private set; }
    public bool PopupPending => ui != null && ui.Pending;
    public bool Running { get; private set; }
    public bool RushActive => Route != null && HighwayChapter2Rules.IsRushSegment(Running,RoadChoice,Route.Distance,S("forkAt"),S("mergeAt"));
    public float SpeedMultiplier => RushActive ? S("openPlayerMultiplier") : 1;
    public bool LogActive { get; private set; }
    public HighwayPrimaryHazard PrimaryHazard { get; private set; }
    public int TankerChains { get; private set; }
    public int DestroyedVehicles { get; private set; }
    public int RandomBonuses { get; private set; }
    public int LogsReleased { get; private set; }
    public int ShieldBlocks { get; private set; }
    public int VehicleProjectileHits { get; private set; }
    public readonly List<string> Timeline = new();
    private HighwayVehicleEnemy[] vehicles = Array.Empty<HighwayVehicleEnemy>();
    private readonly HashSet<int> clearedAccidents = new();
    private readonly HashSet<HighwayVehicleEnemy> nearMissed = new();
    private readonly List<HighwayVehicleEnemy> moving = new();
    private readonly List<HighwayVehicleEnemy[]> trafficGroups = new();
    private float elapsed, quietUntil, priorTimeScale = 1;
    private bool clockOwned, shieldReady, logDone, logReleased, logHit;
    private float logBegan, logStation, logLane, lastBounce, nextCone;
    private float logTruckDistance;
    private BoxCollider logCollider;
    private Collider playerCollider;
    private float[] oppositeDistances = Array.Empty<float>();
    private bool[] oppositeHeld = Array.Empty<bool>();
    private int[] oppositeOrder = Array.Empty<int>();
    private bool workContact;
    private readonly List<ConeFlight> cones = new();
    private sealed class ConeFlight { public Transform t; public Vector3 from, to; public float began; public bool hit; }
    private struct Window { public float from, to; public HighwayPrimaryHazard kind; public int group; }
    private readonly List<Window> windows = new();
    private static float S(string key) => HighwayChapter2Data.Value(key);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Active = null;
    public static HighwayChapter2Controller For(PlayerScript player)
        => Active != null && player != null && Active.gameObject.scene == player.gameObject.scene ? Active : null;

    private void Awake()
    {
        if (gameObject.scene.name != "HighWay") { enabled = false; return; }
        Active = this; Route = GetComponent<HighwayRoute>();
        Player = FindFirstObjectByType<PlayerScript>();
        vehicles = GetComponentsInChildren<HighwayVehicleEnemy>(true);
        if (ui != null) { ui.Chosen = Choose; ui.Cancelled = RestoreClock; }
        foreach (var vehicle in vehicles) { vehicle.Defeated = OnVehicleDefeated; vehicle.ProjectileHit = OnProjectileHit; }
    }

    public void BeginRun()
    {
        Active = this; Running = true; elapsed = quietUntil = 0;
        RoadChoice = ExitChoice = -1; GrantedBonus = null; shieldReady = false;
        TankerChains = DestroyedVehicles = RandomBonuses = LogsReleased = ShieldBlocks = VehicleProjectileHits = 0;
        clearedAccidents.Clear(); nearMissed.Clear(); Timeline.Clear(); RestoreClock();
        GetComponent<EncounterPlacementController>()?.BeginNewRun();
        vehicles = GetComponentsInChildren<HighwayVehicleEnemy>(true);
        foreach (var vehicle in vehicles) { vehicle.Defeated = OnVehicleDefeated; vehicle.ProjectileHit = OnProjectileHit; vehicle.ResetForRun(); }
        trafficGroups.Clear();trafficGroups.AddRange(vehicles.GroupBy(v=>v.group).Select(g=>g.ToArray()));
        Player.originalMoveSpeed = S("runSpeed");
        ui?.ResetForRun(); feedback?.Prepare(Player);
        LeftBonus = (HighwayUniqueBonus)UnityEngine.Random.Range(0, Enum.GetValues(typeof(HighwayUniqueBonus)).Length);
        RightBonus = (HighwayUniqueBonus)(((int)LeftBonus + UnityEngine.Random.Range(1, Enum.GetValues(typeof(HighwayUniqueBonus)).Length)) % Enum.GetValues(typeof(HighwayUniqueBonus)).Length);
        for (int i = 0; i < uniqueCards.Length; i++)
        {
            uniqueCards[i].gameObject.SetActive(true);
            var bonus = i == 0 ? LeftBonus : RightBonus;
            if (i < uniqueNames.Length) uniqueNames[i].text = "유니크 보너스\n" + HighwayChapter2UI.BonusName(bonus);
            if (i < uniqueIcons.Length) uniqueIcons[i].sprite = ui.BonusSprite(bonus);
        }
        foreach (var gate in mysteryGates) { gate.claimed = false; gate.root.gameObject.SetActive(true); }
        logDone = logReleased = logHit = LogActive = false;
        logCollider=singleLog!=null?singleLog.GetComponent<BoxCollider>():null;playerCollider=Player.GetComponent<Collider>();
        oppositeDistances = new float[oppositeCars.Length];
        oppositeHeld = new bool[oppositeCars.Length];
        oppositeOrder = Enumerable.Range(0, oppositeCars.Length).ToArray();
        for (int i = 0; i < oppositeDistances.Length; i++) oppositeDistances[i] = (i / 3) * S("oppositeGap") - S("trafficRecycleBehind");
        if (logTruck != null) logTruck.gameObject.SetActive(false);
        if (singleLog != null) singleLog.gameObject.SetActive(false);
        foreach (var cone in thrownCones) cone.gameObject.SetActive(false);
        cones.Clear(); nextCone = 0; workContact = false; deer?.ResetForRun();
        if (hole != null) { hole.gameObject.SetActive(false); hole.gameObject.SetActive(true); }
        BuildWindows();
    }

    private void BuildWindows()
    {
        windows.Clear();
        for (int i = 1; i <= 4; i++) windows.Add(new Window { from = S("crash" + i) - S("crashLead"), to = S("crash" + i) + S("crashTail"), kind = HighwayPrimaryHazard.Accident, group = i });
        windows.Add(new Window { from = S("swarmStart"), to = S("swarmEnd"), kind = HighwayPrimaryHazard.Swarm, group = 6 });
        windows.Add(new Window { from = S("logStart"), to = S("logEnd"), kind = HighwayPrimaryHazard.Log });
        windows.Add(new Window { from = S("deerAt") - S("deerLead"), to = S("deerAt") + S("deerTail"), kind = HighwayPrimaryHazard.Deer });
        windows.Add(new Window { from = S("holeAt") - S("holeLead"), to = S("holeAt") + S("holeTail"), kind = HighwayPrimaryHazard.Hole });
        windows.Add(new Window { from = S("workAt") - S("workLead"), to = S("workAt") + S("workTail"), kind = HighwayPrimaryHazard.Work });
    }

    public bool WindowEnabled(int group) => group != 2 && group != 3 || RoadChoice == 0;
    public bool UsesBranch(HighwayVehicleEnemy vehicle)
        => vehicle.routeChoice == HighwayVehicleRoute.Open || vehicle.routeChoice == HighwayVehicleRoute.Common && ExitChoice == 1 && vehicle.Distance >= S("tollAt");
    private bool RouteEnabled(HighwayVehicleEnemy vehicle)
        => vehicle.routeChoice == HighwayVehicleRoute.Common || vehicle.routeChoice == HighwayVehicleRoute.Jam && RoadChoice == 0 || vehicle.routeChoice == HighwayVehicleRoute.Open && RoadChoice == 1;

    private void Update()
    {
        if (!Running || Player == null || Route == null) return;
        if (!TimeManager.isGameRunning || Player.currentHealth <= 0) { Running = false; ui?.SetRush(false); RestoreClock(); return; }
        float dt = Time.deltaTime * Mathf.Max(0, TimeManager.timeFactor);
        if (dt <= 0) return;
        elapsed += dt;
        float pd = Route.Distance;
        if (RoadChoice < 0 && pd >= S("forkAt") - S("popupAhead") && !PopupPending) OpenChoice(false);
        ui?.SetRush(RushActive);
        // A missed physical pickup stays on the main exit and grants no bonus.
        if (ExitChoice < 0 && pd >= S("tollAt")) { ExitChoice=0; Route.SelectFork(1,false);foreach(var card in uniqueCards)card.gameObject.SetActive(false); }
        PrimaryHazard = HighwayPrimaryHazard.None;
        foreach (var window in windows) if (WindowEnabled(window.group) && pd >= window.from && pd <= window.to) PrimaryHazard = window.kind;
        TickLog(dt); TickWorkers(dt); TickBonuses(dt); TickOpposite(dt); TickTraffic(dt);
        if (shieldAura != null)
        {
            shieldAura.gameObject.SetActive(shieldReady);
            shieldAura.position = Player.transform.position + Vector3.up * 1.1f;
        }
        foreach (var card in uniqueCards) if (card != null && card.gameObject.activeSelf) card.localScale = Vector3.one * (1 + Mathf.Sin(elapsed * 3) * .035f);
        foreach (var roof in tollRoof) if (roof != null) roof.enabled = Mathf.Abs(pd - (S("tollAt") + S("crashQueueGap"))) > S("tollRoofHide");
    }

    private void OpenChoice(bool toll)
    {
        priorTimeScale = Time.timeScale; clockOwned = true;
        Time.timeScale = priorTimeScale * S("popupSlow");
        ui.Open(toll, LeftBonus, RightBonus);
        Timeline.Add((toll ? "exit popup " : "branch popup ") + Route.Distance.ToString("F1"));
    }
    public void Choose(int index)
    {
        if (index < 0 || index > 1) return;
        bool toll = ui != null && ui.TollChoice;
        if (toll)
        {
            if (ExitChoice >= 0) return;
            ExitChoice = index; Route.SelectFork(1, index == 1);
            ui.Publish(index == 0 ? "상어, 하이패스 출구로 향하다" : "상어, 현금 출구로 향하다");
        }
        else
        {
            if (RoadChoice >= 0) return;
            RoadChoice = index; Route.SelectFork(0, index == 1);
            ui.Publish(index == 0 ? "상어, 정체 구간에 진입" : "상어, 뻥 뚫린 길을 선택");
        }
        Timeline.Add((toll ? "exit " : "road ") + index + " at " + Route.Distance.ToString("F1"));
        RestoreClock();
    }
    private void RestoreClock()
    {
        if (!clockOwned) return;
        Time.timeScale = priorTimeScale; clockOwned = false;
    }

    public float ConstrainLane(float lane, float distance, float step)
    {
        if (PopupPending) return Mathf.MoveTowards(lane, 0, step);
        return lane;
    }

    public bool TryCollectExit(int index, PlayerScript player)
    {
        if (!Running || !TimeManager.isGameRunning || player==null || player.currentHealth<=0 || player != Player || ExitChoice >= 0 || index < 0 || index >= exitPickups.Length || !exitPickups[index].Touches(player)) return false;
        float lane=RouteFrame.Lane(player.transform.position,Route.Distance);
        if(index!=(lane>0?1:0))return false;
        ExitChoice=index;Route.SelectFork(1,index==1);GrantUnique();
        Timeline.Add("exit pickup "+index+" at "+Route.Distance.ToString("F1"));return true;
    }

    public bool IsProtected(float meeting, int ownGroup = 0)
    {
        if (meeting <= quietUntil || PopupPending) return true;
        foreach (float fork in new[] { S("forkAt"), S("tollAt") })
            if (meeting >= fork - S("popupAhead") - S("popupQuiet") && meeting <= fork + S("popupQuiet")) return true;
        foreach (var window in windows)
            if (WindowEnabled(window.group) && (ownGroup == 0 || window.group != ownGroup)
                && HighwayChapter2Rules.ConflictsWithWindow(meeting, window.from, window.to, S("separation"))) return true;
        return false;
    }

    private void TickTraffic(float dt)
    {
        float pd = Route.Distance;
        moving.Clear();
        foreach (var vehicle in vehicles) if (vehicle.Active && !vehicle.Dead) moving.Add(vehicle);
        foreach (var group in trafficGroups)
        {
            var candidate=group[0];
            if (candidate.Spawned || pd < candidate.spawnAt || !RouteEnabled(candidate)) continue;
            if (candidate.Distance < pd + S("crashLead") && candidate.speed > 0) { foreach(var car in group)car.Skip(); continue; }
            float meeting = OncomingLaneTraffic.MeetingDistance(pd, candidate.Distance, Player.ForwardMoveSpeed, candidate.speed);
            if (candidate.speed > 0 && IsProtected(meeting, candidate.group)) continue;
            if (candidate.group == 6 && (pd < S("swarmStart") - S("crashQueueGap") || pd > S("swarmEnd"))) continue;
            if (LogActive && candidate.speed > 0) continue;
            bool fits=true;foreach(var car in group)if(!SpawnFits(car)){fits=false;break;}if(!fits||!GroupClearAt(group,0,true))continue;
            float coins = candidate.routeChoice == HighwayVehicleRoute.Jam ? S("jamCoin") : candidate.routeChoice == HighwayVehicleRoute.Open ? S("openCoin") : 1;
            // Admit the whole front row atomically. A destroyed member opens its lane.
            foreach(var car in group){car.Activate(car.routeChoice==HighwayVehicleRoute.Open,coins);car.Place(Route,UsesBranch(car));moving.Add(car);}
        }
        trafficGroups.Sort((a,b)=>FrontDistance(a).CompareTo(FrontDistance(b)));
        foreach(var group in trafficGroups)
        {
            float travel=float.PositiveInfinity;
            foreach(var car in group)
            {
                if(!car.Active||car.Dead)continue;
                float next=car.Distance-(LogActive||PopupPending?0:car.speed*dt);
                foreach(var leader in moving)
                {
                    if(leader.group==car.group||!leader.Active||leader.Dead||leader.Distance>=car.Distance||UsesBranch(leader)!=UsesBranch(car)||Mathf.Abs(car.Lane-leader.Lane)>=car.HalfWidth+leader.HalfWidth+.1f)continue;
                    next=HighwayChapter2Rules.FollowingDistance(next,car.Distance,leader.Distance,car.HalfLength,leader.HalfLength,S("followGap"));
                }
                if(car.Distance>S("workAt")&&Mathf.Abs(car.Lane-HighwayChapter2Data.Lane(S("workLane")))<car.HalfWidth+1)
                    next=Mathf.Min(car.Distance,Mathf.Max(next,S("workAt")+S("workTail")+car.HalfLength+S("followGap")));
                if(car.Distance>S("holeAt")&&Mathf.Abs(car.Lane-HighwayChapter2Data.Lane(S("holeLane")))<car.HalfWidth+1.2f)
                    next=Mathf.Min(car.Distance,Mathf.Max(next,S("holeAt")+car.HalfLength+S("followGap")));
                travel=Mathf.Min(travel,Mathf.Max(0,car.Distance-next));
            }
            if(float.IsPositiveInfinity(travel))continue;
            if(travel>0&&!GroupClearAt(group,travel,false))
            {
                float low=0,high=travel;
                for(int attempt=0;attempt<7;attempt++){float mid=(low+high)*.5f;if(GroupClearAt(group,mid,false))low=mid;else high=mid;}
                travel=low;
            }
            // Shared braking preserves the wall until a member is actually destroyed.
            foreach(var car in group)
            {
                if(!car.Active||car.Dead)continue;car.Distance-=travel;car.Place(Route,UsesBranch(car));
                if(car.Distance<0||car.Distance<pd-S("trafficRecycleBehind"))car.Retire();
            }
        }
    }

    private static float FrontDistance(HighwayVehicleEnemy[] group)
    {float result=float.PositiveInfinity;foreach(var car in group)if(car.Active&&!car.Dead)result=Mathf.Min(result,car.Distance);return result;}

    private bool GroupClearAt(HighwayVehicleEnemy[] group,float travel,bool spawning)
    {
        foreach(var car in group)
        {
            if(!spawning&&(!car.Active||car.Dead))continue;
            car.SamplePose(Route,car.Distance-travel,UsesBranch(car),out var position,out var rotation);
            foreach(var other in vehicles)
            {
                if(other==car||other.Dead)continue;
                bool together=other.group==car.group;
                if(!other.Active&&!(spawning&&together))continue;
                Vector3 otherPosition;Quaternion otherRotation;
                if(together)other.SamplePose(Route,other.Distance-travel,UsesBranch(other),out otherPosition,out otherRotation);
                else{otherPosition=other.transform.position;otherRotation=other.transform.rotation;}
                float reach=car.footprint.magnitude+other.footprint.magnitude+1;
                if((position-otherPosition).sqrMagnitude>reach*reach)continue;
                var a=car.ContactCollider;var b=other.ContactCollider;
                if(a!=null&&b!=null&&Physics.ComputePenetration(a,position,rotation,b,otherPosition,otherRotation,out _,out float depth)&&depth>.015f)return false;
            }
        }
        return true;
    }

    private bool SpawnFits(HighwayVehicleEnemy candidate)
    {
        foreach (var other in moving)
        {
            if (UsesBranch(candidate) != UsesBranch(other)) continue;
            if (HighwayChapter2Rules.Overlaps(candidate.Distance, candidate.Lane, candidate.HalfLength, candidate.HalfWidth,
                other.Distance, other.Lane, other.HalfLength, other.HalfWidth, S("followGap"), .1f)) return false;
        }
        return true;
    }

    private void OnVehicleDefeated(HighwayVehicleEnemy vehicle)
    {
        DestroyedVehicles++;
        Route.Sample(vehicle.Distance, UsesBranch(vehicle), out _, out var forward);
        feedback?.DestroyVehicle(vehicle, forward);
        if (vehicle.accidentFront && clearedAccidents.Add(vehicle.group))
        {
            quietUntil = Route.Distance + S("accidentQuietSeconds") * S("runSpeed");
            Timeline.Add("accident " + vehicle.group + " opened at " + Route.Distance.ToString("F1"));
            ui?.Publish("상어, 사고 정체를 뚫었다!");
        }
        if (vehicle.kind != HighwayVehicleKind.Tanker) return;
        TankerChains++;
        float radius = S("tankerRadius") * (GrantedBonus == HighwayUniqueBonus.Shatter ? S("shatterMultiplier") : 1);
        int count = 0;
        foreach (var adjacent in vehicles)
        {
            if (adjacent == vehicle || adjacent.Dead || !adjacent.Active || UsesBranch(adjacent) != UsesBranch(vehicle)) continue;
            if (Vector3.Distance(adjacent.transform.position, vehicle.transform.position) > radius) continue;
            adjacent.Combat.ReceiveVehicleDamage(adjacent.Combat.CurrentHealth); count++;
        }
        if (count > 0) ui?.Publish("상어, 탱크로리 연쇄 폭파로 " + count + "대 파괴");
    }

    private void OnProjectileHit(HighwayVehicleEnemy vehicle, BulletScript projectile)
    {
        if(projectile!=null&&projectile.HasDamagePayload)VehicleProjectileHits++;
        // The shipped shark's default projectile is internally named Water. Both upgrades transform
        // that real weapon too; restricting them to the unused Bomb pool made the rewards inert.
        if (projectile == null || !projectile.HasDamagePayload || !GrantedBonus.HasValue) return;
        if (GrantedBonus == HighwayUniqueBonus.ChainMissile)
        {
            var targets = vehicles.Where(v => v != vehicle && v.Active && !v.Dead && UsesBranch(v) == UsesBranch(vehicle)
                && Vector3.Distance(v.transform.position, vehicle.transform.position) <= S("chainRadius"))
                .OrderBy(v => (v.transform.position - vehicle.transform.position).sqrMagnitude).Take(HighwayChapter2Data.Count("chainBounces")).ToArray();
            Vector3 from = vehicle.transform.position + Vector3.up;
            foreach (var target in targets)
            {
                feedback?.ChainMissile(from, target.transform.position + Vector3.up);
                target.Combat.ReceiveVehicleDamage(projectile.LaunchDamage * S("chainDamage")); from = target.transform.position + Vector3.up;
            }
        }
        else if (GrantedBonus == HighwayUniqueBonus.Shatter)
        {
            feedback?.Shatter(vehicle.transform.position + Vector3.up);
            float radius = S("chainRadius") * S("shatterMultiplier");
            foreach (var target in vehicles.Where(v => v != vehicle && v.Active && !v.Dead).ToArray())
                if (Vector3.Distance(target.transform.position, vehicle.transform.position) <= radius)
                    target.Combat.ReceiveVehicleDamage(projectile.LaunchDamage * S("chainDamage"));
        }
    }

    public bool ConsumeShield(float damage, PlayerDamageCause cause)
    {
        if (!Running || !shieldReady || cause == PlayerDamageCause.Hole || damage < Player.MaxHealth * S("shieldThreshold")) return false;
        shieldReady = false; ShieldBlocks++; ui?.Publish("황금 방패가 큰 충격을 막았다!"); return true;
    }
    public float MagnetRadius => Running && GrantedBonus == HighwayUniqueBonus.CoinMagnet ? S("magnetRadius") : 0;

    private void GrantUnique()
    {
        GrantedBonus = ExitChoice == 0 ? LeftBonus : RightBonus;
        shieldReady = GrantedBonus == HighwayUniqueBonus.GoldenShield;
        foreach (var card in uniqueCards) card.gameObject.SetActive(false);
        ui.Publish("요금소 통과! " + HighwayChapter2UI.BonusName(GrantedBonus.Value) + " 획득");
        Timeline.Add("unique " + GrantedBonus + " at " + Route.Distance.ToString("F1"));
    }

    private void TickBonuses(float dt)
    {
        for (int i = 0; i < mysteryGates.Length; i++)
        {
            var gate = mysteryGates[i]; if (gate.claimed) continue;
            for (int j = 0; j < gate.icons.Length; j++)
            {
                var icon = gate.icons[j]; if (icon == null) continue;
                int corner=j%4;
                icon.localPosition = new Vector3(corner%2==0?-1.25f:1.25f,(corner<2?1.8f:-1.8f)+Mathf.Sin(elapsed*2+j)*.06f,-.57f);
                icon.localRotation = Quaternion.Euler(0,0,Mathf.Sin(elapsed+j)*4);
            }
            foreach (var question in gate.questions) if (question != null) question.color = Color.Lerp(new Color(1, .7f, .06f), Color.white, (Mathf.Sin(elapsed * 5) + 1) * .16f);
            if (Route.Distance < S("mystery" + (i + 1))) continue;
            gate.claimed = true; RandomBonuses++; gate.root.gameObject.SetActive(false);
            int outcome = HighwayChapter2Rules.BonusOutcome(UnityEngine.Random.value, S("largeChance"), S("normalChance"), S("coinChance"), S("missChance"));
            string message;
            if (outcome < 2)
            {
                float percent = S(outcome == 0 ? "largePercent" : "normalPercent");
                int stat = UnityEngine.Random.Range(0, 3);
                if (stat == 0) { Player.ApplyRunAttackPercent(percent); message = "공격력 +" + percent + "%"; }
                else if (stat == 1) { Player.ApplyRunHealthBonus(percent, true); message = "체력 +" + percent + "%"; }
                else { BulletScript.AddMissileDurationPercent(percent); message = "미사일 사거리 +" + percent + "%"; }
            }
            else if (outcome == 2) { int coins = HighwayChapter2Data.Count("mysteryCoins"); MoneyScript.S?.GetCoin(coins); DamagePopupFX.ShowCoin(Player.transform.position + Vector3.up * 2, coins); message = "코인 +" + coins; }
            else { Player.ApplyDamage(Player.MaxHealth * S("missDamage"), PlayerDamageCause.NegativeBonus); message = "꽝! 체력 -5%"; }
            ui.Publish("랜덤 보너스: " + message); Timeline.Add("random " + outcome + " at " + Route.Distance.ToString("F1"));
        }
    }

    private void TickOpposite(float dt)
    {
        Array.Sort(oppositeOrder, (a, b) => oppositeDistances[b].CompareTo(oppositeDistances[a]));
        foreach (int i in oppositeOrder)
        {
            var car = oppositeCars[i]; if (car == null) continue;
            int lane = i % 3;
            bool reserveLogLane = lane == 0 && Route.Distance >= S("logStart") - S("trafficVisibleAhead")
                && (!logDone || logTruck != null && logTruck.gameObject.activeSelf);
            if (oppositeHeld[i])
            {
                if (reserveLogLane) continue;
                oppositeHeld[i] = false; oppositeDistances[i] = Route.Distance - S("trafficRecycleBehind");
            }
            float before = oppositeDistances[i], d = before + S("oppositeSpeed") * dt;
            for (int j = 0; j < oppositeCars.Length; j++)
                if (j != i && !oppositeHeld[j] && j % 3 == lane && oppositeDistances[j] > before)
                    d = Mathf.Min(d, Mathf.Max(before, oppositeDistances[j] - S("oppositeGap")));
            if (lane == 0 && logTruck != null && logTruck.gameObject.activeSelf && logTruckDistance > before)
                d = Mathf.Min(d, Mathf.Max(before, logTruckDistance - S("oppositeGap")));
            if (d > Mathf.Min(Route.length, Route.Distance + S("trafficVisibleAhead")))
            { oppositeDistances[i] = Route.Distance - S("trafficRecycleBehind"); oppositeHeld[i] = reserveLogLane; car.gameObject.SetActive(false); continue; }
            oppositeDistances[i] = d;
            if (d < 0) { car.gameObject.SetActive(false); continue; }
            car.gameObject.SetActive(true);
            Route.Sample(d, false, out var p, out var f);
            float nearEdge = -S("laneWidth") * 1.5f - S("medianShoulder") * 2 - S("medianWidth");
            float offset = nearEdge - S("laneWidth") * (lane + .5f);
            car.SetPositionAndRotation(p + Vector3.Cross(Vector3.up, f) * offset, Quaternion.LookRotation(f));
        }
    }

    private void TickLog(float dt)
    {
        if (logTruck == null || singleLog == null) return;
        if (!LogActive && !logDone && Route.Distance >= S("logStart"))
        {
            LogActive = true; logBegan = elapsed;
            logTruckDistance = Route.Distance + S("crashLead");
            // The preceding northbound convoy drove out of view naturally; its rentals stay held until this truck passes.
            logTruck.gameObject.SetActive(true);
            logLane = UnityEngine.Random.Range(-S("laneWidth"), S("laneWidth"));
            Timeline.Add("single log event " + Route.Distance.ToString("F1"));
        }
        if (logTruck.gameObject.activeSelf)
        {
            float desired = logTruckDistance + S("logTruckSpeed") * dt;
            for (int i = 0; i < oppositeDistances.Length; i += 3)
                if (!oppositeHeld[i] && oppositeDistances[i] > logTruckDistance) desired = Mathf.Min(desired, Mathf.Max(logTruckDistance, oppositeDistances[i] - S("oppositeGap")));
            logTruckDistance = desired;
            Route.Sample(logTruckDistance, false, out var truckCentre, out var truckForward);
            float lane = -S("laneWidth") * 2 - S("medianShoulder") * 2 - S("medianWidth");
            logTruck.SetPositionAndRotation(truckCentre + Vector3.Cross(Vector3.up, truckForward) * lane, Quaternion.LookRotation(truckForward));
            if (logDone && logTruckDistance - Route.Distance > S("trafficVisibleAhead")) logTruck.gameObject.SetActive(false);
        }
        if (!LogActive) return;
        float age = elapsed - logBegan;
        if (!logReleased && age >= S("logReleaseSeconds"))
        {
            logReleased = true; LogsReleased++; logStation = logTruckDistance; lastBounce = -1;
            singleLog.gameObject.SetActive(true); ui?.Publish("트럭에서 통나무가 떨어졌다!");
        }
        if (!logReleased) return;
        float t = age - S("logReleaseSeconds"), duration = S("logSeconds");
        float u = Mathf.Clamp01(t / Mathf.Max(.1f, duration * .3f));
        float fromLane = -S("laneWidth") * 2 - S("medianShoulder") * 2 - S("medianWidth");
        float laneNow = Mathf.Lerp(fromLane, logLane, Mathf.SmoothStep(0, 1, u));
        if (u >= 1) laneNow += Mathf.Sin(t * 3.7f) * .55f + Mathf.Sin(t * 6.1f) * .2f;
        float height = u < 1 ? .8f + Mathf.Sin(u * Mathf.PI) * S("logBounceHeight")
            : .55f + Mathf.Abs(Mathf.Sin(t * 4.3f)) * Mathf.Lerp(1.4f, .15f, Mathf.Clamp01(t / duration));
        float d = logStation - S("logSpeed") * t;
        Route.Sample(d, false, out var p, out var forward);
        singleLog.SetPositionAndRotation(p + Vector3.Cross(Vector3.up, forward) * laneNow + Vector3.up * height,
            Quaternion.LookRotation(Vector3.Cross(Vector3.up, forward)) * Quaternion.Euler(Mathf.Sin(t * 2.7f) * 18, Mathf.Sin(t * 1.3f) * 12, t * 440));
        if (height < .8f && t - lastBounce > .35f) { lastBounce = t; feedback?.Dust(singleLog.position); feedback?.WoodChips(singleLog.position); }
        if (!logHit && logCollider!=null && playerCollider!=null
            &&Physics.ComputePenetration(logCollider,logCollider.transform.position,logCollider.transform.rotation,playerCollider,playerCollider.transform.position,playerCollider.transform.rotation,out _,out _))
        { logHit = true; Player.ApplyDamage(Player.MaxHealth * S("logDamage"), PlayerDamageCause.Other); }
        if (t >= duration)
        {
            LogActive = false; logDone = true; singleLog.gameObject.SetActive(false);
            ui?.Publish("상어, 통나무 구간을 통과했다");
        }
    }

    private void TickWorkers(float dt)
    {
        float pd = Route.Distance;
        bool active = pd >= S("workAt") - S("workLead") && pd <= S("workAt") + S("workTail");
        if (active && elapsed >= nextCone && workers.Length > 0)
        {
            var available = thrownCones.FirstOrDefault(t => t != null && !t.gameObject.activeSelf);
            if (available != null)
            {
                int index = Mathf.FloorToInt(elapsed / S("coneInterval")) % workers.Length;
                var worker = workers[index];
                var animation = worker.GetComponentInChildren<Animator>();
                if (animation != null && animation.HasState(0, Animator.StringToHash("attack_once"))) animation.Play("attack_once", 0, 0);
                float at = Mathf.Min(S("workAt") + S("workTail"), pd + S("runSpeed") * S("coneFlight"));
                Route.Sample(at, false, out var centre, out var forward);
                float lane = (index % 2 == 0 ? -1 : 0) * S("laneWidth");
                Vector3 hand = worker.position + worker.forward * .8f + worker.right * .35f + Vector3.up * 1.1f;
                if (animation != null && animation.isHuman && animation.GetBoneTransform(HumanBodyBones.RightHand) != null) hand = animation.GetBoneTransform(HumanBodyBones.RightHand).position;
                cones.Add(new ConeFlight { t = available, from = hand,
                    to = centre + Vector3.Cross(Vector3.up, forward) * lane, began = elapsed });
                available.gameObject.SetActive(true); nextCone = elapsed + S("coneInterval");
                feedback?.Dust(worker.position);
            }
        }
        for (int i = cones.Count - 1; i >= 0; i--)
        {
            var cone = cones[i]; float u = (elapsed - cone.began) / S("coneFlight");
            if (u >= 1.3f) { cone.t.gameObject.SetActive(false); cones.RemoveAt(i); continue; }
            cone.t.position = Vector3.Lerp(cone.from, cone.to, Mathf.Clamp01(u)) + Vector3.up * (Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI) * 2.5f);
            cone.t.Rotate(Vector3.right, dt * 400, Space.Self);
            if (!cone.hit && Vector3.Distance(cone.t.position, Player.transform.position + Vector3.up) < 1.3f)
            { cone.hit = true; Player.ApplyDamage(Player.MaxHealth * S("coneDamage"), PlayerDamageCause.Other); feedback?.Dust(cone.t.position); }
        }
        if (active && !workContact && Mathf.Abs(pd - S("workAt")) < S("followGap")
            && Mathf.Abs(RouteFrame.Lane(Player.transform.position, pd) - HighwayChapter2Data.Lane(S("workLane"))) < S("vehicleWidth") * .5f)
        { workContact = true; Player.ApplyDamage(Player.MaxHealth * S("coneDamage"), PlayerDamageCause.Roadblock); }
    }

    private void OnDisable()
    {
        RestoreClock(); Running = false;ui?.SetRush(false);
        if (Active == this) Active = null;
    }
}
