import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const work='tmp/noryangjin-release-20260910';
const require=createRequire(path.resolve('tmp/sr18-contact-pairs-20260910/loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const source='Assets/ShooterSurvival/GameData/Editor/Data.xlsx';
const bytes=await fs.readFile(source);
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(source));
await fs.mkdir(work,{recursive:true});
if(!process.argv.includes('--apply')){
  for(const [name,range,file] of [['업그레이드','B2:J8','upgrades-before.png'],['적 배치','M2:P12','enemy-growth-before.png']]){
    const image=await wb.render({sheetName:name,range,scale:1.1,format:'png'});
    await fs.writeFile(`${work}/${file}`,new Uint8Array(await image.arrayBuffer()));
  }
  console.log('Before views saved');process.exit(0);
}
await fs.writeFile(`${work}/Data.before.xlsx`,bytes,{flag:'wx'});
const changed={};const styleCells={};const newSheets=['밸런스 조정','커스터마이징'];
function set(sheet,cell,value,formula=false,style=false){
  const sh=wb.worksheets.getItem(sheet);sh.getRange(cell)[formula?'formulas':'values']=[[value]];
  (changed[sheet]??=[]).push(cell);if(style)(styleCells[sheet]??=[]).push(cell);
}
const controls=wb.worksheets.add('밸런스 조정');
controls.getRange('B1').values=[['노량진 성장과 구매 비용']];
controls.getRange('B2:H2').values=[['항목','기본값','레벨당 증가','첫 가격','가격 증가','수치 타입','가격 타입']];
const tuning=[
 ['ATT',0,6,50,25,'value','Coin'],['HP',0,15,45,25,'value','Coin'],
 ['ATT_SPEED',0,5,65,35,'percent','Coin'],['PROJECTILE_SPEED',0,4,55,25,'percent','Coin'],
 ['BOSS_DAMAGE',0,10,80,40,'percent','Coin'],['COIN_BONUS',0,10,70,40,'percent','Coin'],
 ['HP_REGEN',0,.2,120,60,'value','Coin'],['TUNGTUNGTUNG',90,10,250,100,'percent','Coin'],
 ['BOOMBAR',15,5,350,120,'percent','Coin']];
controls.getRange('B3:H11').values=tuning;
controls.getRange('B14:C14').values=[['적 성장 기준','값']];
controls.getRange('B15:C22').values=[['첫 일반 공격력',33],['첫 일반 체력',55],['일반 공격력 증가율',.08],['일반 체력 증가율',.12],['엘리트 공격력 배수',1.6],['엘리트 체력 배수',1.8],['보스 공격력 배수',3],['보스 체력 배수',3.4]];
controls.getRange('C17:C18').setNumberFormat('0%');
controls.getRange('B25').values=[['체력 회복은 초당 수치. 지원군은 종류별 한 명이며 비율은 체력/공격력에 적용.']];
const up=wb.worksheets.getItem('업그레이드').getRange('A1:Q1200').values;
const notes={ATT:'기본 공격력 증가',HP:'최대 체력 증가',ATT_SPEED:'공격 속도 증가',PROJECTILE_SPEED:'미사일 지속 시간 증가',BOSS_DAMAGE:'보스에게 주는 피해 증가',COIN_BONUS:'획득 코인 증가',HP_REGEN:'초당 체력 회복',TUNGTUNGTUNG:'퉁퉁퉁 한 명 · 플레이어 체력 비율',BOOMBAR:'붐바르 한 명 · 플레이어 공격력 비율'};
for(let i=0;i<up.length;i++){
  const row=up[i],index=tuning.findIndex(t=>t[0]===row[2]);if(index<0||typeof row[3]!=='number')continue;
  const r=i+1,c=index+3;
  set('업그레이드',`F${r}`,`='밸런스 조정'!$C$${c}+D${r}*'밸런스 조정'!$D$${c}`,true,row[2]==='HP_REGEN');
  set('업그레이드',`G${r}`,tuning[index][5]);set('업그레이드',`H${r}`,'Coin');
  set('업그레이드',`I${r}`,`='밸런스 조정'!$E$${c}+(D${r}-1)*'밸런스 조정'!$F$${c}`,true);
  set('업그레이드',`J${r}`,notes[row[2]]);
  if(row[2]==='HP_REGEN')wb.worksheets.getItem('업그레이드').getRange(`F${r}`).setNumberFormat('0.0');
  if(row[2]==='PROJECTILE_SPEED')set('업그레이드',`E${r}`,'미사일 지속 시간');
}
const enemy=wb.worksheets.getItem('적 배치').getRange('A1:Q40').values;
let previous=0;
for(let i=2;i<enemy.length;i++){
  const row=enemy[i];if(typeof row[3]!=='string'||!row[3].startsWith('SR18_L_E')||row[3].endsWith('_Right'))continue;
  const r=i+1,tier=row[12];
  if(tier==='Normal'){
    set('적 배치',`P${r}`,"='밸런스 조정'!$C$18",true);
    set('적 배치',`N${r}`,previous?`=ROUND(N${previous}*(1+'밸런스 조정'!$C$17),0)`:"='밸런스 조정'!$C$15",true);
    set('적 배치',`O${r}`,previous?`=ROUND(O${previous}*(1+P${r}),0)`:"='밸런스 조정'!$C$16",true);
    previous=r;
  }else{
    const a=tier==='Boss'?21:19,h=tier==='Boss'?22:20;
    set('적 배치',`N${r}`,`=ROUND(N${previous}*'밸런스 조정'!$C$${a},0)`,true);
    set('적 배치',`O${r}`,`=ROUND(O${previous}*'밸런스 조정'!$C$${h},0)`,true);
  }
  set('적 배치',`L${r}`,tier==='Normal'?'공격력은 공통 성장률, 체력은 해당 행 증가율 적용.':'직전 일반 적 수치에 등급 배수 적용.');
  if(row[3].startsWith('SR18_L_E10_')||row[3].startsWith('SR18_L_E19_')){
    set('적 배치',`G${r}`,1.4);set('적 배치',`H${r}`,2);set('적 배치',`L${r}`,'좌우 왕복 · 차선 안에서 조준 방향 변경');
  }
}
// Companion references stay live; refresh their cached results in the exported candidate.
for(const cell of ['N28','O28','N29','O29'])(changed['적 배치']??=[]).push(cell);
set('적 배치','P2','체력증가율');set('적 배치','B1','성장률과 등급 배수는 밸런스 조정 시트에서 편집. P열은 일반 적 체력 증가율. 다음 시작에 적용.');
const hazards=wb.worksheets.getItem('기믹 배치').getRange('A1:I40').values;
for(let i=2;i<hazards.length;i++)if(hazards[i][5]==='Hole'){
  set('기믹 배치',`G${i+1}`,80);set('기믹 배치',`H${i+1}`,'파손된 판자 · 피해80, 생존하면 전진 유지');
}
const env=wb.worksheets.getItem('환경 변수');
for(const [r,key,value] of [[8,'playerDefaultFireRate',1.6],[9,'playerDefaultMissileCount',1]]){
  set('환경 변수',`B${r}`,key);set('환경 변수',`C${r}`,'float');set('환경 변수',`D${r}`,value);
}
for(const r of [7,8,16,17])set('보너스',`L${r}`,'최대 체력과 현재 체력을 함께 증가');
const cosmetics=wb.worksheets.add('커스터마이징');
cosmetics.getRange('B1').values=[['상어 꾸미기 · 외형 전용']];
cosmetics.getRange('B2:I2').values=[['ID','부위','이름','가격타입','가격','비주얼','기본','설명']];
const items=[
 ['skin_original','Skin','오리지널',0,1,'익숙한 푸른 상어'],['skin_coral','Skin','산호빛',150,0,'따뜻한 산호색 피부'],
 ['skin_ice','Skin','얼음빛',220,0,'차가운 푸른 피부'],['skin_sand','Skin','모래빛',260,0,'햇빛을 머금은 피부'],
 ['shoes_original','Shoes','블루 러너',0,1,'기본 파란 운동화'],['shoes_ruby','Shoes','루비 러너',120,0,'짙은 루비색 운동화'],
 ['shoes_mint','Shoes','민트 러너',160,0,'산뜻한 민트색 운동화'],['shoes_gold','Shoes','골드 러너',200,0,'반짝이는 금빛 운동화'],
 ['hat_none','Hat','모자 없음',0,1,'가볍게 출발'],['hat_cap','Hat','항구 캡',180,0,'챙이 있는 남색 모자'],
 ['hat_bucket','Hat','낚시 모자',220,0,'넓은 챙의 낚시 모자'],['hat_tophat','Hat','신사 모자',300,0,'금색 띠의 높은 모자']];
cosmetics.getRange('B3:I14').values=items.map(([id,slot,name,price,base,desc])=>[id,slot,name,'Coin',price,id,base,desc]);
for(const sh of [controls,cosmetics]){
  sh.getUsedRange().format.font={name:'Arial',size:11};sh.getUsedRange().format.rowHeight=27;sh.showGridLines=false;
  sh.getRange('B:B').format.columnWidth=29;sh.getRange('C:H').format.columnWidth=16;sh.getRange('I:I').format.columnWidth=36;
  sh.getRange(sh===controls?'B2:H2':'B2:I2').format={fill:'#DDE6ED',font:{bold:true,name:'Arial',size:11},rowHeight:30};
  sh.getRange('B1:I1').format.font={name:'Arial',bold:true,size:14};
  sh.freezePanes.freezeRows(2);
}
controls.getRange('B14:C14').format={fill:'#DDE6ED',font:{name:'Arial',bold:true,size:11}};
controls.getRange('C3:F11').format.fill='#FFF6DC';controls.getRange('C15:C22').format.fill='#FFF6DC';
cosmetics.getRange('E3:F14').format.fill='#FFF6DC';cosmetics.getRange('F3:F14').setNumberFormat('#,##0');
cosmetics.getRange('B3:B14').format.font={name:'Arial',size:10};cosmetics.getRange('G:G').format.columnWidth=23;
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:30},maxChars:1800});console.log(errors.ndjson);
for(const [name,range,file] of [['밸런스 조정','B1:I25','balance-controls.png'],['커스터마이징','B1:I14','cosmetics-data.png'],['업그레이드','B2:J8','upgrades-after.png']]){
  const image=await wb.render({sheetName:name,range,scale:1.15,format:'png'});await fs.writeFile(`${work}/${file}`,new Uint8Array(await image.arrayBuffer()));
}
await fs.writeFile(`${work}/changes.json`,JSON.stringify({beforeHash:crypto.createHash('sha256').update(bytes).digest('hex'),changed,styleCells,newSheets},null,2));
await(await SpreadsheetFile.exportXlsx(wb)).save(`${work}/candidate.xlsx`);
console.log(JSON.stringify({enemyFirst:wb.worksheets.getItem('적 배치').getRange('M3:P5').values,enemyLast:wb.worksheets.getItem('적 배치').getRange('M26:O29').values,cosmetics:items.length}));
