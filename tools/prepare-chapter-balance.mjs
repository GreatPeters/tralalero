import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const work='tmp/chapters-balance-20260913';
const require=createRequire(path.resolve(work,'loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const source='Assets/ShooterSurvival/GameData/Editor/Data.xlsx';
const liveBytes=await fs.readFile(source),liveHash=crypto.createHash('sha256').update(liveBytes).digest('hex');
let baseline=`${work}/before.xlsx`;try{await fs.access(baseline);}catch{baseline=source;}
const bytes=await fs.readFile(baseline),hash=crypto.createHash('sha256').update(bytes).digest('hex');
const snapshot=JSON.parse(await fs.readFile('map-concepts/chapters-polish-2026-09-12/balance-source.json','utf8'));
if(hash!==snapshot.sourceSha256)throw new Error('Workbook changed since balance review; inspect the new version first.');
let known=[hash];try{const prior=JSON.parse(await fs.readFile('outputs/chapters-polish-2026-09-12/balance/verification.json','utf8'));known.push(prior.candidateSha256,...(prior.knownCandidateHashes??[]));}catch{}
if(!known.includes(liveHash))throw new Error('Unreviewed concurrent workbook changes');
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(baseline));
async function render(sheet,range,file){const blob=await wb.render({sheetName:sheet,range,scale:1.25,format:'png'});await fs.writeFile(`${work}/${file}.png`,new Uint8Array(await blob.arrayBuffer()));}
if(process.argv.includes('--preview')){
 await render('밸런스 조정','B53:D69','before-progression');
 await render('업그레이드','B2:J10','before-upgrades');
 console.log('Source previews saved');process.exit(0);
}
try{await fs.writeFile(`${work}/before.xlsx`,bytes,{flag:'wx'});}catch(error){if(error.code!=='EEXIST'||!(await fs.readFile(`${work}/before.xlsx`)).equals(bytes))throw error;}
const plan=JSON.parse(await fs.readFile('map-concepts/chapters-polish-2026-09-12/reststop-layout.json','utf8'));
const controls=wb.worksheets.getItem('밸런스 조정');
for(const [cell,value] of Object.entries({C30:220,C31:540,C32:.035,C33:.075,C38:180,C39:240,C40:280,C41:240,C42:1.8,C55:15,C56:20,C69:3.1}))controls.getRange(cell).values=[[value]];
controls.getRange('D56').values=[['챕터별 목표, 실제 도전 횟수는 조작과 구매에 따라 달라짐']];
controls.getRange('B71:D71').copyFrom(controls.getRange('B53:D53'),'all');
controls.getRange('B71:D71').values=[['휴게소 전투와 성장','값','설명']];
controls.getRange('B71:D71').format={fill:'#DBE5EF',font:{name:'Malgun Gothic',size:11,bold:true}};
const restControls=[
 ['첫 일반 공격력',650,'고속도로 후반에서 이어지는 투사체 피해'],['첫 일반 체력',2750,'고속도로 후반 일반 적 체력에 맞춘 진입 기준'],
 ['공격력 증가율',.035,'구간 순서마다 증가'],['체력 증가율',.06,'구간 순서마다 증가'],
 ['엘리트 공격력 배수',1.25,'일반 성장값에 적용'],['엘리트 체력 배수',1.5,'일반 성장값에 적용'],
 ['보스 공격력 배수',1.8,'마지막 구간에 적용'],['보스 체력 배수',2.6,'마지막 구간에 적용'],
 ['정비 바리케이드 피해',500,'충돌 시 피해'],['출차 차량 피해',650,'예고 후 차량 횡단'],
 ['주차 차단기 피해',750,'닫힌 통로에 부딪힌 피해'],['바리케이드 내구도',1250,'사격으로 파괴 가능'],
 ['출차 예고 시간',2,'초'],['차량 횡단 시간',5.5,'초'],['차단기 전환 주기',7,'초'],
 ['차량 이동 폭',9,'월드 유닛'],['휴게소 전진 속도',7,'월드 유닛/초'],['휴게소 완주 목표',300,'초'],
 ['휴게소 일반 적 코인',300,'완주 반복 검증으로 조정, 엘리트2배/보스5배'],['고속도로 일반 적 코인',150,'2챕터 강화 비용 반영, 엘리트2배/보스5배'],
 ['공격력 최대 레벨',null,'강화 표에서 계산'],['체력 최대 레벨',null,'강화 표에서 계산'],
 ['회복 후반 성장 계수',.02,'레벨 × (레벨−1), 초당 회복량']
];
controls.getRange('B72:D94').values=restControls.map(row=>[row[0],row[1],' '+row[2]]);
controls.getRange('B72:D94').format.font={name:'Malgun Gothic',size:11};
controls.getRange('B72:D94').format.rowHeight=32;controls.getRange('D72:D94').format.wrapText=true;
controls.getRange('C72:C91').format.fill='#FFF6DC';controls.getRange('C94').format.fill='#FFF6DC';
controls.getRange('C74:C75').setNumberFormat('0.0%');controls.getRange('C94').setNumberFormat('0.00');
controls.getRange('B95:D99').values=[['진행 보상 간격',15,' 완료한 구간마다 계산(초)'],['고속도로 진행 코인',150,' 한 구간을 진행한 기본 보상'],['휴게소 진행 코인',300,' 한 구간을 진행한 기본 보상'],['진행 보상 최대 구간',20,' 300초 완주 기준'],['노량진 진행 코인',0,' 기존 처치 보상 유지']];
controls.getRange('B95:D99').format.font={name:'Malgun Gothic',size:11};controls.getRange('B95:D99').format.rowHeight=32;controls.getRange('D95:D99').format.wrapText=true;controls.getRange('C95:C99').format.fill='#FFF6DC';
controls.getRange('B100:D105').values=[['노량진 첫 완주 보석',20,' 스킨 구매를 위한 첫 완주 보상'],['노량진 재완주 보석',5,' 같은 챕터를 다시 완주한 보상'],['고속도로 첫 완주 보석',25,' 챕터별 첫 지급 여부를 저장'],['고속도로 재완주 보석',5,' 한 번의 완주에 한 번 지급'],['휴게소 첫 완주 보석',30,' 광고 없이 스킨 구매 가능'],['휴게소 재완주 보석',5,' 반복 완주의 추가 보상']];
controls.getRange('B100:D105').format.font={name:'Malgun Gothic',size:11};controls.getRange('B100:D105').format.rowHeight=32;controls.getRange('D100:D105').format.wrapText=true;controls.getRange('C100:C105').format.fill='#FFF6DC';
controls.getRange('B106:D107').values=[['고속도로 코인 수집 범위',3.2,' 넓은 차선에서 회피하며 처치 보상 수집'],['휴게소 코인 수집 범위',3.2,' 월드 유닛, 높이 차가 큰 층간 수집 제외']];controls.getRange('B106:D107').format.font={name:'Malgun Gothic',size:11};controls.getRange('B106:D107').format.rowHeight=32;controls.getRange('D106:D107').format.wrapText=true;controls.getRange('C106:C107').format.fill='#FFF6DC';
controls.getRange('J71:Q71').values=[['챕터별 성장 목표',null,null,null,null,null,null,null]];
controls.getRange('J72:Q72').values=[['챕터','이름','도전 목표','누적 도전','체력 레벨','공격 레벨','일반 코인','완주 초']];
controls.getRange('J72:Q72').format={fill:'#DBE5EF',font:{name:'Malgun Gothic',size:11,bold:true}};
controls.getRange('J73:Q77').values=[
 [1,'노량진',20,null,12,10,10,300],[2,'고속도로',20,null,24,20,150,300],
 [3,'휴게소',20,null,36,30,300,300],[4,'챕터4',20,null,48,40,700,300],[5,'챕터5',20,null,60,50,1200,300]
];
controls.getRange('P74:P75').formulas=[['=C91'],['=C90']];
controls.getRange('M73').formulas=[['=L73']];controls.getRange('M74:M77').formulas=[['=M73+L74'],['=M74+L75'],['=M75+L76'],['=M76+L77']];
controls.getRange('J71:Q79').format.font.name='Malgun Gothic';controls.getRange('J71:Q79').format.rowHeight=30;
controls.getRange('J71:Q79').format.columnWidth=14;
controls.getRange('J79').values=[['4·5챕터 수치는 후속 씬의 설계 기준입니다.']];
controls.getRange('R72:R77').values=[['첫 완주 보석'],[null],[null],[null],[35],[40]];
controls.getRange('R73:R75').formulas=[['=C100'],['=C102'],['=C104']];controls.getRange('R72:R77').format.columnWidth=18;controls.getRange('R72:R77').format.font={name:'Malgun Gothic',size:11};controls.getRange('R72').format={fill:'#DBE5EF',font:{name:'Malgun Gothic',size:11,bold:true}};
const upgrades=wb.worksheets.getItem('업그레이드');
const old=upgrades.getRange('B3:J242').values;
const first=new Map();for(const row of old)if(!first.has(row[0]))first.set(row[0],row);
const values=[],effects=[],prices=[];
for(let id=1;id<=9;id++){
 const base=first.get(id),cap=id<=2?60:id<=6?30:20,control=id+2;
 for(let level=1;level<=cap;level++){
  const row=3+values.length;values.push([id,base[1],level,base[3],null,base[5],base[6],null,base[8]]);
  let effect=`='밸런스 조정'!$C$${control}+'밸런스 조정'!$D$${control}*D${row}`;
  if(id===2)effect+=`+'밸런스 조정'!$C$66*D${row}*(D${row}-1)`;
  if(id===7)effect+=`+'밸런스 조정'!$C$94*D${row}*(D${row}-1)`;
  effects.push([effect]);
  prices.push([`=ROUND('밸런스 조정'!$E$${control}+'밸런스 조정'!$F$${control}*(D${row}-1)${id<=2?`+'밸런스 조정'!$C$69*MAX(0,D${row}-3)^2`:''},0)`]);
 }
}
for(let row=243;row<=302;row++)upgrades.getRange(`B${row}:J${row}`).copyFrom(upgrades.getRange('B3:J3'),'all');
upgrades.getRange('B3:J302').values=values;upgrades.getRange('F3:F302').formulas=effects;upgrades.getRange('I3:I302').formulas=prices;
controls.getRange('C92').formulas=[["=MAX('업그레이드'!D3:D62)"]];controls.getRange('C93').formulas=[["=MAX('업그레이드'!D63:D122)"]];
const environment=wb.worksheets.getItem('환경 변수');
environment.getRange('B12:F12').copyFrom(environment.getRange('B11:F11'),'all');
environment.getRange('B12:F12').values=[['playerSpeed_RestStop','float',null,null,null]];environment.getRange('D12').formulas=[["='밸런스 조정'!C88"]];
environment.getRange('B2:B12').format.columnWidth=50;
environment.getRange('B13:F17').values=[['progressRewardInterval','float',null,null,null],['progressCoinPerCheckpoint_HighWay','float',null,null,null],['progressCoinPerCheckpoint_RestStop','float',null,null,null],['progressRewardMaximum','float',null,null,null],['progressCoinPerCheckpoint_Noryangjin_MapTool_Mode_SR18','float',null,null,null]];
environment.getRange('D13:D17').formulas=[["='밸런스 조정'!C95"],["='밸런스 조정'!C96"],["='밸런스 조정'!C97"],["='밸런스 조정'!C98"],["='밸런스 조정'!C99"]];
environment.getRange('B2:B17').format.columnWidth=60;
environment.getRange('B18:F23').values=[['firstClearJewels_Noryangjin_MapTool_Mode_SR18','float',null,null,null],['replayClearJewels_Noryangjin_MapTool_Mode_SR18','float',null,null,null],['firstClearJewels_HighWay','float',null,null,null],['replayClearJewels_HighWay','float',null,null,null],['firstClearJewels_RestStop','float',null,null,null],['replayClearJewels_RestStop','float',null,null,null]];
environment.getRange('D18:D23').formulas=Array.from({length:6},(_,i)=>["='밸런스 조정'!C"+(100+i)]);environment.getRange('B2:B23').format.columnWidth=64;
environment.getRange('B24:F25').values=[['coinPickupRadius_HighWay','float',null,null,null],['coinPickupRadius_RestStop','float',null,null,null]];environment.getRange('D24:D25').formulas=[["='밸런스 조정'!C106"],["='밸런스 조정'!C107"]];
function nextRow(sheet,lastColumn){const values=sheet.getRange(`B1:${lastColumn}500`).values;return values.reduce((last,row,index)=>row[2]?index+1:last,2)+1;}
const enemy=wb.worksheets.getItem('적 배치');
const enemyBefore=enemy.getRange('B3:R102').values;
for(let i=0;i<enemyBefore.length;i++)if(enemyBefore[i][1]==='HighWay'){
 enemy.getRange(`R${i+3}`).formulas=[[`='밸런스 조정'!$C$91*IF(M${i+3}="Boss",5,IF(M${i+3}="Elite",2,1))`]];
 if(enemyBefore[i][4]==='왕복')enemy.getRange(`H${i+3}`).values=[[1.2]];
}
const enemyStart=nextRow(enemy,'R');
for(let i=0;i<plan.enemies.length;i++){
 const item=plan.enemies[i],r=enemyStart+i;enemy.getRange(`B${r}:R${r}`).copyFrom(enemy.getRange('B53:R53'),'all');
 enemy.getRange(`B${r}:R${r}`).values=[[101+i,'RestStop',item.id,1,item.mode,item.speed,item.move,item.lead,item.delay,item.projectileSpeed,'안전 통로가 좌우로 번갈아 바뀌는 배치',item.tier,null,null,null,null,null]];
 enemy.getRange(`N${r}:R${r}`).formulas=[[
  `=ROUND('밸런스 조정'!$C$72*(1+'밸런스 조정'!$C$74)^${item.station-1}*IF(M${r}="Boss",'밸런스 조정'!$C$78,IF(M${r}="Elite",'밸런스 조정'!$C$76,1)),0)`,
  `=ROUND('밸런스 조정'!$C$73*(1+'밸런스 조정'!$C$75)^${item.station-1}*IF(M${r}="Boss",'밸런스 조정'!$C$79,IF(M${r}="Elite",'밸런스 조정'!$C$77,1)),0)`,
  "='밸런스 조정'!$C$75","='밸런스 조정'!$C$64",`='밸런스 조정'!$C$90*IF(M${r}="Boss",5,IF(M${r}="Elite",2,1))`
 ]];
}
const bonus=wb.worksheets.getItem('보너스 배치'),bonusStart=nextRow(bonus,'G');
for(let i=0;i<plan.bonuses.length;i++){const item=plan.bonuses[i],r=bonusStart+i;bonus.getRange(`B${r}:G${r}`).copyFrom(bonus.getRange('B3:G3'),'all');bonus.getRange(`B${r}:G${r}`).values=[[51+i,'RestStop',item.id,1,item.rarity,'좌우 중 하나 선택']];}
const gimmick=wb.worksheets.getItem('기믹 배치'),gimmickStart=nextRow(gimmick,'L');
for(let i=0;i<plan.gimmicks.length;i++){
 const item=plan.gimmicks[i],r=gimmickStart+i;gimmick.getRange(`B${r}:L${r}`).copyFrom(gimmick.getRange('B28:L28'),'all');
 gimmick.getRange(`B${r}:L${r}`).values=[[51+i,'RestStop',item.id,1,item.pattern,null,item.theme,0,0,0,0]];
 const roadblock=item.pattern==='HighwayRoadblock',traffic=item.pattern==='HighwayTraffic';
 gimmick.getRange(`G${r}`).formulas=[[`='밸런스 조정'!$C$${roadblock?80:traffic?81:82}`]];
 if(roadblock)gimmick.getRange(`I${r}`).formulas=[["='밸런스 조정'!$C$83"]];
 if(traffic)gimmick.getRange(`J${r}:L${r}`).formulas=[["='밸런스 조정'!$C$84","='밸런스 조정'!$C$85","='밸런스 조정'!$C$87"]];
 if(!roadblock&&!traffic)gimmick.getRange(`K${r}`).formulas=[["='밸런스 조정'!$C$86"]];
}
const scenarioChecks=[];
controls.getRange('C72').values=[[520]];
if(enemy.getRange(`N${enemyStart}`).values[0][0]!==520)throw new Error('RestStop attack input is not linked');
controls.getRange('C72').values=[[650]];scenarioChecks.push('RestStop attack input updates the first encounter');
controls.getRange('C91').values=[[45]];
if(enemy.getRange('R53').values[0][0]!==45||enemy.getRange(`R${enemyStart}`).values[0][0]!==300)throw new Error('Chapter coin rewards are coupled');
controls.getRange('C91').values=[[150]];scenarioChecks.push('Highway reward changes independently of RestStop');
controls.getRange('C69').values=[[4]];
if(upgrades.getRange('I3').values[0][0]!==10||upgrades.getRange('I62').values[0][0]!==13301)throw new Error('Late price curve changed the first purchase or used a wrong level');
controls.getRange('C69').values=[[3.1]];scenarioChecks.push('Late upgrade pricing preserves first price and computes level60');
controls.getRange('C100').values=[[21]];if(environment.getRange('D18').values[0][0]!==21||controls.getRange('R73').values[0][0]!==21)throw new Error('First-clear jewel input is not linked');controls.getRange('C100').values=[[20]];scenarioChecks.push('Chapter jewel rewards link to runtime environment and campaign plan');
const checks={sourceHash:hash,upgrades:values.length,restStopRows:100,scenarioChecks,chapterTargets:controls.getRange('L73:M77').values,
 firstHighway:enemy.getRange('N53:O53').values,lastHighway:enemy.getRange('N101:O101').values,
 firstRestStop:enemy.getRange(`N${enemyStart}:O${enemyStart}`).values,lastRestStop:enemy.getRange(`N${enemyStart+48}:O${enemyStart+48}`).values,
 maximumLevels:controls.getRange('C92:C93').values};
if(checks.maximumLevels.some(row=>row[0]!==60)||checks.chapterTargets[4][1]!==100)throw new Error('Progression formulas disagree with target');
for(const name of ['업그레이드','밸런스 조정','적 배치','보너스 배치','기믹 배치','환경 변수']){
 const errors=await wb.inspect({kind:'match',sheetId:name,searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:30},maxChars:1200});
 await fs.writeFile(`${work}/${name}-errors.ndjson`,errors.ndjson);
}
await render('밸런스 조정','B71:D107','reststop-controls');await render('밸런스 조정','J71:R79','campaign-targets');
await render('업그레이드','B57:J66','upgrade-boundary');await render('적 배치',`B${enemyStart}:R${enemyStart+3}`,'reststop-enemies');
await render('보너스 배치',`B${bonusStart}:G${bonusStart+4}`,'reststop-bonuses');await render('기믹 배치',`B${gimmickStart}:L${gimmickStart+4}`,'reststop-gimmicks');
await render('환경 변수','B9:F17','chapter-speeds');
await render('환경 변수','B18:F23','chapter-jewels');
await fs.writeFile(`${work}/changes.json`,JSON.stringify({beforeHash:hash,sheets:['업그레이드','환경 변수','밸런스 조정','적 배치','보너스 배치','기믹 배치'],checks},null,2));
await(await SpreadsheetFile.exportXlsx(wb)).save(`${work}/artifact-candidate.xlsx`);
console.log(JSON.stringify(checks));
