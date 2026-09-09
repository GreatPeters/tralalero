import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const req=createRequire(path.resolve('tmp/sr18-data-work-2026-09-07/loader.cjs'));
const {SpreadsheetFile}=await import(pathToFileURL(req.resolve('@oai/artifact-tool')).href);
const wb=await SpreadsheetFile.importXlsx(new Uint8Array(await fs.readFile('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')));
const work='tmp/sr18-polish-2026-09-09';
for(const [name,range,file] of [['적 배치','L2:P12','verified-combat.png'],['적 배치','E2:K8','verified-ambush.png'],['기믹 배치','F2:H8','verified-buckets.png']]) {
 const png=await wb.render({sheetName:name,range,scale:1.2,format:'png'});
 await fs.writeFile(`${work}/${file}`,new Uint8Array(await png.arrayBuffer()));
}
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:10},maxChars:1000})).ndjson);
console.log('Final stats',JSON.stringify(wb.worksheets.getItem('적 배치').getRange('M25:P27').values));
