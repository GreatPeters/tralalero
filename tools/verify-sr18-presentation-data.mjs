import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const require=createRequire(path.resolve('tmp/sr18-presentation-data-20260910/loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const label=process.argv[2]||'v6';
const config=JSON.parse(await fs.readFile(`tmp/sr18-progression-data-20260910/${label}/progression-changes.json`,'utf8'));
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load('Assets/ShooterSurvival/GameData/Editor/Data.xlsx'));
const items=wb.worksheets.getItem('커스터마이징').getRange('B3:L26').values;
if(items.length!==24||new Set(items.map(r=>r[0])).size!==24)throw new Error('Equipment IDs/count mismatch');
for(const slot of ['Skin','Shoes','Hat'])if(items.filter(r=>r[1]===slot).length!==8)throw new Error('Equipment slot count mismatch');
if(items.some(r=>r[3]!=='Jewel'||r[4]<0||(!r[6]&&!(r[9]>0))))throw new Error('Equipment currency/effect mismatch');
const controls=wb.worksheets.getItem('밸런스 조정').getRange('B3:H11').values;
if(JSON.stringify(controls)!==JSON.stringify(config.tuning))throw new Error('Saved controls differ from reviewed tuning');
const environment=wb.worksheets.getItem('환경 변수').getRange('D4:D5').values;
if(environment[0][0]!==config.input.attack||environment[1][0]!==config.input.hp)throw new Error('Player baseline mismatch');
const upgradeRows=wb.worksheets.getItem('업그레이드').getRange('A1:Q1200').values;
let checkedUpgrades=0;
for(const row of upgradeRows){
 const control=config.tuning.find(c=>c[0]===row[2]);if(!control||typeof row[3]!=='number')continue;
 const amount=control[1]+row[3]*control[2],price=control[3]+(row[3]-1)*control[4];
 if(Math.abs(row[5]-amount)>1e-6||Math.abs(row[8]-price)>1e-6)throw new Error(`Upgrade formula mismatch: ${row[2]} level ${row[3]}`);
 checkedUpgrades++;
}
if(checkedUpgrades<200)throw new Error('Upgrade rows missing');
const enemies=wb.worksheets.getItem('적 배치').getRange('A3:Q40').values.filter(r=>typeof r[3]==='string'&&r[3].startsWith('SR18_L_E'));
if(enemies.length!==27)throw new Error('Enemy count changed');
for(const r of enemies){
 if(!(r[13]>=0&&r[14]>0))throw new Error('Invalid enemy stats');
 if(r[3].endsWith('_Right')){const left=enemies.find(l=>l[3]===r[3].slice(0,-6));if(!left||left[13]!==r[13]||left[14]!==r[14])throw new Error('Pair stats diverged');}
}
for(const name of ['업그레이드','적 배치','환경 변수','밸런스 조정','커스터마이징']){
 const values=wb.worksheets.getItem(name).getRange('A1:Q1200').values;
 if(values.flat().some(v=>typeof v==='string'&&/^#(REF!|DIV\/0!|VALUE!|NAME\?|N\/A|NUM!)/.test(v)))throw new Error('Formula error in '+name);
}
const output='tmp/image-previews/sr18-presentation-progression-2026-09-10';
for(const [sheet,range,file] of [['밸런스 조정','B1:H25','balance-reviewed.png'],['커스터마이징','B1:L10','equipment-data-reviewed.png']]){
 const image=await wb.render({sheetName:sheet,range,scale:1.15,format:'png'});await fs.writeFile(`${output}/${file}`,new Uint8Array(await image.arrayBuffer()));
}
const report={label,passed:true,equipment:items.length,enemies:enemies.length,upgradeRows:checkedUpgrades,formulaErrors:0,baseline:config.input,tuning:controls};
await fs.writeFile('map-concepts/sr18-presentation-progression-2026-09-10/data-verification.json',JSON.stringify(report,null,2));
console.log(JSON.stringify({...report,tuning:undefined}));
