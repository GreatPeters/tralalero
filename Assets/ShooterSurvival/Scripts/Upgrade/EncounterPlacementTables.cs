using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ExcelDataReader;
using IndianOceanAssets.ShooterSurvival;

public sealed class EncounterPlacementRow
{
    public string scene, id, kind;
    public bool enabled;
    public EnemyEventMode mode;
    public float moveSpeed, moveDistance, activationLead, throwDelay, throwSpeed, effectValue;
    public Rarity rarity;
    public ObstaclePattern pattern;
    public bool hasCombatStats;
    public float damage, health;
    public EnemyTier tier;
    public bool hasHighwaySettings;
    public float durability, warningSeconds, operationSeconds, crossingDistance;
    public bool dropBonusAltar=true;
    public int coinReward=-1;
}

// One atomic snapshot. Placed combat stats override legacy chapter growth; bonus effects remain in BonusTables.
public static class EncounterPlacementTables
{
    public static readonly string[] SheetNames = { "적 배치", "보너스 배치", "기믹 배치" };
    private static IReadOnlyList<EncounterPlacementRow> cached;
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => cached = null;
    public static IReadOnlyList<EncounterPlacementRow> Rows
    {
        get { if (cached == null) Reload(); return cached; }
    }
    public static void Reload()
    {
        using var stream = GameDataWorkbook.OpenRead("Data.xlsx");
        cached = Read(stream);
    }
    public static IReadOnlyList<EncounterPlacementRow> Read(Stream stream)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var rows = new List<EncounterPlacementRow>();
        var sheets = new HashSet<string>();
        do
        {
            int kind = Array.IndexOf(SheetNames, reader.Name?.Trim());
            if (kind < 0) continue;
            sheets.Add(SheetNames[kind]);
            Dictionary<string, int> headers = null;
            while (reader.Read())
            {
                if (headers == null)
                {
                    var names = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i)?.ToString()?.Trim() ?? "").ToArray();
                    if (!names.Contains("배치ID")) continue;
                    headers = names.Select((n, i) => (n, i)).Where(p => p.n.Length > 0).ToDictionary(p => p.n, p => p.i);
                    continue;
                }
                string Text(string name)
                {
                    if (!headers.TryGetValue(name, out int i)) throw new InvalidDataException($"{reader.Name}: 필수 열 '{name}' 없음");
                    return Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)?.Trim() ?? "";
                }
                if (Enumerable.Range(0, reader.FieldCount).All(i => string.IsNullOrWhiteSpace(reader.GetValue(i)?.ToString()))) continue;
                var row = new EncounterPlacementRow { scene = Text("맵"), id = Text("배치ID"), kind = SheetNames[kind] };
                string enabled = Text("사용");
                if (enabled != "0" && enabled != "1") throw new InvalidDataException($"{row.id}: 사용은 0 또는 1이어야 합니다.");
                row.enabled = enabled == "1";
                if (kind == 0)
                {
                    string OptionalReward(string name)=>headers.TryGetValue(name,out int column)?Convert.ToString(reader.GetValue(column),CultureInfo.InvariantCulture)?.Trim()??"":"";
                    string drop=OptionalReward("보너스드롭"),coins=OptionalReward("코인보상");
                    if(drop.Length>0){if(drop!="0"&&drop!="1")throw new InvalidDataException(row.id+": 보너스드롭은 0 또는 1이어야 합니다.");row.dropBonusAltar=drop=="1";}
                    if(coins.Length>0 && (!int.TryParse(coins,NumberStyles.Integer,CultureInfo.InvariantCulture,out row.coinReward)||row.coinReward<0))throw new InvalidDataException(row.id+": 코인보상은 0 이상 정수여야 합니다.");
                    row.mode = ParseMode(Text("이벤트"));
                    row.moveSpeed = Number(Text("이동속도"), "이동속도");
                    row.moveDistance = Number(Text("이동거리"), "이동거리");
                    row.activationLead = Number(Text("발동앞거리"), "발동앞거리");
                    row.throwDelay = Number(Text("발사준비초"), "발사준비초");
                    row.throwSpeed = Number(Text("투사체속도"), "투사체속도");
                    bool anyStats = new[] { "공격력", "체력", "타입" }.Any(headers.ContainsKey);
                    if (anyStats)
                    {
                        row.hasCombatStats = true;
                        row.damage = Number(Text("공격력"), "공격력");
                        row.health = Number(Text("체력"), "체력");
                        if (!Enum.TryParse(Text("타입"), out row.tier) || !Enum.IsDefined(typeof(EnemyTier), row.tier))
                            throw new InvalidDataException(row.id + ": 타입은 Normal/Elite/Boss 중 하나여야 합니다.");
                    }
                }
                else if (kind == 1)
                {
                    if (!Enum.TryParse(Text("등급"), out row.rarity) || !Enum.IsDefined(typeof(Rarity), row.rarity))
                        throw new InvalidDataException($"{row.id}: 등급은 Normal/Rare/Unique 중 하나여야 합니다.");
                }
                else
                {
                    if (!Enum.TryParse(Text("기믹종류"), out row.pattern) || !Enum.IsDefined(typeof(ObstaclePattern), row.pattern) || row.pattern == ObstaclePattern.None)
                        throw new InvalidDataException($"{row.id}: 잘못된 기믹종류");
                    row.effectValue = Number(Text("효과값"), "효과값");
                    string Optional(string name) => headers.TryGetValue(name, out int column)
                        ? Convert.ToString(reader.GetValue(column), CultureInfo.InvariantCulture)?.Trim() ?? "" : "";
                    string[] settings = { Optional("내구도"), Optional("예고초"), Optional("작동초"), Optional("이동폭") };
                    row.hasHighwaySettings = settings.Any(s => s.Length > 0);
                    if (row.hasHighwaySettings)
                    {
                        float Value(int index) => settings[index].Length == 0 ? 0 : Number(settings[index], "고속도로 기믹 설정");
                        row.durability = Value(0); row.warningSeconds = Value(1); row.operationSeconds = Value(2); row.crossingDistance = Value(3);
                    }
                }
                rows.Add(row);
            }
            if (headers == null) throw new InvalidDataException($"{reader.Name}: 배치ID 헤더가 없습니다.");
        } while (reader.NextResult());
        if (sheets.Count != 0 && sheets.Count != 3) throw new InvalidDataException("적 배치/보너스 배치/기믹 배치 시트가 모두 필요합니다.");
        ValidateRows(rows);
        return rows.AsReadOnly();
    }
    public static void ValidateRows(IEnumerable<EncounterPlacementRow> rows)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.scene) || string.IsNullOrWhiteSpace(row.id) || !keys.Add(row.scene + "/" + row.id))
                throw new InvalidDataException("맵/배치ID는 비어 있거나 중복될 수 없습니다.");
            if (row.kind == SheetNames[0])
            {
                if(row.coinReward < -1)throw new InvalidDataException(row.id+": 잘못된 코인보상");
                if (!Enum.IsDefined(typeof(EnemyEventMode), row.mode)) throw new InvalidDataException(row.id + ": 잘못된 적 이벤트");
                if (row.hasCombatStats && (!Enum.IsDefined(typeof(EnemyTier), row.tier) ||
                    float.IsNaN(row.damage) || float.IsInfinity(row.damage) || row.damage < 0 ||
                    float.IsNaN(row.health) || float.IsInfinity(row.health) || row.health <= 0))
                    throw new InvalidDataException(row.id + ": 공격력은 0 이상, 체력은 양수, 타입은 Normal/Elite/Boss여야 합니다.");
                foreach (float value in new[] { row.moveSpeed, row.moveDistance, row.activationLead, row.throwDelay, row.throwSpeed })
                    if (float.IsNaN(value) || float.IsInfinity(value) || value < 0) throw new InvalidDataException(row.id + ": 음수/비정상 수치");
                if (EnemyEventController.RequiresTarget(row.mode) && (row.moveDistance <= 0 || row.moveSpeed <= 0))
                    throw new InvalidDataException(row.id + ": 이동 이벤트는 양수 이동거리와 속도가 필요합니다.");
                if ((row.mode == EnemyEventMode.Shoot || row.mode == EnemyEventMode.AmbushMoveThenShoot) && row.throwSpeed <= 0)
                    throw new InvalidDataException(row.id + ": 사격 속도는 양수여야 합니다.");
            }
            if (row.kind == SheetNames[1] && !Enum.IsDefined(typeof(Rarity), row.rarity))
                throw new InvalidDataException(row.id + ": 잘못된 제단 등급");
            if (row.kind == SheetNames[2] && (row.effectValue < 0 || float.IsNaN(row.effectValue) || float.IsInfinity(row.effectValue)))
                throw new InvalidDataException(row.id + ": 잘못된 효과값");
            if (row.hasHighwaySettings)
            {
                foreach (float value in new[] { row.durability, row.warningSeconds, row.operationSeconds, row.crossingDistance })
                    if (value < 0 || float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException(row.id + ": 잘못된 기믹 설정");
                if (row.pattern == ObstaclePattern.HighwayRoadblock && row.durability <= 0) throw new InvalidDataException(row.id + ": 내구도는 양수여야 합니다.");
                if ((row.pattern == ObstaclePattern.HighwayTraffic || row.pattern == ObstaclePattern.HighwayToll) && row.operationSeconds < 1) throw new InvalidDataException(row.id + ": 작동초는 1 이상이어야 합니다.");
            }
        }
    }
    public static EnemyEventMode ParseMode(string value) => value switch
    {
        "공격 반복" => EnemyEventMode.AttackLoop,
        "공격 한 번" => EnemyEventMode.AttackOnce,
        "사격" => EnemyEventMode.Shoot,
        "왕복" => EnemyEventMode.PatrolBetweenStartAndTarget,
        "이동 후 공격" => EnemyEventMode.MoveToTargetThenAttack,
        "전방 매복 사격" => EnemyEventMode.AmbushMoveThenShoot,
        _ => throw new InvalidDataException("알 수 없는 적 이벤트: " + value)
    };
    private static float Number(string value, string field)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) || float.IsNaN(result) || float.IsInfinity(result) || result < 0)
            throw new InvalidDataException(field + ": 0 이상의 유효한 숫자가 필요합니다.");
        return result;
    }
}
