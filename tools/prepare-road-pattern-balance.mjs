import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const work='tmp/road-pattern-balance-20260913';
const require=createRequire(path.resolve(work,'loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const source='Assets/ShooterSurvival/GameData/Editor/Data.xlsx';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const liveBytes=await fs.readFile(source), liveHash=hash(liveBytes);
let bytes=liveBytes, baseline=source;
try {bytes=await fs.readFile(`${work}/before.xlsx`);baseline=`${work}/before.xlsx`;} catch(e) {if(e.code!=='ENOENT')throw e;}
let allowed=[hash(bytes)];
try {const report=JSON.parse(await fs.readFile('outputs/road-patterns-2026-09-13/balance/verification.json','utf8'));allowed.push(report.candidateSha256,report.installFromHash);} catch(e) {if(e.code!=='ENOENT')throw e;}
if(hash(bytes)!=='c233f05baec8f91ae8ab4b9437ce06f11509068914a06bbbaad7fb6add43988b'||!allowed.includes(liveHash)) throw new Error('Canonical workbook changed; review before authoring');
await fs.mkdir(work,{recursive:true});
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(baseline));
async function render(range,file){const blob=await wb.render({sheetName:'밸런스 조정',range,scale:1.15,format:'png'});await fs.writeFile(`${work}/${file}.png`,new Uint8Array(await blob.arrayBuffer()));}
if(process.argv.includes('--preview')) {
 await render('B100:D107','before');
 console.log((await wb.inspect({kind:'table',range:'환경 변수!A1:F45',tableMaxRows:45,tableMaxCols:6,maxChars:5500})).ndjson);
 for(const name of ['적 배치','보너스 배치','기믹 배치']) console.log((await wb.inspect({kind:'table',range:`'${name}'!A1:G5`,maxChars:1800})).ndjson);
 process.exit(0);
}
try {await fs.writeFile(`${work}/before.xlsx`,bytes,{flag:'wx'});} catch(e) {if(e.code!=='EEXIST'||hash(await fs.readFile(`${work}/before.xlsx`))!==hash(bytes))throw e;}
const controls=wb.worksheets.getItem('밸런스 조정');
const rows=[
 ['정면 차량 경고 시간',2.2,'초, 경고한 차로로만 접근'],
 ['정면 차량 속도',16,'월드 유닛/초'],
 ['정면 차량 충돌 피해',220,'차량마다 한 번 적용'],
 ['초록 우회로 회복 비율',.1,'최대 체력 기준, 우회로마다 한 번'],
 ['식당 방어전 시간',30,'초, 일시정지 시간 제외'],
 ['식당 경찰 체력',700,'접촉 시 남은 체력만큼 피해'],
 ['식당 경찰 이동 속도',3.8,'월드 유닛/초, 마지막 구간 12% 증가'],
 ['식당 경찰 진입 간격',1.05,'초, 마지막 구간 간격 25% 감소'],
 ['식당 탈출 회복 비율',.12,'최대 체력 기준, 성공 시 한 번']
];
controls.getRange('B109:D109').copyFrom(controls.getRange('B71:D71'),'all');
controls.getRange('B109:D109').values=[['고속도로 분기와 식당 방어전','값','설명']];
for(let i=0;i<rows.length;i++) {const r=110+i;controls.getRange(`B${r}:D${r}`).copyFrom(controls.getRange('B106:D106'),'all');controls.getRange(`B${r}:D${r}`).values=[rows[i]];}
controls.getRange('B110:D118').format.rowHeight=44;
controls.getRange('D110:D118').format.wrapText=true;
controls.getRange('B109:D109').format={fill:'#DBE5EF',font:{name:'Malgun Gothic',size:11,bold:true}};
controls.getRange('B110:D118').format.font={name:'Malgun Gothic',size:11};
controls.getRange('C110:C118').format.fill='#FFF6DC';
controls.getRange('C113').setNumberFormat('0%'); controls.getRange('C118').setNumberFormat('0%');
controls.getRange('C88').values=[[7.8]];
controls.getRange('D88').values=[['전진 약269초 + 식당 방어전30초']];
const env=wb.worksheets.getItem('환경 변수');
const keys=['highwayOncomingWarning','highwayOncomingSpeed','highwayOncomingDamage','highwayBypassHeal','reststopHoldoutSeconds','reststopPoliceHealth','reststopPoliceSpeed','reststopPoliceInterval','reststopHoldoutHeal'];
const values=env.getRange('A1:G200').values;
let last=values.length;
while(last>0&&!values[last-1].some(v=>v!==null&&v!==''))last--;
for(let i=0;i<keys.length;i++) {
 const r=last+1+i;
 env.getRange(`B${r}:D${r}`).copyFrom(env.getRange('B25:D25'),'all');
 env.getRange(`B${r}:D${r}`).values=[[keys[i],'float',null]];
 env.getRange(`D${r}`).formulas=[[`='밸런스 조정'!C${110+i}`]];
}
const disabled=[];
for(const name of ['적 배치','보너스 배치','기믹 배치']) {
 const sheet=wb.worksheets.getItem(name);const matrix=sheet.getRange('A1:T200').values;
 for(let i=0;i<matrix.length;i++) {
  const row=matrix[i]; const id=row.find(v=>typeof v==='string'&&/^RST_[EBG](11|12)(_|$)/.test(v));
  if(!id)continue;
  // Existing sheet contract: C scene, D stable ID, E enabled.
  if(row[2]!=='RestStop'||row[3]!==id)throw new Error(`Unexpected placement columns ${name}`);
  sheet.getRange(`E${i+1}`).values=[[0]];disabled.push({sheet:name,row:i+1,id});
 }
}
if(disabled.length!==8)throw new Error(`Expected 8 replaced placement rows, got ${disabled.length}`);
await render('B109:D118','after');
const check=await wb.inspect({kind:'table',range:"'밸런스 조정'!B109:D118",include:'values,formulas',tableMaxRows:10,tableMaxCols:3,maxChars:5000});
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!|#NULL!',options:{useRegex:true,maxResults:20},maxChars:2500});
await fs.writeFile(`${work}/inspection.json`,JSON.stringify({controls:check.ndjson,errors:errors.ndjson,disabled},null,2));
await fs.writeFile(`${work}/changes.json`,JSON.stringify({beforeHash:hash(bytes),installFromHash:liveHash,sheets:['밸런스 조정','환경 변수','적 배치','보너스 배치','기믹 배치'],checks:{disabled,keys,reststopSpeed:7.8}},null,2));
await (await SpreadsheetFile.exportXlsx(wb)).save(`${work}/artifact-candidate.xlsx`);
console.log(JSON.stringify({keys:keys.length,disabled:disabled.length,before:hash(bytes),inspection:check.ndjson,errors:errors.ndjson}));
