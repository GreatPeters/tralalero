const LOG_LIMIT=5000;
function onOpen(){SpreadsheetApp.getUi().createMenu('플레이 로그').addItem('지금 새로고침','refreshPlayerLogs').addItem('매일 자동 갱신','installDailyRefresh').addItem('자동 갱신 끄기','removeDailyRefresh').addToUi();}
function removeDailyRefresh(){ScriptApp.getProjectTriggers().filter(t=>t.getHandlerFunction()==='refreshPlayerLogs').forEach(t=>ScriptApp.deleteTrigger(t));const settings=SpreadsheetApp.getActiveSpreadsheet().getSheetByName('연결설정');if(settings)settings.getRange('B23').setValue('자동 갱신 꺼짐');}
function installDailyRefresh(){refreshPlayerLogs();removeDailyRefresh();ScriptApp.newTrigger('refreshPlayerLogs').timeBased().everyDays(1).atHour(10).inTimezone('Asia/Seoul').create();SpreadsheetApp.getActiveSpreadsheet().getSheetByName('연결설정').getRange('B23').setValue('매일 오전 10~11시 (한국 시간)');}
function numeric(value){if(value==null)return null;const n=Number(value);if(!Number.isFinite(n))throw new Error('숫자 필드가 올바르지 않습니다.');return n;}
function sheetDate(value){const ms=numeric(value);return ms==null?null:ms/86400000+25569+9/24;}
function cell(value){return value==null?{}:{userEnteredValue:typeof value==='number'?{numberValue:value}:{stringValue:String(value)}};}
function gridUpdate(sheetId,row,column,rows){return {updateCells:{range:{sheetId,startRowIndex:row,endRowIndex:row+rows.length,startColumnIndex:column,endColumnIndex:column+rows[0].length},rows:rows.map(values=>({values:values.map(cell)})),fields:'userEnteredValue'}};}
function readSettings(sheet){
  const values=sheet.getRange('B5:B10').getValues().flat();
  const [project,dataset,location,days,maxGiB,rowLimit]=values;
  if(!/^[a-z][a-z0-9-]{4,62}$/.test(project)||!/^analytics_[0-9]+$/.test(dataset)||!/^([a-z]+-[a-z]+[0-9]+|US|EU)$/.test(location))throw new Error('연결설정의 프로젝트·데이터세트·리전을 확인하세요.');
  if(!Number.isInteger(days)||days<1||days>90||!Number.isFinite(maxGiB)||maxGiB<=0||maxGiB>100||!Number.isInteger(rowLimit)||rowLimit<1||rowLimit>LOG_LIMIT)throw new Error('조회 기간은 1~90일, 조회 한도는 0~100 GiB, 최대 행 수는 1~5000이어야 합니다.');
  return {project,dataset,location,days,maxGiB,rowLimit};
}
function refreshPlayerLogs(){
  const lock=LockService.getDocumentLock();if(!lock.tryLock(1000))throw new Error('다른 새로고침이 진행 중입니다.');
  const book=SpreadsheetApp.getActiveSpreadsheet();const settings=book.getSheetByName('연결설정');let job;
  try{
    const config=readSettings(settings);const log=book.getSheetByName('플레이로그');if(!log)throw new Error('플레이로그 시트가 없습니다.');
    const utcToday=new Date().toISOString().slice(0,10);const end=new Date(utcToday+'T00:00:00Z');end.setUTCDate(end.getUTCDate()-1);const start=new Date(end);start.setUTCDate(start.getUTCDate()-config.days+1);
    const parameter=(name,type,value)=>({name,parameterType:{type},parameterValue:{value:String(value)}});
    const request={query:ROUND_QUERY.split('YOUR_PROJECT.analytics_PROPERTY_ID').join(config.project+'.'+config.dataset),useLegacySql:false,location:config.location,maximumBytesBilled:String(Math.floor(config.maxGiB*1073741824)),parameterMode:'NAMED',queryParameters:[parameter('reporting_start','DATE',start.toISOString().slice(0,10)),parameter('reporting_end','DATE',end.toISOString().slice(0,10)),parameter('row_limit','INT64',config.rowLimit+1)],timeoutMs:10000,maxResults:1000};
    let response=BigQuery.Jobs.query(request,config.project);job=response.jobReference;const deadline=Date.now()+180000;
    while(!response.jobComplete){if(Date.now()>deadline)throw new Error('조회 시간이 3분을 넘었습니다. 기존 로그를 유지합니다.');Utilities.sleep(1000);response=BigQuery.Jobs.getQueryResults(job.projectId,job.jobId,{location:job.location,maxResults:1000});}
    if(response.errors&&response.errors.length)throw new Error(response.errors.map(e=>e.message).join('\n'));
    const fields=response.schema.fields;if(fields.length!==20)throw new Error('조회 결과 열 구성이 바뀌었습니다.');
    let rows=response.rows||[];
    while(response.pageToken){if(Date.now()>deadline)throw new Error('결과 다운로드 시간 초과');response=BigQuery.Jobs.getQueryResults(job.projectId,job.jobId,{location:job.location,pageToken:response.pageToken,maxResults:1000});rows=rows.concat(response.rows||[]);if(rows.length>config.rowLimit)throw new Error('행 수 한도를 넘었습니다. 조회 기간을 줄이세요. 기존 로그를 유지합니다.');}
    if(rows.length>config.rowLimit)throw new Error('행 수 한도를 넘었습니다. 조회 기간을 줄이세요.');
    const data=rows.map(row=>row.f.map((v,i)=>i<2?sheetDate(v.v):[4,7,8,9,10,11,16,17,18,19].includes(i)?numeric(v.v):v.v==null?null:String(v.v)));
    const filled=data.length;while(data.length<LOG_LIMIT)data.push(Array(20).fill(null));
    const refreshed=sheetDate(Date.now());
    const updates=[gridUpdate(log.getSheetId(),5,0,data),gridUpdate(settings.getSheetId(),12,1,[[refreshed],[filled],[filled?'갱신 완료':'연결 정상 · 조회 기간에 플레이 기록 없음'],[start.toISOString().slice(0,10)],[end.toISOString().slice(0,10)]])];
    if(log.getMaxRows()<LOG_LIMIT+5)updates.unshift({appendDimension:{sheetId:log.getSheetId(),dimension:'ROWS',length:LOG_LIMIT+5-log.getMaxRows()}});
    // One atomic request preserves the previous dataset on query or write failure.
    // Explicit stringValue keeps IDs literal and prevents formula execution.
    Sheets.Spreadsheets.batchUpdate({requests:updates},book.getId());
  }catch(error){
    if(job)try{BigQuery.Jobs.cancel(job.projectId,job.jobId,{location:job.location});}catch(_){}
    if(settings)settings.getRange('B15').setValue('갱신 실패: '+String(error.message).slice(0,350));
    throw error;
  }finally{lock.releaseLock();}
}
