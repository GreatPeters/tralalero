import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const req=createRequire(path.resolve('tmp/sr18-data-work-2026-09-07/loader.cjs'));
const {SpreadsheetFile}=await import(pathToFileURL(req.resolve('@oai/artifact-tool')).href);
const work='tmp/sr18-polish-2026-09-09';
const source='Assets/ShooterSurvival/GameData/Editor/Data.xlsx';
await fs.mkdir(work,{recursive:true});
const bytes=await fs.readFile(source);
const wb=await SpreadsheetFile.importXlsx(new Uint8Array(bytes));
const sheet=wb.worksheets.getItem('적 배치');
if(sheet.getRange('M2').values[0][0]) throw Error('Combat columns already exist; refusing reseed.');
await fs.writeFile(`${work}/Data.before.xlsx`,bytes,{flag:'wx'});
const enemies=JSON.parse(await fs.readFile(`${work}/before-enemies.json`,'utf8'));
sheet.getRange('M2:P2').values=[['타입','공격력','체력','증가율']];
sheet.getRange('M2:P2').format={fill:'#DDE6ED',font:{name:'Arial',size:11,bold:true},rowHeight:28};
sheet.getRange('M3:P27').format.font={name:'Arial',size:11};
sheet.getRange('M:P').format.columnWidth=14;
sheet.getRange('M3:P27').format.fill='#FFF6DC';
sheet.getRange('M3:M27').dataValidation={rule:{type:'list',values:['Normal','Elite','Boss']}};
sheet.getRange('N3:O27').setNumberFormat('#,##0');
sheet.getRange('P3:P27').setNumberFormat('0%');
let previousNormal=0;
for(let i=0;i<25;i++) {
  const r=i+3, old=sheet.getRange(`B${r}:L${r}`).values[0];
  const e=enemies.find(e=>e.id===old[2]); if(!e) throw Error(old[2]);
  sheet.getRange(`M${r}`).values=[[e.tier]];
  sheet.getRange(`P${r}`).values=[[.30]];
  if(e.tier==='Normal') {
    if(previousNormal===0) sheet.getRange(`N${r}:O${r}`).values=[[33,55]];
    else sheet.getRange(`N${r}:O${r}`).formulas=[[`=ROUND(N${previousNormal}*(1+P${r}),0)`,`=ROUND(O${previousNormal}*(1+P${r}),0)`]];
    previousNormal=r;
  } else {
    // Keep a rare tier stronger than the contemporary Normal; do not restart Elite at 55 late in the map.
    sheet.getRange(`N${r}:O${r}`).formulas=[[`=ROUND(N${previousNormal}*IF(M${r}="Boss",5,55/33),0)`,`=ROUND(O${previousNormal}*IF(M${r}="Boss",190/55,2),0)`]];
  }
  sheet.getRange(`L${r}`).values=[[e.tier==='Normal'?'공격력·체력 직접 편집 가능. 기본 수식: 앞 Normal × (1+증가율).':'공격력·체력 직접 편집 가능. 초기 수식은 직전 Normal 대비 타입 배수.']];
  if(old[4]==='전방 매복 사격') {
    sheet.getRange(`G${r}:K${r}`).values=[[3,8,44,.8,14]];
    sheet.getRange(`L${r}`).values=[['먼 진입점 Walk → 중앙 사격. 발동앞거리는 코너 안전구간에 의해 제한됨.']];
  }
}
sheet.getRange('B1').values=[['공격력·체력·타입은 이 시트가 최종 기준. Normal 순차 +30%; Elite/Boss는 직전 Normal 배수. 수식 직접 덮어쓰기 가능. 다음 Play 적용.']];
const gimmicks=wb.worksheets.getItem('기믹 배치');
for(let r=3;r<=26;r++) if(gimmicks.getRange(`F${r}`).values[0][0]==='Bucket')
  gimmicks.getRange(`H${r}`).values=[['3개 묶음 전체에 적용. 효과값: 바구니별 사격 금지 시간(초).']];
await(await SpreadsheetFile.exportXlsx(wb)).save(`${work}/artifact-candidate.xlsx`);
for(const [name,range,file] of [['적 배치','L2:P12','combat-columns.png'],['적 배치','E2:K8','ambush-settings.png'],['기믹 배치','B2:H6','bucket-settings.png']]) {
  const preview=await wb.render({sheetName:name,range,scale:1.4,format:'png'});
  await fs.writeFile(`${work}/${file}`,new Uint8Array(await preview.arrayBuffer()));
}
console.log(JSON.stringify(sheet.getRange('M2:P27').values));
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:10},maxChars:1000})).ndjson);
