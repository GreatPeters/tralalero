import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const folder='tmp/campaign-balance-2026-09-23/workbook';
const require=createRequire(path.resolve(folder,'loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const previewCurrent=process.argv.includes('--preview-current');
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(previewCurrent?'Assets/ShooterSurvival/GameData/Editor/Data.xlsx':folder+'/before.xlsx'));
if(process.argv.includes('--preview')||previewCurrent){
 for(const [sheet,range,label] of (previewCurrent?[['밸런스 조정','B71:D97','later-chapter-controls-current']]:[['밸런스 조정','B2:H11','upgrade-controls'],['밸런스 조정','B53:D69','growth-controls']])){
  const image=await wb.render({sheetName:sheet,range,scale:1.3,format:'png'});
  await fs.writeFile(folder+'/before-'+label+'.png',new Uint8Array(await image.arrayBuffer()));
 }
 console.log('Baseline previews rendered');
}else{
 const config=JSON.parse(await fs.readFile(process.argv[2],'utf8'));
 for(const edit of config.edits){const range=wb.worksheets.getItem(edit.sheet).getRange(edit.cell);if(edit.formula)range.formulas=[[edit.formula]];else range.values=[[edit.value]];}
 const checks={};
 for(const [sheet,range] of [['밸런스 조정','B71:D97'],['업그레이드','B3:J8'],['적 배치','M3:R8']]){
  checks[sheet]=(await wb.inspect({kind:'table',range:sheet+'!'+range,include:'values,formulas',tableMaxRows:6,tableMaxCols:8,maxChars:4500})).ndjson;
 }
 const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:30},maxChars:3000});
 const out='outputs/campaign-balance-2026-09-23/'+config.revision;await fs.mkdir(out,{recursive:true});
 await fs.writeFile(out+'/checks.json',JSON.stringify({checks,errors:errors.ndjson},null,2));
 await fs.writeFile(out+'/errors.ndjson',errors.ndjson);
 const image=await wb.render({sheetName:'밸런스 조정',range:'B71:D97',scale:1.3,format:'png'});
 await fs.writeFile(out+'/growth-controls.png',new Uint8Array(await image.arrayBuffer()));
 await (await SpreadsheetFile.exportXlsx(wb)).save(out+'/artifact.xlsx');
 console.log(out);
}
