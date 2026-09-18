import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const work='tmp/harbor-refinement-workbook';
const require=createRequire(path.resolve(work,'loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const book=await SpreadsheetFile.importXlsx(await FileBlob.load('Assets/ShooterSurvival/GameData/Editor/Data.xlsx'));
if(process.argv.includes('--corner-fix')){
 const sheet=book.worksheets.getItem('적 배치');const changes=[{sheet:'적 배치',cell:'I3',before:sheet.getRange('I3').values[0][0],value:12}];
 sheet.getRange('I3').values=[[12]];await fs.writeFile(work+'/corner-changes.json',JSON.stringify(changes));await (await SpreadsheetFile.exportXlsx(book)).save(work+'/corner-candidate.xlsx');console.log('First moving enemy activation lead12');process.exit(0);
}
if(process.argv.includes('--apply')){
 const sheet=book.worksheets.getItem('적 배치');const rows=sheet.getRange('A1:U220').values;const changes=[];
 const byId=new Map(rows.map((r,i)=>[r[3],i+1]));
 const set=(cell,value,formula=false)=>{changes.push({sheet:'적 배치',cell,before:sheet.getRange(cell).values[0][0],value,formula});sheet.getRange(cell)[formula?'formulas':'values']=[[value]];};
 for(let i=2;i<rows.length;i++){
  const row=rows[i],id=row[3];if(!id)continue;const n=i+1,right=String(id).endsWith('_Right');
  if(right){const left=byId.get(id.slice(0,-6));if(!left)throw Error('Missing pair '+id);set(`P${n}`,0.15);set(`O${n}`,`=ROUND(O${left}*(1+P${n}),0)`,true);set(`L${n}`,'왼쪽 적보다 체력 15% 증가. 접근 시차를 두고 발동.');}
  if(['공격 반복','공격 한 번'].includes(row[5]))set(`I${n}`,right?5:8);
  else if(String(row[5]).includes('사격')||row[5]==='발사') {set(`I${n}`,Math.min(Number(row[8])||16,right?12:16));if(right)set(`J${n}`,Number(row[9])+.25);}
  else if(right)set(`I${n}`,Math.max(5,(Number(row[8])||18)-4));
  if(id==='SR18_L_E01_T005_Enemy_OldMan'){set(`F${n}`,'이동 후 공격');set(`G${n}`,1.8);set(`H${n}`,2);set(`I${n}`,12);}
 }
 const gimmick=book.worksheets.getItem('기믹 배치');const values=gimmick.getRange('A1:U150').values;
 const bucket=values.findIndex(row=>row[3]==='SR18_L_G01_T013_Bucket');if(bucket<0)throw Error('Opening bucket row missing');
 changes.push({sheet:'기믹 배치',cell:`E${bucket+1}`,before:values[bucket][4],value:0});gimmick.getRange(`E${bucket+1}`).values=[[0]];
 const final=sheet.getRange('A1:U220').values;
 for(let i=2;i<final.length;i++)if(String(final[i][3]).endsWith('_Right')){const left=byId.get(final[i][3].slice(0,-6));const ratio=final[i][14]/final[left-1][14];if(ratio<1.1||ratio>1.2)throw Error(`Health ratio invalid ${final[i][3]} ${ratio}`);}
 await fs.writeFile(work+'/changes.json',JSON.stringify(changes,null,2));
 await (await SpreadsheetFile.exportXlsx(book)).save(work+'/candidate.xlsx');
 const preview=await book.render({sheetName:'적 배치',range:'B1:P9',scale:1,format:'png'});await fs.writeFile(work+'/after.png',new Uint8Array(await preview.arrayBuffer()));
 console.log(JSON.stringify({edited:changes.length,pairs:final.filter(r=>String(r[3]).endsWith('_Right')).length,openingBucketDisabled:true}));process.exit(0);
}
const rows={};
for(const name of ['적 배치','기믹 배치']){
 const sheet=book.worksheets.getItem(name);
 rows[name]={values:sheet.getRange('A1:U220').values,formulas:sheet.getRange('A1:U220').formulas};
}
await fs.writeFile(work+'/before-rows.json',JSON.stringify(rows,null,2));
const preview=await book.render({sheetName:'적 배치',range:'B1:O9',scale:1,format:'png'});
await fs.writeFile(work+'/before.png',new Uint8Array(await preview.arrayBuffer()));
console.log(JSON.stringify(rows['적 배치'].values.slice(0,8)));
