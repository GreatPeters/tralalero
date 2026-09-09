import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const work='tmp/noryangjin-release-20260910';
const require=createRequire(path.resolve('tmp/sr18-contact-pairs-20260910/loader.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load('Assets/ShooterSurvival/GameData/Editor/Data.xlsx'));
await fs.mkdir(work,{recursive:true});
const result={};
for(const name of ['업그레이드','보너스','스킨','환경 변수','적 배치']){
  try{
    const sh=wb.worksheets.getItem(name);
    const values=sh.getRange('A1:Q1200').values;
    const formulas=sh.getRange('A1:Q1200').formulas;
    result[name]={values,formulas};
    const nonempty=values.map((v,i)=>({row:i+1,v})).filter(r=>r.v.some(v=>v!==null&&v!==''));
    if(name==='업그레이드'){
      const header=nonempty.find(r=>r.v.includes('식별Enum'));const hi=header.v;
      const enumCol=hi.indexOf('식별Enum'),levelCol=hi.indexOf('레벨');
      const groups={};for(const r of nonempty.filter(r=>r.row>header.row)){
        const key=r.v[enumCol];if(!key)continue;(groups[key]??=[]).push(r);
      }
      console.log(JSON.stringify({name,header,groups:Object.entries(groups).map(([key,rows])=>({key,count:rows.length,first:rows[0],last:rows.at(-1)}))}));
    }else console.log(JSON.stringify({name,count:nonempty.length,rows:nonempty.slice(0,name==='보너스'?25:13)}));
  }catch(e){console.log(JSON.stringify({name,error:e.message}));}
}
await fs.writeFile(`${work}/workbook-before.json`,JSON.stringify(result));
