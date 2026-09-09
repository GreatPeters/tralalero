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
                string Text(string name) => Convert.ToString(reader.GetValue(headers[name]), System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? "";
                string id = Text("ID"); if (id.Length == 0) continue;
                rows.Add(new CosmeticItem { id = id, slot = (CosmeticSlot)Enum.Parse(typeof(CosmeticSlot), Text("부위")),
                    name = Text("이름"), currency = (PriceType)Enum.Parse(typeof(PriceType), Text("가격타입")),
                    price = int.Parse(Text("가격")), visualKey = Text("비주얼"), isDefault = Text("기본") == "1", description = Text("설명") });
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
        foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))
            if (rows.Count(r => r.slot == slot && r.isDefault) != 1) throw new InvalidDataException("Each cosmetic slot needs one free default: " + slot);
    }
}
