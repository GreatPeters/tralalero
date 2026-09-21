import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const root = process.cwd();
const source = path.join(root, "Assets/ShooterSurvival/GameData/Editor/Data.xlsx");
const folder = path.join(root, "map-concepts/mobile-presentation-2026-09-14/workbook");
const book = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const sheet = book.worksheets.getItem("업그레이드");
const rows = sheet.getRange("B2:J302").values;
const utility = rows.map((value, index) => ({ value, row: index + 2 }))
  .filter(x => Number(x.value[0]) === 6).sort((a, b) => Number(a.value[2]) - Number(b.value[2]));
console.log(JSON.stringify({ rowCount: rows.length, firstRows: rows.slice(0,4), utility: utility.slice(0, 10), tables: sheet.tables.items.map(t => t.name) }));
console.log((await book.inspect({kind:"region",sheetId:"업그레이드",range:"B2:J6",maxChars:2000})).ndjson);
if (!process.argv.includes("--write")) process.exit(0);
if (rows.some(r => Number(r[0]) === 10)) throw new Error("Preserve an existing upgrade id 10.");
if (utility.length < 10) throw new Error("Expected an established ten-level utility price reference.");

for (let level = 1; level <= 10; level++) {
  const reference = utility[level - 1];
  if (Number(reference.value[2]) !== level || reference.value[6] !== "Coin") throw new Error("Unexpected utility price data.");
  const row = 302 + level;
  sheet.getRange(`B${row}:J${row}`).copyFrom(sheet.getRange(`B${reference.row}:J${reference.row}`), "all");
  sheet.getRange(`B${row}:J${row}`).values = [[
    10, "LATERAL_SPEED", level, "좌우 이동 속도 늘리기", level * 5, "percent",
    "Coin", Number(reference.value[7]), "좌우 이동 속도 단계마다 +5%, 최대 +50%"
  ]];
}
sheet.getRange("E303:E312").format.wrapText = true;
sheet.getRange("J303:J312").format.wrapText = true;
sheet.getRange("B303:J312").format.rowHeight = 32;
await fs.mkdir(folder, { recursive: true });
console.log((await book.inspect({kind:"table", range:"업그레이드!B303:J312", include:"values,formulas", tableMaxRows:10, tableMaxCols:9, maxChars:4500})).ndjson);
const output = await SpreadsheetFile.exportXlsx(book);
await output.save(path.join(folder, "Data.xlsx"));
const preview = await book.render({sheetName:"업그레이드",range:"B303:J312",scale:1.5});
await fs.writeFile(path.join(folder, "lateral-upgrade-v2.png"), new Uint8Array(await preview.arrayBuffer()));
