import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const folder='tmp/mobile-feedback-2026-09-20/workbook';
const require=createRequire(path.resolve(folder,'loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const fatman=process.argv.includes('--fatman');
const book=await SpreadsheetFile.importXlsx(await FileBlob.load(folder+(fatman?'/before-damage.xlsx':'/before.xlsx')));
const sheet=book.worksheets.getItem('적 배치');
const rows=sheet.getRange('A1:U400').values;
const headers=rows.find(row=>row.includes('배치ID'));
const idColumn=headers.indexOf('배치ID'),healthColumn=headers.indexOf('체력'),tierColumn=headers.indexOf('타입'),enabledColumn=headers.indexOf('사용');
if(Math.min(idColumn,healthColumn,tierColumn)<0)throw Error('Missing enemy placement columns');
const valueColumn=fatman?headers.indexOf('공격력'):healthColumn;
const matches=rows.map((row,index)=>({row,index})).filter(({row})=>fatman?String(row[idColumn]??'').includes('FatMan'):String(row[idColumn]??'').includes('Woman')&&row[tierColumn]==='Boss');
if(matches.length===0)throw Error('No female boss rows');
const edits=matches.map(({row,index})=>{
 const cell=String.fromCharCode(65+valueColumn)+(index+1),range=sheet.getRange(cell);
 return {sheet:'적 배치',cell,id:row[idColumn],enabled:Number(row[enabledColumn])===1,before:range.values[0][0],formula:range.formulas[0][0]};
});
const first=Math.min(...matches.map(x=>x.index))+1,last=Math.max(...matches.map(x=>x.index))+1;
const range=String.fromCharCode(65+Math.max(0,healthColumn-2))+Math.max(1,first-1)+':'+String.fromCharCode(65+Math.min(20,healthColumn+1))+(last+1);
if(!process.argv.includes('--apply')){
 const preview=await book.render({sheetName:'적 배치',range,scale:1,format:'png'});
 await fs.writeFile(folder+(fatman?'/fatman-before.png':'/before.png'),new Uint8Array(await preview.arrayBuffer()));
 await fs.writeFile(folder+(fatman?'/fatman-inspection.json':'/inspection.json'),JSON.stringify({range,edits},null,2));
 console.log(JSON.stringify({range,edits}));
}else{
 for(const edit of edits.filter(e=>fatman||e.enabled)){
  const range=sheet.getRange(edit.cell);
  if(fatman){range.values=[[100]];}
  else if(edit.formula){range.formulas=[[`=ROUND((${edit.formula.replace(/^=/,'')})*1.5,0)`]];}
  else range.values=[[Number(edit.before)*1.5]];
 }
 // The disabled right-hand duplicate depends on the left boss's cell. Recalculate
 // its cached result without applying the multiplier twice or rewriting its formula.
 for(const edit of edits){
  edit.after=sheet.getRange(edit.cell).values[0][0];
  if(Math.abs(edit.after-(fatman?100:Math.round(edit.before*1.5)))>1)throw Error('Incorrect stat value at '+edit.cell);
 }
 const afterRows=sheet.getRange('A1:U400').values;
 const targets=new Set(edits.map(e=>e.id));
 const unexpected=rows.flatMap((row,i)=>!targets.has(row[idColumn])&&row[valueColumn]!==afterRows[i][valueColumn]?[{id:row[idColumn],before:row[valueColumn],after:afterRows[i][valueColumn]}]:[]);
 // Import/render recalculates legacy formula caches. The installer grafts only
 // the requested cells and verifies every other original cell is preserved.
 await fs.writeFile(folder+'/candidate-recalculation-differences.json',JSON.stringify(unexpected,null,2));
 const errors=await book.inspect({kind:'match',sheetId:'적 배치',range,searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!',options:{useRegex:true,maxResults:10},maxChars:1000});
 await fs.writeFile(folder+'/errors.txt',errors.ndjson);
 const preview=await book.render({sheetName:'적 배치',range,scale:1,format:'png'});
 await fs.writeFile(folder+(fatman?'/fatman-after.png':'/after.png'),new Uint8Array(await preview.arrayBuffer()));
 await fs.mkdir('outputs/mobile-feedback-2026-09-20',{recursive:true});
 await(await SpreadsheetFile.exportXlsx(book)).save('outputs/mobile-feedback-2026-09-20/Data.xlsx');
 await fs.writeFile(folder+(fatman?'/fatman-changes.json':'/changes.json'),JSON.stringify(edits,null,2));
 console.log(JSON.stringify({edits,errors:errors.ndjson}));
}
