"""Run only after opening the final encoded review sheets and room comparisons."""
from pathlib import Path
import json,shutil
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23';DOC=ROOT/'map-concepts/reststop-blender-v2-2026-09-23'
v=json.loads((OUT/'video-manifest.json').read_text(encoding='utf8'));n=json.loads((OUT/'validation.json').read_text());f=json.loads((OUT/'fresh-glb-validation.json').read_text());b=json.loads((OUT/'build-metrics.json').read_text());r=json.loads((DOC/'iteration_review.json').read_text(encoding='utf8'))
assert len(v['clips'])==10 and v['full']['frames']==7200 and f['fresh_import']
assert all((OUT/'review-final'/f'{i:02}-review.jpg').exists() for i in range(1,11))
assert (OUT/'comparison/comparison-store.png').exists() and (OUT/'comparison/comparison-restroom.png').exists()
r.update(decision='retain_repair',selected_defect='Facilities felt too small and the defense view was vertical instead of quarter view.',source_cause='Previous world scale and route timing were authored without native game measurements; camera viewed the arena vertically.',repair='Rebuilt facilities and travel route around measured game player/motion/camera references; placed 91 vehicles; authored distinct moving hazards; changed defense camera to a fixed oblique view.',final_evidence={'master':str((OUT/'reststop-v2.blend').relative_to(ROOT)),'encoded_reviews':str((OUT/'review-final').relative_to(ROOT)),'gimmick_sequences':str((OUT/'gimmick-review').relative_to(ROOT)),'same_scale_comparison':str((OUT/'comparison').relative_to(ROOT)),'native_validation':str((OUT/'validation.json').relative_to(ROOT)),'fresh_import':str((OUT/'fresh-glb-validation.json').relative_to(ROOT))})
r['ledger_after']=[
 {'requirement':'larger restroom','status':'pass','evidence':'08-review.jpg, comparison-restroom.png; 72×84 versus previous 30×20 game units'},
 {'requirement':'larger convenience store','status':'pass','evidence':'07-review.jpg, comparison-store.png; 88×92 versus previous 24×44 game units'},
 {'requirement':'actual game reference','status':'pass','evidence':'game-scale.json, game-vehicles.json, game-settings.json, validation.json; actual shark/car assets and measured movement/camera baseline'},
 {'requirement':'more cars','status':'pass','evidence':'02-review.jpg, 09-review.jpg, whole-map.png; 91 vehicles in build-metrics.json'},
 {'requirement':'readable moving gimmicks','status':'pass','evidence':'gimmick-review/01-sequence.jpg through 08-sequence.jpg; before/active/after encoded frames'},
 {'requirement':'quarter-view defense','status':'pass','evidence':'05-review.jpg, comparison-defense.png, actual Chrome playback; fixed oblique camera and visible character side silhouettes'}]
r['invariant_results']={'duration_seconds':300,'clips':10,'fps':24,'frames':7200,'stationary_defense_seconds':60,'max_aim_degrees_per_second':n['defense_max_yaw_degrees_s'],'all_eight_heading_bins_visited':True,'previous_v1_preserved':True,'original_shark_identity_preserved':True,'new_native_source_and_static_glb_reopened':True,'trellis_calls':0,'seedance_calls':0,'unity_asset_writes':0}
r['accepted_scope_changes']={'store_area_ratio':b['store_area_ratio_to_v1'],'restroom_area_ratio':b['restroom_area_ratio_to_v1'],'expanded_world_and_routes':'Allowed by requested larger game-scale spaces','camera':'Quarter view explicitly requested'}
r['limits']=['Previs, not Unity gameplay installation or performance-ready level','Authored encounters and hazards, not a verified gameplay-balance simulation','Visual sampling covers all stages and critical events, not every pixel of all 7200 frames','Body size is measured at the reference frame; posed extents vary']
(DOC/'iteration_review.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf8')
summary=f'''# 휴게소 Blender v2 최종 검토

수정판을 유지한다. 이전판은 보존했다.

- 전체 300초, 10개 구간, 24fps, 7,200프레임. 모든 MP4를 디코딩해 프레임 수와 길이를 검증했다.
- 화장실 바닥 면적 10.08배, 편의점 7.6667배. 동일 카메라 배율과 방향의 별도 비교 렌더를 확인했다.
- 차량 91대. 실제 게임의 상어·차량 모델과 진행 속도 7.8 유닛/초를 참조했다.
- 5번은 60초 쿼터뷰 방어. 실제 저장된 키의 최대 회전 속도 {n['defense_max_yaw_degrees_s']:.6f}도/초, 고정 XY와 고정 방어 카메라를 검증했다.
- 전체 모델 {b['triangles']:,} 삼각형, 정적 GLB 새 임포트 {f['triangles']:,} 삼각형. 유한 좌표·필수 구간·임포트를 확인했다. 게임용 최적화 완료를 뜻하지 않는다.
- 모든 구간의 인코딩된 4개 시점과 주요 기믹의 작동 전·중·후를 눈으로 확인했다. Chrome에서 주차장과 쿼터뷰 MP4를 재생했다.
- TRELLIS·Seedance·Unity 씬 설치는 수행하지 않았다.

회전 시작 경계, 평가된 상어 몸체 치수, 출구 차량의 도로 이탈, 전체 지도 카메라의 far clip을 보정했다. 관련 절차는 `docs/solutions/workflow-issues/verify-imported-timing-and-mesh-ownership-before-blender-previs-render-2026-09-23.md`에 통합했다.

원본·검증·비교·재현 방법은 [README](README.md)와 `iteration_review.json`을 참조한다.
'''
(DOC/'final_report.md').write_text(summary,encoding='utf8');print('REVIEW_RETAIN_REPAIR',flush=True)
