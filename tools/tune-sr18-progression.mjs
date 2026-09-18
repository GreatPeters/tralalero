import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const require=createRequire(path.resolve('tmp/sr18-presentation-data-20260910/loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const value=(key,fallback)=>{const i=process.argv.indexOf('--'+key);return i<0?fallback:Number(process.argv[i+1]);};
const label=process.argv[2]||'v1';
const work=`tmp/sr18-progression-data-20260910/${label}`;
await fs.mkdir(work,{recursive:true});
const source='Assets/ShooterSurvival/GameData/Editor/Data.xlsx';
const bytes=await fs.readFile(source);const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const controls=wb.worksheets.getItem('밸런스 조정');
const tuning=[
 ['ATT',value('att-base',0),value('att-step',3),value('att-price',20),value('att-growth',12),'value','Coin'],
 ['HP',value('hp-base',0),value('hp-step',10),value('hp-price',20),value('hp-growth',10),'value','Coin'],
 ['ATT_SPEED',0,5,45,20,'percent','Coin'],['PROJECTILE_SPEED',0,4,40,16,'percent','Coin'],
 ['BOSS_DAMAGE',0,8,60,25,'percent','Coin'],['COIN_BONUS',0,8,70,35,'percent','Coin'],
 ['HP_REGEN',0,.1,80,35,'value','Coin'],['TUNGTUNGTUNG',95,5,450,120,'percent','Coin'],
 ['BOOMBAR',125,5,550,150,'percent','Coin']];
const input={attack:value('attack',14),hp:value('hp',50),enemyAttackGrowth:value('enemy-attack-growth',.08),enemyHealthGrowth:value('enemy-health-growth',.12)};
await fs.writeFile(`${work}/progression-before.xlsx`,bytes,{flag:'wx'});
controls.getRange('B3:H11').values=tuning;
controls.getRange('C17:C18').values=[[input.enemyAttackGrowth],[input.enemyHealthGrowth]];
if(process.argv.includes('--opening-walls')){
 controls.getRange('C16').values=[[45]];
 controls.getRange('B23:C24').values=[['두 번째 일반 적 체력',75],['첫 성장 벽 체력',value('opening-wall',150)]];
 controls.getRange('C23:C24').format.fill='#FFF6DC';
 wb.worksheets.getItem('적 배치').getRange('O4:O5').formulas=[["='밸런스 조정'!$C$23"],["='밸런스 조정'!$C$24"]];
}
controls.getRange('B25').values=[['약 20회 도전 목표. 첫 도전 약 30초, 실제 획득 코인과 업그레이드 구매로 진행을 검증.']];
wb.worksheets.getItem('환경 변수').getRange('D4:D5').values=[[input.attack],[input.hp]];
// Artifact Tool recalculates dependents when the control inputs change. Keep
// existing formula addresses untouched; used ranges do not necessarily start A1.
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:20},maxChars:1200});console.log(errors.ndjson);
const render=await wb.render({sheetName:'밸런스 조정',range:'B1:H25',scale:1.1,format:'png'});await fs.writeFile(`${work}/balance.png`,new Uint8Array(await render.arrayBuffer()));
await fs.writeFile(`${work}/progression-changes.json`,JSON.stringify({beforeHash:crypto.createHash('sha256').update(bytes).digest('hex'),sheets:['밸런스 조정','환경 변수','업그레이드','적 배치'],input,tuning},null,2));
await(await SpreadsheetFile.exportXlsx(wb)).save(`${work}/progression-candidate.xlsx`);
console.log(JSON.stringify({work,input,firstUpgrade:tuning.slice(0,2)}));
