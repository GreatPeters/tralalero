import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const work='tmp/combat-route-workbook';
const require=createRequire(path.resolve(work,'loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const book=await SpreadsheetFile.importXlsx(await FileBlob.load('Assets/ShooterSurvival/GameData/Editor/Data.xlsx'));
const sheet=book.worksheets.getItem('적 배치');
const rows=sheet.getRange('A1:U400').values;
if(!process.argv.includes('--apply')) {
 await fs.writeFile(work+'/rows.json',JSON.stringify({values:rows,formulas:sheet.getRange('A1:U400').formulas}));
 const preview=await book.render({sheetName:'적 배치',range:'B1:Q11',scale:1,format:'png'});
 await fs.writeFile(work+'/before.png',new Uint8Array(await preview.arrayBuffer()));
 console.log(JSON.stringify(rows.slice(0,4)));process.exit(0);
}
const headers=rows.find(r=>r.includes('배치ID'));
const col=name=>{const i=headers.indexOf(name);if(i<0)throw Error('Missing '+name);return String.fromCharCode(65+i);};
const changes=[];
function set(i,header,value,formula=false){const cell=col(header)+(i+1);const range=sheet.getRange(cell);changes.push({sheet:'적 배치',cell,before:range.values[0][0],value,formula});range[formula?'formulas':'values']=[[value]];}
for(let i=2;i<rows.length;i++){
 const row=rows[i],id=String(row[headers.indexOf('배치ID')]??'');if(!id)continue;
 if(id.includes('FatMan')){
  set(i,'이벤트','사격');set(i,'이동속도',0);set(i,'이동거리',0);set(i,'발사준비초',.62);set(i,'투사체속도',18);set(i,'발동앞거리',16);
 }
 if(id.includes('Guard'))set(i,'발사준비초',.45);
 if(id.includes('Woman')&&id.endsWith('_Right'))set(i,'사용',0);
 if(id==='SR18_L_E06_T065_Enemy_YllowMan_Net')set(i,'사용',0);
 if(id==='SR18_L_E06_T065_Enemy_YllowMan_Net_Right'){
  set(i,'타입','Boss');
  // Preserve the existing formula-based progression, using the nearby normal
  // encounter as the early boss baseline. Tier separation is explicit in data.
  const left=rows.findIndex(r=>r[headers.indexOf('배치ID')]==='SR18_L_E06_T065_Enemy_YllowMan_Net');
  set(i,'체력',`=ROUND(${col('체력')}${left+1}*3,0)`,true);
 }
}
for(const item of changes)if(sheet.getRange(item.cell).values[0][0]==null)throw Error('Empty '+item.cell);
const errors=await book.inspect({kind:'match',sheetId:'적 배치',range:'A1:U400',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!',options:{useRegex:true,maxResults:20},maxChars:2000});
await fs.writeFile(work+'/error-scan.txt',errors.ndjson);
await fs.writeFile(work+'/changes.json',JSON.stringify(changes,null,2));
await (await SpreadsheetFile.exportXlsx(book)).save(work+'/candidate.xlsx');
const preview=await book.render({sheetName:'적 배치',range:'B1:Q11',scale:1,format:'png'});
await fs.writeFile(work+'/after.png',new Uint8Array(await preview.arrayBuffer()));
console.log(JSON.stringify({changedCells:changes.length,scan:errors.ndjson}));
