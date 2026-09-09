import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';

const work = 'tmp/sr18-contact-pairs-20260910';
const require = createRequire(path.resolve(work, 'loader.cjs'));
const {SpreadsheetFile, FileBlob} = await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const source = 'Assets/ShooterSurvival/GameData/Editor/Data.xlsx';
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const sheet = workbook.worksheets.getItem('적 배치');
const baseline = sheet.getRange('B2:P27').values;
if (process.argv.includes('--verify')) {
  const preview = await workbook.render({sheetName:'적 배치',range:'B26:P29',scale:1,format:'png'});
  await fs.writeFile(`${work}/preserved-after.png`, new Uint8Array(await preview.arrayBuffer()));
  console.log(JSON.stringify(sheet.getRange('D28:O29').values));
  console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:20},maxChars:1200})).ndjson);
  process.exit(0);
}
await fs.writeFile(`${work}/before-rows.json`, JSON.stringify(baseline, null, 2));
if (!process.argv.includes('--apply')) {
  const preview = await workbook.render({sheetName:'적 배치',range:'B2:P9',scale:1,format:'png'});
  await fs.writeFile(`${work}/before.png`, new Uint8Array(await preview.arrayBuffer()));
  console.log(JSON.stringify(baseline.slice(0,7)));
  process.exit(0);
}
if (sheet.getRange('D28:D29').values.flat().some(Boolean)) throw Error('Companion rows already populated');
const mappings = [];
for (const [index, prefix] of ['SR18_L_E02_', 'SR18_L_E06_'].entries()) {
  const sourceIndex = baseline.findIndex(row => String(row[2]).startsWith(prefix));
  if (sourceIndex < 1) throw Error(`Missing source ${prefix}`);
  const sourceRow = sourceIndex + 2;
  const destination = index + 28;
  sheet.getRange(`B${destination}:P${destination}`).copyFrom(sheet.getRange(`B${sourceRow}:P${sourceRow}`), 'all');
  sheet.getRange(`B${destination}`).values = [[26 + index]];
  const id = String(baseline[sourceIndex][2]) + '_Right';
  sheet.getRange(`D${destination}`).values = [[id]];
  sheet.getRange(`L${destination}`).values = [['좌우 일반 적 군집의 오른쪽 적. 공격력과 체력은 왼쪽 적 수치 참조.']];
  sheet.getRange(`M${destination}`).values = [['Normal']];
  sheet.getRange(`N${destination}:O${destination}`).formulas = [[`=N${sourceRow}`, `=O${sourceRow}`]];
  mappings.push({id,sourceRow,destination});
}
const errors = await workbook.inspect({kind:'match', searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!', options:{useRegex:true,maxResults:20},maxChars:1800});
console.log(errors.ndjson);
console.log(JSON.stringify(sheet.getRange('M28:O29').values));
await fs.writeFile(`${work}/row-mappings.json`, JSON.stringify(mappings,null,2));
const preview = await workbook.render({sheetName:'적 배치',range:'B26:P29',scale:1,format:'png'});
await fs.writeFile(`${work}/after.png`,new Uint8Array(await preview.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(`${work}/candidate.xlsx`);
