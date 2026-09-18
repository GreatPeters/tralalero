import fs from 'node:fs';import vm from 'node:vm';import assert from 'node:assert/strict';
const code=fs.readFileSync(new URL('./Code.gs',import.meta.url),'utf8');
function environment(options={}){
 let writes=[],errors=[],released=false,cancelled=false;
 const settings={getRange:range=>({getValues:()=>[["tralaleroshooter"],[options.dataset??'analytics_123456'],['US'],[30],[1],[2]],setValue:value=>errors.push({range,value})}),getSheetId:()=>3};
 const log={getSheetId:()=>2,getMaxRows:()=>1000};const book={getSheetByName:name=>name==='연결설정'?settings:log,getId:()=>"test"};
 let pages=[...(options.pages||[{jobComplete:true,schema:{fields:Array(20).fill({})},rows:[]}])];
 const ctx={Date,Math,Number,Array,String,Error,console,ROUND_QUERY:'-- YOUR_PROJECT.analytics_PROPERTY_ID\nSELECT * FROM `YOUR_PROJECT.analytics_PROPERTY_ID.events_*`',
  SpreadsheetApp:{getActiveSpreadsheet:()=>book},LockService:{getDocumentLock:()=>({tryLock:()=>true,releaseLock:()=>released=true})},Utilities:{sleep:()=>{}},
  BigQuery:{Jobs:{query:req=>{assert.equal(req.useLegacySql,false);assert.equal(req.maximumBytesBilled,'1073741824');assert.ok(!req.query.includes('YOUR_PROJECT'));assert.ok(req.query.includes('`tralaleroshooter.analytics_123456.events_*`'));return {...pages.shift(),jobReference:{projectId:'p',jobId:'j',location:'US'}};},getQueryResults:()=>pages.shift(),cancel:()=>cancelled=true}},
  Sheets:{Spreadsheets:{batchUpdate:request=>{if(options.failWrite)throw new Error('write failed');writes.push(request);}}}};
 vm.createContext(ctx);vm.runInContext(code,ctx);return {ctx,writes,errors,released:()=>released,cancelled:()=>cancelled};
}
function row(id='0000123'){const values=['0','1000',id,'=HYPERLINK("bad")','1','Noryangjin','death','30.5','0.1','5','1','10','complete','ATT:1',null,null,'0','0','1','1'];return {f:values.map(v=>({v}))};}
{
 const e=environment({pages:[{jobComplete:true,schema:{fields:Array(20).fill({})},rows:[row()],pageToken:'next'},{jobComplete:true,rows:[row('0000456')]}]});e.ctx.refreshPlayerLogs();assert.equal(e.writes.length,1);const update=e.writes[0].requests.find(r=>r.updateCells?.range.sheetId===2).updateCells;
 assert.equal(update.rows.length,5000);assert.equal(update.rows[0].values[2].userEnteredValue.stringValue,'0000123');assert.equal(update.rows[0].values[3].userEnteredValue.stringValue,'=HYPERLINK("bad")');assert.equal(update.rows[0].values[7].userEnteredValue.numberValue,30.5);assert.equal(update.rows[0].values[0].userEnteredValue.numberValue,25569.375);assert.equal(Object.keys(update.rows[0].values[14]).length,0);assert.ok(e.released());
}
{
 const e=environment({pages:[{jobComplete:true,schema:{fields:Array(20).fill({})},rows:[row(),row(),row()]}]});assert.throws(()=>e.ctx.refreshPlayerLogs(),/행 수/);assert.equal(e.writes.length,0);assert.ok(e.released());assert.ok(e.cancelled());
}
{const e=environment({dataset:'analytics_1`;DROP TABLE x;'});assert.throws(()=>e.ctx.refreshPlayerLogs(),/연결설정/);assert.equal(e.writes.length,0);assert.ok(e.released());}
{const e=environment({failWrite:true});assert.throws(()=>e.ctx.refreshPlayerLogs(),/write failed/);assert.equal(e.writes.length,0);assert.ok(e.errors.length);assert.ok(e.released());}
{const e=environment();e.ctx.refreshPlayerLogs();assert.equal(e.writes.length,1);const rows=e.writes[0].requests.find(r=>r.updateCells?.range.sheetId===2).updateCells.rows;assert.equal(rows.length,5000);assert.ok(rows.every(r=>r.values.every(c=>Object.keys(c).length===0)));}
console.log('5 connector scenarios passed: paginated typed rows, row cap, invalid settings, failed write, empty refresh.');
