import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const req=createRequire(path.resolve('tmp/sr18-data-work-2026-09-07/loader.cjs'));
const {SpreadsheetFile}=await import(pathToFileURL(req.resolve('@oai/artifact-tool')).href);
const wb=await SpreadsheetFile.importXlsx(new Uint8Array(await fs.readFile('Assets/ShooterSurvival/GameData/Editor/Data.xlsx')));
for(const name of ['몬스터 성장','몬스터','적 배치']) console.log(name,JSON.stringify(wb.worksheets.getItem(name).getUsedRange().values));
