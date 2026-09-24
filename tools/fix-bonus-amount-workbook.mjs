import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const folder='tmp/bonus-amount-fix-2026-09-22';
const require=createRequire(path.resolve(folder,'loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const book=await SpreadsheetFile.importXlsx(await FileBlob.load(folder+'/before.xlsx'));
const sheet=book.worksheets.getItem('보너스');
const rows=sheet.getRange('A1:P60').values;
const header=rows.map(r=>r.map(v=>String(v??'').replace(/\s/g,''))).find(r=>r.includes('항목')&&r.includes('수치타입'));
if(!header)throw Error('Bonus headers missing');
const columns=Object.fromEntries(['식별순번','항목','최소','최대','수치타입'].map(name=>[name,header.indexOf(name)]));
if(Object.values(columns).some(i=>i<0))throw Error('Required columns missing: '+JSON.stringify(header));
const targets=rows.map((r,i)=>({r,i})).filter(({r})=>['att','hp'].includes(r[columns['항목']])&&String(r[columns['수치타입']]).toLowerCase()==='ratio');
if(targets.length!==4)throw Error('Expected four flat ATT/HP rows, found '+targets.length);
const edits=[];
for(const {r,i} of targets)for(const key of ['최소','최대','수치타입']){
 const column=columns[key],cell=String.fromCharCode(65+column)+(i+1),before=r[column];
 const after=key==='수치타입'?'value':Math.round(Number(before)*100*1e6)/1e6;
 edits.push({sheet:'보너스',cell,id:r[columns['식별순번']],before,after});
}
const apply=process.argv.includes('--apply');
if(apply)for(const e of edits)sheet.getRange(e.cell).values=[[e.after]];
const preview=await book.render({sheetName:'보너스',range:'B2:N20',scale:1.2,format:'png'});
await fs.writeFile(folder+(apply?'/after.png':'/before.png'),new Uint8Array(await preview.arrayBuffer()));
await fs.writeFile(folder+'/changes.json',JSON.stringify(edits,null,2));
if(apply){
 for(const e of edits)if(sheet.getRange(e.cell).values[0][0]!==e.after)throw Error('Unexpected value at '+e.cell);
 const errors=await book.inspect({kind:'match',sheetId:'보너스',range:'B2:N20',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!',options:{useRegex:true,maxResults:10},maxChars:1000});
 await fs.writeFile(folder+'/errors.txt',errors.ndjson);
 await fs.mkdir('outputs/bonus-amount-fix-2026-09-22',{recursive:true});
 await(await SpreadsheetFile.exportXlsx(book)).save('outputs/bonus-amount-fix-2026-09-22/Data.xlsx');
}
console.log(JSON.stringify({columns,edits,applied:apply}));
