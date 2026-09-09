import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';
const require = createRequire(path.resolve('tmp/sr18-contact-pairs-20260910/loader.cjs'));
const { SpreadsheetFile, FileBlob } = await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const wb = await SpreadsheetFile.importXlsx(await FileBlob.load('Assets/ShooterSurvival/GameData/Editor/Data.xlsx'));
const cosmetics = wb.worksheets.getItem('커스터마이징').getRange('B3:I14').values;
if (cosmetics.length !== 12 || new Set(cosmetics.map(r => r[0])).size !== 12) throw new Error('Invalid cosmetic catalog');
const controls = wb.worksheets.getItem('밸런스 조정').getRange('B3:H11').values;
if (controls[0][2] !== 6 || controls[0][3] !== 50 || controls[1][2] !== 15) throw new Error('Balance controls mismatch');
for (const sheet of ['업그레이드', '적 배치', '밸런스 조정', '커스터마이징']) {
  const values = wb.worksheets.getItem(sheet).getRange('A1:Q1000').values;
  if (values.flat().some(v => typeof v === 'string' && /^#(REF!|DIV\/0!|VALUE!|NAME\?|N\/A|NUM!)/.test(v))) throw new Error('Formula error in ' + sheet);
}
const dir = 'tmp/image-previews/noryangjin-release-2026-09-10';
for (const [sheet, range, name] of [['밸런스 조정','B1:H25','balance-final'],['커스터마이징','B1:I14','cosmetics-data-final']]) {
  const png = await wb.render({ sheetName:sheet, range, scale:1.2, format:'png' });
  await fs.writeFile(`${dir}/${name}.png`,new Uint8Array(await png.arrayBuffer()));
}
console.log(JSON.stringify({ cosmeticItems:cosmetics.length, upgradeTracks:controls.length, formulaErrors:0, savedSourceVerified:true }));
