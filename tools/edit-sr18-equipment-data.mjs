import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const work='tmp/sr18-presentation-data-20260910';
const require=createRequire(path.resolve(`${work}/loader.cjs`));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(require.resolve('@oai/artifact-tool')).href);
const source='Assets/ShooterSurvival/GameData/Editor/Data.xlsx';
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(source));
await fs.mkdir(work,{recursive:true});
const sheet=wb.worksheets.getItem('커스터마이징');
if(!process.argv.includes('--apply')){
  const image=await wb.render({sheetName:'커스터마이징',range:'B1:I14',scale:1,format:'png'});
  await fs.writeFile(`${work}/equipment-before.png`,new Uint8Array(await image.arrayBuffer()));
  console.log('Before view saved');process.exit(0);
}
const bytes=await fs.readFile(source);
await fs.writeFile(`${work}/equipment-before.xlsx`,bytes,{flag:'wx'});
const items=[
 ['skin_original','Skin','저주받은 상어',0,1,'훔친 신발이 남긴 푸른 저주의 흔적','',0,'Percent'],
 ['skin_coral','Skin','산호 부적',25,0,'피부에 스민 붉은 산호의 생명력','HP',5,'Percent'],
 ['skin_ice','Skin','냉각 수지',35,0,'밀수 냉각제로 물탄의 흐름을 안정시킨다','ATT_SPEED',5,'Percent'],
 ['skin_sand','Skin','황동 비늘',45,0,'개조업자가 덧댄 황동빛 보호막','HP',8,'Percent'],
 ['skin_armor','Skin','철갑 밀수복',70,0,'등과 어깨를 감싸는 철판 장갑','HP',12,'Percent'],
 ['skin_raider','Skin','해적의 낙인',60,0,'붉은 가죽띠와 흉터로 새긴 약탈자의 표식','ATT',8,'Percent'],
 ['skin_relic','Skin','봉인된 공물',90,0,'금속 봉인과 푸른 보석에 남은 공물의 힘','BOSS_DAMAGE',12,'Percent'],
 ['skin_diver','Skin','심해 잠수복',65,0,'공기통과 황동 배관으로 회복을 돕는다','HP_REGEN',.15,'Value'],
 ['shoes_original','Shoes','벗겨지지 않는 신발',0,1,'저주로 발에 묶인 세 짝의 파란 신발','',0,'Percent'],
 ['shoes_ruby','Shoes','열처리 덧창',20,0,'원래 신발 위에 덧댄 붉은 강화창','ATT',4,'Percent'],
 ['shoes_mint','Shoes','냉각 덧창',30,0,'열을 빼는 민트빛 탄성 덧창','ATT_SPEED',4,'Percent'],
 ['shoes_gold','Shoes','금박 덧창',40,0,'공물을 끌어당기는 금빛 가공','COIN_BONUS',6,'Percent'],
 ['shoes_steel','Shoes','강철 앞코',60,0,'벗길 수 없는 신발에 볼트로 고정한 앞코','ATT',8,'Percent'],
 ['shoes_spring','Shoes','태엽 가속창',75,0,'노출된 황동 태엽이 사격 박자를 당긴다','ATT_SPEED',8,'Percent'],
 ['shoes_relic','Shoes','봉인 보석창',90,0,'발등의 보석이 물탄의 힘을 오래 붙든다','PROJECTILE_SPEED',10,'Percent'],
 ['shoes_salvage','Shoes','인양 자석창',65,0,'발목 자석으로 흩어진 전리품을 모은다','COIN_BONUS',10,'Percent'],
 ['hat_none','Hat','머리 장비 없음',0,1,'저주만으로도 충분히 무겁다','',0,'Percent'],
 ['hat_cap','Hat','항구 밀수꾼 캡',25,0,'뒷골목 거래상에게 받은 낡은 캡','COIN_BONUS',4,'Percent'],
 ['hat_bucket','Hat','인양꾼 모자',35,0,'짠 바람을 막는 넓은 챙','HP',5,'Percent'],
 ['hat_tophat','Hat','가짜 귀족 모자',50,0,'신에게 잘 보이려는 허세','BOSS_DAMAGE',8,'Percent'],
 ['hat_goggles','Hat','황동 조준경',65,0,'빛나는 렌즈로 약점을 읽는다','ATT',6,'Percent'],
 ['hat_diver','Hat','심해 잠수모',85,0,'둥근 황동 헬멧과 작은 공기 밸브','HP',10,'Percent'],
 ['hat_pirate','Hat','세갈래 해적모',75,0,'약탈한 공물을 더 오래 붙든다','PROJECTILE_SPEED',8,'Percent'],
 ['hat_relic','Hat','공물 봉인관',100,0,'푸른 보석을 가둔 왕관','BOSS_DAMAGE',12,'Percent'],
];
sheet.getRange('B1').values=[['밀수 장비 · 보석 거래 · 장착한 세 부위의 효과 적용']];
sheet.getRange('B2:L2').values=[['ID','부위','이름','가격타입','가격','비주얼','기본','설명','효과','효과값','효과타입']];
sheet.getRange('B3:L26').values=items.map(([id,slot,name,price,base,desc,stat,value,type])=>[id,slot,name,'Jewel',price,id,base,desc,stat,value,type]);
sheet.getRange('B2:L2').format={fill:'#DDE6ED',font:{name:'Arial',size:11,bold:true},rowHeight:30};
sheet.getRange('B3:L26').format.font={name:'Arial',size:11};
sheet.getRange('B3:L26').format.rowHeight=28;
sheet.getRange('D:D').format.columnWidth=25;sheet.getRange('I:I').format.columnWidth=55;
sheet.getRange('J:L').format.columnWidth=21;sheet.getRange('K3:K26').setNumberFormat('0.##');
sheet.getRange('E3:F26').format.fill='#FFF6DC';sheet.getRange('J3:L26').format.fill='#FFF6DC';
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!',options:{useRegex:true,maxResults:20},maxChars:1500});
console.log(errors.ndjson);
const image=await wb.render({sheetName:'커스터마이징',range:'B1:L10',scale:1.15,format:'png'});
await fs.writeFile(`${work}/equipment-after.png`,new Uint8Array(await image.arrayBuffer()));
await fs.writeFile(`${work}/equipment-changes.json`,JSON.stringify({beforeHash:crypto.createHash('sha256').update(bytes).digest('hex'),sheets:['커스터마이징']},null,2));
await(await SpreadsheetFile.exportXlsx(wb)).save(`${work}/equipment-candidate.xlsx`);
console.log(JSON.stringify({items:items.length,currency:'Jewel',equippedEffects:true}));
