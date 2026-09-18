using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExcelDataReader;

public enum CosmeticSlot { Skin, Shoes, Hat }

[Serializable]
public sealed class CosmeticItem
{
    public string id, name, visualKey, description;
    public CosmeticSlot slot;
    public PriceType currency;
    public int price;
    public bool isDefault;
    public string effectStat;
    public float effectValue;
    public ValueType effectValueType = ValueType.Percent;
}

public static class CosmeticTables
{
    public const string SheetName = "커스터마이징";
    private static IReadOnlyList<CosmeticItem> cached;
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => cached = null;
    public static IReadOnlyList<CosmeticItem> Rows
    {
        get { if (cached == null) Reload(); return cached; }
    }
    public static void Reload()
    {
        using var stream = GameDataWorkbook.OpenRead("Data.xlsx");
        cached = Read(stream);
    }
    public static IReadOnlyList<CosmeticItem> Read(Stream stream)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var rows = new List<CosmeticItem>();
        do
        {
            if (reader.Name != SheetName) continue;
            Dictionary<string, int> headers = null;
            while (reader.Read())
            {
                if (headers == null)
                {
                    var names = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i)?.ToString()?.Trim() ?? "").ToArray();
                    if (!names.Contains("ID")) continue;
                    headers = names.Select((v, i) => (v, i)).Where(x => x.v.Length > 0).ToDictionary(x => x.v, x => x.i);
                    continue;
                }
                string Text(string name) => headers.TryGetValue(name, out int column) && column < reader.FieldCount
                    ? Convert.ToString(reader.GetValue(column), System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? "" : "";
                string id = Text("ID"); if (id.Length == 0) continue;
                string effectText = Text("효과값");
                float parsedEffect = 0f;
                if (effectText.Length > 0 && !float.TryParse(effectText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsedEffect))
                    throw new InvalidDataException("Invalid equipment effect value: " + id);
                string effectTypeText = Text("효과타입");
                ValueType parsedEffectType = ValueType.Percent;
                if (effectTypeText.Length > 0 && !Enum.TryParse(effectTypeText, out parsedEffectType))
                    throw new InvalidDataException("Invalid equipment effect type: " + id);
                rows.Add(new CosmeticItem { id = id, slot = (CosmeticSlot)Enum.Parse(typeof(CosmeticSlot), Text("부위")),
                    name = Text("이름"), currency = (PriceType)Enum.Parse(typeof(PriceType), Text("가격타입")),
                    price = int.Parse(Text("가격")), visualKey = Text("비주얼"), isDefault = Text("기본") == "1", description = Text("설명"),
                    effectStat = Text("효과"),
                    effectValue = parsedEffect, effectValueType = parsedEffectType });
            }
            Validate(rows);
            return rows.AsReadOnly();
        } while (reader.NextResult());
        return rows.AsReadOnly(); // Older workbooks/scenes do not install customization.
    }
    public static void Validate(IEnumerable<CosmeticItem> source)
    {
        var rows = source.ToArray();
        if (rows.Any(r => string.IsNullOrWhiteSpace(r.id) || string.IsNullOrWhiteSpace(r.name) || string.IsNullOrWhiteSpace(r.visualKey) ||
            !Enum.IsDefined(typeof(CosmeticSlot), r.slot) || !Enum.IsDefined(typeof(PriceType), r.currency) || r.price < 0 || (r.isDefault && r.price != 0)))
            throw new InvalidDataException("Invalid cosmetic item");
        if (rows.Select(r => r.id).Distinct(StringComparer.Ordinal).Count() != rows.Length)
            throw new InvalidDataException("Duplicate cosmetic ID");
        foreach (var row in rows)
        {
            if (float.IsNaN(row.effectValue) || float.IsInfinity(row.effectValue) || row.effectValue < 0f || row.effectValue > 50f ||
                (row.effectValue != 0f && (!Enum.TryParse(row.effectStat, out UpgradeStatManager.UpgradeType effect) || !Enum.IsDefined(typeof(UpgradeStatManager.UpgradeType), effect))) ||
                (row.effectValueType != ValueType.Value && row.effectValueType != ValueType.Percent))
                throw new InvalidDataException("Invalid equipment effect: " + row.id);
        }
        foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))
            if (rows.Count(r => r.slot == slot && r.isDefault) != 1) throw new InvalidDataException("Each cosmetic slot needs one free default: " + slot);
    }
}
