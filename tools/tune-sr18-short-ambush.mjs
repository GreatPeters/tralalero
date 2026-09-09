import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const req=createRequire(path.resolve('tmp/sr18-data-work-2026-09-07/loader.cjs'));
const {SpreadsheetFile}=await import(pathToFileURL(req.resolve('@oai/artifact-tool')).href);
const work='tmp/sr18-polish-2026-09-09';
const bytes=await fs.readFile('Assets/ShooterSurvival/GameData/Editor/Data.xlsx');
await fs.writeFile(`${work}/Data.revision-before.xlsx`,bytes,{flag:'wx'});
const wb=await SpreadsheetFile.importXlsx(new Uint8Array(bytes));
const s=wb.worksheets.getItem('적 배치');
for(const r of [7,26]) {
 if(s.getRange(`H${r}`).values[0][0]!==8) throw Error('Unexpected ambush distance');
 s.getRange(`H${r}`).values=[[4]];
}
for(const r of [3,10,14,26,27]) s.getRange(`P${r}`).clear({applyTo:'contents'});
await(await SpreadsheetFile.exportXlsx(wb)).save(`${work}/artifact-candidate.xlsx`);
console.log('Two short sections: walk 4 units; other ambushers: 8. Cleared non-applicable growth inputs.');
