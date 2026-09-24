"""Run relevant native EditMode suites after the campaign runner has restored the user save."""
import json
import argparse
from pathlib import Path
import runpy
import time

command=runpy.run_path(str(Path(__file__).with_name('run-campaign-balance.py')))['command']
state=command('eval',code='return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;')
if state.get('result'):raise RuntimeError('Stop the campaign first; do not interrupt a live attempt')
suites=['PlayerDamageNotificationTests','PlayerHealingLifecycleTests','CombatPresentationPoolTests',
        'HighwayProjectilePathTests','CampaignHazardTimingTests','RestStopContactPairTests',
        'RestStopEncounterLaneTests',
        'HighwayRebuildContractTests','HighwayEncounterLanesTests','EnemyEventControllerTests',
        'EnemyThrowDirectionTests','CombatRouteFeedbackTests','MobileCombatFeedbackTests',
        'FatManTwoHandPoseTests','ReviewReadabilityTests','NoryangjinCameraOcclusionTests',
        'BonusDisplayedAmountTests','BonusAltarRulesTests','BonusTalismanTests',
        'CombatFeedbackRevisionTests','SeagullContactTests','NoryangjinRunBalanceTests','RoadChapterPatternTests']
parser=argparse.ArgumentParser();parser.add_argument('--output',default='tmp/campaign-balance-2026-09-23/regression-final.json');parser.add_argument('--data',action='store_true');args=parser.parse_args()
if args.data:suites+=['GameDataWorkbookTests','EncounterPlacementTablesTests','WorkbookEnemyAssignmentTests']
output=Path(args.output)
results=[]
for suite in suites:
    result=command('run_tests',mode='editor',filter=suite,async_tests=True)
    deadline=time.monotonic()+180
    while result.get('result')=='running' or result.get('status')=='running':
        if time.monotonic()>deadline:raise TimeoutError('Test suite did not finish: '+suite)
        time.sleep(1)
        result=command('test_status')
        if isinstance(result,str):result=json.loads(result)
    results.append({'suite':suite,'result':result})
    output.write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    summary=result.get('summary',{})
    if result.get('status')!='completed' or summary.get('total',0)==0 or summary.get('failed',0)>0:
        raise RuntimeError('Review test result: '+suite)
    print(suite,summary['passed'],'/',summary['total'],flush=True)
print('TOTAL',sum(r['result']['summary']['passed'] for r in results),flush=True)
