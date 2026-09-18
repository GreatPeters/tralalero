from pathlib import Path
import json,re
root=Path(__file__).resolve().parent.parent
folder=root/'tmp/analytics-flow-20260912'
log=(folder/'device-angle-analytics.log').read_text(encoding='utf-8',errors='replace')
starts=[line for line in log.splitlines() if 'Logging event:' in line and 'name=game_round_start,' in line]
ends=[line for line in log.splitlines() if 'Logging event:' in line and 'name=game_round_end,' in line]
assert len(starts)==len(ends)==1
round_id=re.search(r'round_id=([a-f0-9]+)',starts[0]).group(1)
assert round_id in ends[0]
required=['chapter','stage','max_stage','scene_name','game_mode','client_event_time_ms','play_time_ms','coins_earned','outcome','chapter_progress_pct','end_pos_x','end_pos_y','end_pos_z','upgrade_levels','upgrade_flat','upgrade_pct']
assert all(key+'=' in ends[0] for key in required)
upload=log.find('Network upload successful with code, uploadAttempted: 204',log.find(ends[0]))
assert upload>0
report={'date':'2026-09-12','platform':'Android API36 x86_64 emulator, Google Play image','apk':'Builds/Android/AnalyticsSmoke-20260912.apk','debugMode':True,'startEvents':len(starts),'endEvents':len(ends),'roundId':round_id,'playTimeMs':38133,'outcome':'death','chapter':1,'startStage':1,'endStage':3,'maxStage':16,'coinsEarned':0,'chapterProgressPercent':13.333333333333334,'allRequiredParametersPresent':True,'serverUploadHttpStatus':204,'firebaseDebugViewVerified':True,'debugViewPlayTimeMs':38133,'debugViewRoundIdMatches':True,'newBigQueryRowVerified':False,'reason':'Firebase daily export and the sheet reporting window through UTC yesterday have not yet included this new run.','renderingLimitation':'Emulator reported MSAA/FlatKit capability errors and black world rendering; UI and actual gameplay/physics emitted the observed start/death events. This is a logging test, not a device rendering/performance certification.','cleanup':'Stopped only task-owned read-only emulator; original AVD data unchanged. Build finally restored ARM64 and custom signing.'}
out=root/'map-concepts/analytics-smoke-2026-09-12';out.mkdir(exist_ok=True)
(out/'result.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
