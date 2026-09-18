import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const noryMode=process.argv.includes('--nory-refine');
const work=noryMode?'tmp/highway-data-20260911/nory-refine':'tmp/highway-data-20260911';
const prefix=noryMode?'nory':'highway';
await fs.mkdir(work,{recursive:true});
const require=createRequire(path.resolve('tmp/highway-data-20260911/loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const source='Assets/ShooterSurvival/GameData/Editor/Data.xlsx';
const bytes=await fs.readFile(source);
await fs.writeFile(`${work}/${prefix}-before.xlsx`,bytes,{flag:'wx'});
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(source));
if(noryMode){
 const changes=JSON.parse(await fs.readFile('map-concepts/noryangjin-refinement-2026-09-11/changes.json','utf8'));
 const sheet=wb.worksheets.getItem('기믹 배치'),controls=wb.worksheets.getItem('밸런스 조정');
 const values=sheet.getRange('B1:L200').values;
 controls.getRange('B47').values=[['노량진 추가 기믹']];controls.getRange('B47:D47').format.font.bold=true;
 controls.getRange('B48:D50').values=[['기름 웅덩이 지속 시간',2,'조작 감도 55%, 중첩 없음'],['갈매기 급강하 피해',20,'그림자를 보고 회피'],['해안 배 포격 피해',10,'물가에서 진행로를 조준']];
 controls.getRange('C48:C50').format.fill='#FFF6DC';controls.getRange('B48:D50').format.rowHeight=32;controls.getRange('D48:D50').format.wrapText=true;
 for(const change of changes.replacements){
  const row=values.findIndex(v=>v[2]===change.previous)+1;if(row<3)throw new Error('Missing station '+change.previous);
  sheet.getRange(`D${row}`).values=[[change.id]];sheet.getRange(`F${row}`).values=[[change.pattern]];
  sheet.getRange(`G${row}`).formulas=[[`='밸런스 조정'!$C$${change.pattern==='Oil'?48:49}`]];
  sheet.getRange(`H${row}`).values=[[change.pattern==='Oil'?'잠시 좌우 조작 감소. 진행 방향과 사격 유지.':'그림자 예고 후 갈매기 급강하']];
 }
 let last=values.reduce((end,row,i)=>row[2]?i+1:end,2);
 let count=values.filter(v=>v[1]==='Noryangjin_MapTool_Mode_SR18').length;
 for(const change of changes.additions){
  const row=++last;sheet.getRange(`B${row}:L${row}`).copyFrom(sheet.getRange('B26:L26'),'all');
  sheet.getRange(`B${row}:L${row}`).values=[[++count,'Noryangjin_MapTool_Mode_SR18',change.id,1,change.pattern,null,'해안에서 진행로를 향한 포격',null,null,null,null]];
  sheet.getRange(`G${row}`).formulas=[["='밸런스 조정'!$C$50"]];
 }
 const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:20},maxChars:1000});console.log(errors.ndjson);
 const image=await wb.render({sheetName:'밸런스 조정',range:'B47:D50',scale:1.3,format:'png'});await fs.writeFile(`${work}/nory-balance.png`,new Uint8Array(await image.arrayBuffer()));
 await fs.writeFile(`${work}/nory-changes.json`,JSON.stringify({beforeHash:crypto.createHash('sha256').update(bytes).digest('hex'),sheets:['밸런스 조정','기믹 배치']},null,2));
 await(await SpreadsheetFile.exportXlsx(wb)).save(`${work}/nory-candidate.xlsx`);
 console.log(JSON.stringify({replacements:changes.replacements.length,added:changes.additions.length}));
 process.exit(0);
}
const plan=JSON.parse(await fs.readFile('map-concepts/highway-chapter-2026-09-11/placements.json','utf8'));
const controls=wb.worksheets.getItem('밸런스 조정');
controls.getRange('B28').values=[['고속도로 전투와 기믹']];
controls.getRange('B28:D45').format.font={name:'Malgun Gothic',size:11};
controls.getRange('B28:D28').format.font.bold=true;
controls.getRange('B29:D29').values=[['항목','값','설명']];
controls.getRange('B29:D29').format={fill:'#DBE5EF',font:{bold:true}};
controls.getRange('B30:D45').values=[
 ['첫 일반 공격력',42,'투사체 기본 피해'],['첫 일반 체력',140,'첫 전투 기준'],
 ['공격력 증가율',.025,'배치 순서마다 증가'],['체력 증가율',.045,'배치 순서마다 증가'],
 ['엘리트 공격력 배수',1.25,'일반 성장값에 적용'],['엘리트 체력 배수',1.5,'일반 성장값에 적용'],
 ['보스 공격력 배수',1.8,'마지막 대장에 적용'],['보스 체력 배수',2.6,'마지막 대장에 적용'],
 ['도로 장애물 피해',25,'충돌 시 피해'],['차량 충돌 피해',35,'횡단 차량의 피해'],['차단기 충돌 피해',40,'닫힌 차단기의 피해'],
 ['장애물 내구도',85,'사격으로 부술 수 있음'],['차량 예고 시간',1.5,'움직이기 전 경고(초)'],
 ['차량 통과 시간',4,'차선 횡단 시간(초)'],['차단기 주기',6,'다음 통로 전환(초)'],['차량 이동 폭',9,'좌우 횡단 거리']
];
controls.getRange('C30:C45').format.fill='#FFF6DC';controls.getRange('C32:C33').setNumberFormat('0.0%');
controls.getRange('B30:D45').format.rowHeight=32;controls.getRange('D30:D45').format.wrapText=true;
const enemy=wb.worksheets.getItem('적 배치');
if(enemy.getRange('C30').values[0][0])throw new Error('Enemy append area is occupied');
for(let i=0;i<plan.enemies.length;i++){
 const e=plan.enemies[i],r=30+i;enemy.getRange(`B${r}:P${r}`).copyFrom(enemy.getRange('B29:P29'),'all');
 enemy.getRange(`B${r}:P${r}`).values=[[i+1,'HighWay',e.id,1,e.mode,e.speed,e.move,e.lead,e.delay,e.projectileSpeed,e.model,e.tier,null,null,null]];
 enemy.getRange(`N${r}:P${r}`).formulas=[[
  `=ROUND('밸런스 조정'!$C$30*(1+'밸런스 조정'!$C$32)^(B${r}-1)*IF(M${r}="Boss",'밸런스 조정'!$C$36,IF(M${r}="Elite",'밸런스 조정'!$C$34,1)),0)`,
  `=ROUND('밸런스 조정'!$C$31*(1+'밸런스 조정'!$C$33)^(B${r}-1)*IF(M${r}="Boss",'밸런스 조정'!$C$37,IF(M${r}="Elite",'밸런스 조정'!$C$35,1)),0)`,
  "='밸런스 조정'!$C$33"
 ]];
}
const bonus=wb.worksheets.getItem('보너스 배치');
if(bonus.getRange('C28').values[0][0])throw new Error('Bonus append area is occupied');
for(let i=0;i<plan.bonuses.length;i++){
 const b=plan.bonuses[i],r=28+i;bonus.getRange(`B${r}:G${r}`).copyFrom(bonus.getRange('B27:G27'),'all');
 bonus.getRange(`B${r}:G${r}`).values=[[i+1,'HighWay',b.id,1,b.rarity,'좌우 중 하나 선택']];
}
const hazards=wb.worksheets.getItem('기믹 배치');
if(hazards.getRange('C27').values[0][0])throw new Error('Gimmick append area is occupied');
hazards.getRange('I2:L2').values=[['내구도','예고초','작동초','이동폭']];
hazards.getRange('I2:L2').format={fill:'#DBE5EF',font:{bold:true}};hazards.getRange('I2:L55').format.columnWidth=13;
for(let i=0;i<plan.gimmicks.length;i++){
 const g=plan.gimmicks[i],r=27+i;hazards.getRange(`B${r}:H${r}`).copyFrom(hazards.getRange('B26:H26'),'all');
 const kind=g.pattern;hazards.getRange(`B${r}:L${r}`).values=[[i+1,'HighWay',g.id,1,kind,null,kind==='HighwayTraffic'?'예고 후 차량 횡단':kind==='HighwayToll'?'초록 신호 통로로 이동':'사격으로 파괴하거나 회피',0,0,0,0]];
 hazards.getRange(`G${r}`).formulas=[[`='밸런스 조정'!$C$${kind==='HighwayRoadblock'?38:kind==='HighwayTraffic'?39:40}`]];
 if(kind==='HighwayRoadblock')hazards.getRange(`I${r}`).formulas=[["='밸런스 조정'!$C$41"]];
 if(kind==='HighwayTraffic'){
  hazards.getRange(`J${r}:L${r}`).formulas=[["='밸런스 조정'!$C$42","='밸런스 조정'!$C$43","='밸런스 조정'!$C$45"]];
 }
 if(kind==='HighwayToll')hazards.getRange(`K${r}`).formulas=[["='밸런스 조정'!$C$44"]];
}
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:30},maxChars:1500});
console.log(errors.ndjson);
await fs.writeFile(`${work}/highway-errors.ndjson`,errors.ndjson);
const render=await wb.render({sheetName:'밸런스 조정',range:'B28:D45',scale:1.2,format:'png'});
await fs.writeFile(`${work}/highway-balance.png`,new Uint8Array(await render.arrayBuffer()));
await fs.writeFile(`${work}/highway-values.json`,JSON.stringify({enemies:enemy.getRange('B30:P52').values,gimmicks:hazards.getRange('B27:L55').values},null,2));
await fs.writeFile(`${work}/highway-changes.json`,JSON.stringify({beforeHash:crypto.createHash('sha256').update(bytes).digest('hex'),sheets:['밸런스 조정','적 배치','보너스 배치','기믹 배치']},null,2));
await(await SpreadsheetFile.exportXlsx(wb)).save(`${work}/highway-candidate.xlsx`);
console.log(JSON.stringify({enemies:plan.enemies.length,bonuses:plan.bonuses.length,gimmicks:plan.gimmicks.length}));
