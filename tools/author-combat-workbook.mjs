import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
const require = createRequire(path.resolve('tmp/highway-data-20260911/loader.cjs'));
const {SpreadsheetFile, FileBlob} = await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const folder = 'map-concepts/combat-feedback-2026-09-14';
const wb = await SpreadsheetFile.importXlsx(await FileBlob.load('Assets/ShooterSurvival/GameData/Editor/Data.xlsx'));
const before = await wb.render({sheetName:'커스터마이징',range:'D2:L10',scale:1.5});
await fs.writeFile(`${folder}/cosmetics-before.png`,new Uint8Array(await before.arrayBuffer()));
const changes = JSON.parse(await fs.readFile(`${folder}/balance-proposal.json`,'utf8'));
if (!process.argv.includes('--apply')) { console.log('Rendered baseline'); process.exit(0); }
for(const change of changes) {
  const cell = wb.worksheets.getItem(change.sheet).getRange(change.cell);
  const formula = cell.formulas[0][0];
  change.formulaBefore = formula || '';
  // Right-hand enemy damage already references the left-hand row.
  if(formula && change.multiplier != null && !/^=?\$?N\$?\d+$/.test(formula))
    cell.formulas = [[`=(${formula.replace(/^=/,'')})*${change.multiplier}`]];
  else if(!formula || change.multiplier == null) cell.values = [[change.after]];
  change.formulaAfter = cell.formulas[0][0] || '';
}
await fs.writeFile(`${folder}/balance-authored.json`,JSON.stringify(changes,null,2));
console.log((await wb.inspect({kind:'table',range:'커스터마이징!J2:L10',include:'values,formulas',tableMaxRows:9,tableMaxCols:3})).ndjson);
const after = await wb.render({sheetName:'커스터마이징',range:'D2:L10',scale:1.5});
await fs.writeFile(`${folder}/cosmetics-after.png`,new Uint8Array(await after.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(wb)).save(`${folder}/artifact-candidate.xlsx`);
