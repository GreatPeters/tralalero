import json
import pathlib
import re
import sys

root = pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
out = pathlib.Path(sys.argv[1]) / 'shared-regressions'
before = json.loads((root / 'outputs/chapters45-2026-10-02/qa/full-editmode-1124.json').read_text(encoding='utf-8-sig'))
after = json.loads((out / 'full-editmode-final.json').read_text(encoding='utf-8-sig'))
cohort = set('PlayerCharacterDefaultsTests PlayerDamageNotificationTests PlayerHealingLifecycleTests CombatFeedbackRevisionTests LateralUpgradeTests NoryangjinSlopeTests NoryangjinTurnSpotTests RoadChapterPatternTests RestStopEncounterLaneTests ProjectilePoolLifetimeTests MissileDurationTests HighwayProjectilePathTests CombatRouteFeedbackTests Sr18CombatPolishTests Sr18PresentationRevisionTests EnemyEventControllerTests EnemyThrowDirectionTests MobileCombatFeedbackTests HighwayEncounterLanesTests NoryangjinGameplayIntegrationTests NoryangjinRuntimeCleanupContractTests NoryangjinRunBalanceTests ChapterWorkshopTests ChapterUpgradeRosterTests ChapterEquipmentRevisionTests RunProgressRewardTests'.split())

def signature(row):
    return [row['FullName'], re.sub(r'\s+', ' ', row.get('Message') or '').strip()]

old = {r['FullName']: r for r in before['results'] if r['Status'] == 'Failed'}
new = {r['FullName']: r for r in after['results'] if r['Status'] == 'Failed'}
shared = [r for r in after['results'] if r['FullName'].split('.')[0] in cohort]
chapter = [r for r in after['results'] if r['FullName'].startswith('Chapter45')]
record = {
    'beforeSummary': before['summary'], 'afterSummary': after['summary'],
    'newFailureNames': sorted(new.keys() - old.keys()),
    'resolvedFailureNames': sorted(old.keys() - new.keys()),
    'sameFailureSignatures': [name for name in sorted(old.keys() & new.keys()) if signature(old[name]) == signature(new[name])],
    'changedFailureMessages': [{'name': name, 'before': old[name].get('Message'), 'after': new[name].get('Message')} for name in sorted(old.keys() & new.keys()) if signature(old[name]) != signature(new[name])],
    'sharedRuntimeCohort': {'classes': sorted(cohort), 'total': len(shared), 'passed': sum(r['Status'] == 'Passed' for r in shared), 'failed': sum(r['Status'] == 'Failed' for r in shared), 'failures': [signature(r) for r in shared if r['Status'] == 'Failed']},
    'chapter45': {'total': len(chapter), 'passed': sum(r['Status'] == 'Passed' for r in chapter), 'failed': sum(r['Status'] == 'Failed' for r in chapter), 'names': [r['FullName'] for r in chapter]},
    'limits': 'Exact signature comparison establishes recurrence, not identical root cause or absence of untested defects. Existing failures stay visible.'
}
(out / 'failure-comparison.json').write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: v for k, v in record.items() if k not in ['sameFailureSignatures', 'sharedRuntimeCohort', 'chapter45']}, ensure_ascii=False))
print(json.dumps({'sameSignatures': len(record['sameFailureSignatures']), 'shared': {k: record['sharedRuntimeCohort'][k] for k in ['total', 'passed', 'failed']}, 'chapter45': {k: record['chapter45'][k] for k in ['total', 'passed', 'failed']}}))
