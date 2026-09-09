import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';

const work = path.resolve('tmp/sr18-data-work-2026-09-07');
const require = createRequire(path.join(work, 'loader.cjs'));
const { SpreadsheetFile, Workbook } = await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const source = 'Assets/ShooterSurvival/GameData/Editor/Data.xlsx';
const workbook = await SpreadsheetFile.importXlsx(new Uint8Array(await fs.readFile(source)));
console.log((await workbook.inspect({kind:'workbook,sheet,table',maxChars:4000,tableMaxRows:3,tableMaxCols:8})).ndjson);
console.log((await workbook.inspect({kind:'region',sheetId:'환경 변수',range:'A1:J18',maxChars:4000})).ndjson);
await fs.mkdir(work,{recursive:true});
const preview = await workbook.render({sheetName:'환경 변수',range:'A1:G12',scale:1.4,format:'png'});
await fs.writeFile(path.join(work,'source-environment.png'),new Uint8Array(await preview.arrayBuffer()));
if (process.argv.includes('--verify')) {
  for (const name of ['적 배치','보너스 배치','기믹 배치']) {
    console.log((await workbook.inspect({kind:'region',sheetId:name,range:'B2:L5',maxChars:1500})).ndjson);
    const image=await workbook.render({sheetName:name,range:name==='적 배치'?'B1:L8':name==='보너스 배치'?'B1:G8':'B1:H8',scale:1,format:'png'});
    await fs.writeFile(path.join(work,`${name}-final.png`),new Uint8Array(await image.arrayBuffer()));
  }
  console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!',options:{useRegex:true,maxResults:20},maxChars:2000})).ndjson);
}

if (process.argv.includes('--additions')) {
  const report = JSON.parse(await fs.readFile('map-concepts/sr18-latest-elements-applied-2026-09-07/placement-report.json','utf8'));
  const additions = Workbook.create();
  const scene = 'Noryangjin_MapTool_Mode_SR18';
  const enemyRows = report.placements.filter(r=>r.kind==='enemy').map((r,i)=>{
    const yaw=r.rotation[1]*Math.PI/180;
    const dir=[Math.sin(yaw),Math.cos(yaw)];
    const offset=r.role==='patrol'?4:r.role==='ambush'?-2.5:0;
    const center=[r.position[0]+dir[0]*offset,r.position[2]+dir[1]*offset];
    const gate=report.activationSpots.find(g=>g.enemy===r.name);
    const lead=(center[0]-gate.position[0])*dir[0]+(center[1]-gate.position[2])*dir[1];
    return [i+1,scene,r.name,1,r.role==='patrol'?'왕복':r.role==='ambush'?'전방 매복 사격':r.time===5?'공격 한 번':'공격 반복',r.role==='patrol'?2.5:r.role==='ambush'?4:2,r.role==='patrol'?8:r.role==='ambush'?2.5:0,Math.round(lead*10000)/10000,r.role==='ambush'?.7:2,r.role==='ambush'?14:12,'체력·공격력: 몬스터 성장 시트. 이동거리: 왕복 전체 폭 / 매복 등장 거리.'];
  });
  const bonusRows=report.placements.filter(r=>r.kind==='bonus').map((r,i)=>[i+1,scene,r.name,1,r.subtype,'효과·최소·최대 수치: 보너스 시트']);
  const gimmickRows=report.placements.filter(r=>r.kind==='object').map((r,i)=>[i+1,scene,r.name,1,r.subtype==='Bucket'?'Bucket':r.subtype==='Light'?'Light':'Hole',r.subtype==='Bucket'?3:r.subtype==='Light'?50:999999,r.subtype==='Bucket'?'효과값: 발사 금지 지속 초. 기믹종류는 모델 확인용.':'효과값: 접촉 피해량. 기믹종류는 모델 확인용.']);
  const datasets=[
    ['적 배치',['순번','맵','배치ID','사용','이벤트','이동속도','이동거리','발동앞거리','발사준비초','투사체속도','설명'],enemyRows],
    ['보너스 배치',['순번','맵','배치ID','사용','등급','설명'],bonusRows],
    ['기믹 배치',['순번','맵','배치ID','사용','기믹종류','효과값','설명'],gimmickRows]
  ];
  for (const [name,headers,rows] of datasets) {
    const sheet=additions.worksheets.add(name);
    sheet.getRange('B1').values=[['값을 저장한 뒤 다음 Play부터 적용됩니다. 사용: 1=배치 유지, 0=숨김. 맵·배치ID·기믹종류는 연결용입니다.']];
    const end=String.fromCharCode(65+headers.length);
    sheet.getRange(`B2:${end}${rows.length+2}`).values=[headers,...rows];
    sheet.getRange(`B1:${end}${rows.length+2}`).format.font={name:'Arial',size:11};
    sheet.getRange(`B2:${end}2`).format={fill:'#DDE6ED',font:{name:'Arial',size:11,bold:true},rowHeight:28};
    sheet.getRange('B:B').format.columnWidth=7;
    sheet.getRange('C:C').format.columnWidth=38;
    sheet.getRange('D:D').format.columnWidth=61;
    sheet.getRange(`E:${end}`).format.columnWidth=15;
    sheet.getRange(`${end}:${end}`).format.columnWidth=55;
    sheet.getRange(`B3:${end}${rows.length+2}`).format.rowHeight=30;
    sheet.getRange(`${end}3:${end}${rows.length+2}`).format.wrapText=true;
    sheet.getRange(`E3:E${rows.length+2}`).dataValidation={rule:{type:'list',values:['0','1']}};
    sheet.getRange(`B3:B${rows.length+2}`).setNumberFormat('0');
    sheet.getRange(`E3:E${rows.length+2}`).setNumberFormat('0');
    sheet.getRange(`E3:${String.fromCharCode(64+headers.length)}${rows.length+2}`).format.fill='#FFF6DC';
    sheet.freezePanes.freezeRows(2);
    if(name==='적 배치')sheet.getRange(`F3:F${rows.length+2}`).dataValidation={rule:{type:'list',values:['공격 반복','공격 한 번','사격','왕복','이동 후 공격','전방 매복 사격']}};
    if(name==='보너스 배치')sheet.getRange(`F3:F${rows.length+2}`).dataValidation={rule:{type:'list',values:['Normal','Rare','Unique']}};
    if(name==='기믹 배치')sheet.getRange(`F3:F${rows.length+2}`).format.fill='#FFFFFF';
    const png=await additions.render({sheetName:name,range:`B1:${end}8`,scale:1,format:'png'});
    await fs.writeFile(path.join(work,`${name}.png`),new Uint8Array(await png.arrayBuffer()));
    console.log((await additions.inspect({kind:'region',sheetId:name,range:`B2:${end}5`,maxChars:1700})).ndjson);
  }
  await (await SpreadsheetFile.exportXlsx(additions)).save(path.join(work,'additions.xlsx'));
}
