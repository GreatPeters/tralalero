"""Present verified native evidence after the earned campaign reaches chapter3."""
import argparse
import hashlib
import html
import json
from pathlib import Path
import shutil
import subprocess

parser=argparse.ArgumentParser();parser.add_argument('cohort');parser.add_argument('--preview',action='store_true')
parser.add_argument('--balance-only',action='store_true');parser.add_argument('--gallery-name',default='campaign-balance');parser.add_argument('--state-folder',default='tmp/campaign-balance-2026-09-23');args=parser.parse_args()
root=Path(__file__).resolve().parents[1];source=root/'tmp/image-previews/campaign-balance-2026-09-23'
cohort=Path(args.cohort);report=json.loads((cohort/'measured-summary.json').read_text(encoding='utf-8'))
complete=len(report['chapters'])==3 and all(c['clearRun'] is not None for c in report['chapters'])
if not complete and not args.preview:raise RuntimeError('All three chapter clears must be measured first')
gallery=root/'tmp/image-previews/bonus-gallery-2026-09-19'/args.gallery_name;gallery.mkdir(parents=True,exist_ok=True)
ffmpeg=Path.home()/'AppData/Roaming/Python/Python312/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
def copy(path):
    path=Path(path);name=hashlib.sha256(path.read_bytes()).hexdigest()[:10]+'-'+path.name;target=gallery/name
    if not target.exists():shutil.copy2(path,target)
    return name
def video(folder):
    target=folder/'native-motion.mp4'
    if not target.exists():subprocess.run([str(ffmpeg),'-hide_banner','-loglevel','error','-framerate','30','-i',str(folder/'frame-%04d.jpg'),'-c:v','libx264','-threads','4','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(target)],check=True)
    return copy(target)
parts=['''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>피격 표시와 3챕터 성장 검증</title><style>
body{margin:0;background:#111d29;color:#f1f4f6;font:16px/1.65 system-ui,sans-serif}main{max-width:1180px;margin:auto;padding:34px}h1{font-size:34px;line-height:1.35}h2{font-size:24px}p{color:#bccbd7}a{color:#88d8ff}section{padding:26px 0;border-top:1px solid #344857}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:20px}article{background:#203342;border-radius:8px;padding:16px}img,video{max-width:100%;height:auto;border-radius:6px}.portrait{max-height:720px}.proof{display:grid;grid-template-columns:350px 1fr;gap:30px;align-items:start}table{border-collapse:collapse;width:100%}th,td{text-align:left;padding:12px;border-bottom:1px solid #3b5163}.note{font-size:14px;color:#a0b4c3}nav{display:flex;gap:22px;flex-wrap:wrap}@media(max-width:750px){main{padding:20px}.grid,.proof{grid-template-columns:1fr}h1{font-size:27px}.proof img{max-width:360px}table{font-size:13px}th,td{padding:8px}}</style><main><h1>피격 표시와 3챕터 성장 검증</h1><p>실제 게임의 코인을 모아 강화를 구매하고, 성장 상태를 이어서 세 챕터를 완주했습니다.</p><nav><a href="#balance">성장 기록</a><a href="#damage">피격 표시</a><a href="#motion">휴게소 동작</a><a href="#play">실제 진행</a></nav>''']
parts.append('<section id="balance"><h2>챕터별 실제 결과</h2><table><tr><th>챕터</th><th>해당 챕터 도전</th><th>첫 도전</th><th>완주 시간</th><th>강화 구매 방문</th></tr>')
names={1:'노량진',2:'고속도로',3:'휴게소'}
if not complete:parts[0]=parts[0].replace('실제 게임의 코인을 모아 강화를 구매하고, 성장 상태를 이어서 세 챕터를 완주했습니다.','피격 표시와 동작 수정을 적용했습니다. 1·2챕터 완주를 확인했고, 3챕터 성장 검증은 진행 중입니다.')
for c in report['chapters']:
    clear=f'{c["clearSeconds"]:.1f}초' if c['clearSeconds'] is not None else '검증 중'
    parts.append(f'<tr><td>{c["chapter"]}. {names[c["chapter"]]}</td><td>{c["attempts"]}판</td><td>{c["firstSeconds"]:.1f}초</td><td>{clear}</td><td>{c["purchaseVisits"]}회</td></tr>')
restored='원래 사용자 저장 상태를 복원했습니다.' if (root/args.state_folder/'final-state.json').exists() else '원래 사용자 저장 상태는 최종 검증 후 복원합니다.'
parts.append(f'</table><p class="note">자동 조작·고정 30 게임 프레임 시뮬레이션 기준입니다. 체력·공격력 보정, 무료 코인, 광고 보상 없이 실제 구매를 사용했습니다. 보너스와 조작에 따라 개별 도전 시간은 달라집니다. {restored}</p><p><a href="{copy(cohort/"measured-runs.csv")}">전체 도전·구매·코인 기록 CSV</a></p>')
if complete:
    growth=' / '.join(f'{c["endpointSecondsPerPurchaseVisit"]:.1f}초' for c in report['chapters'])
    if args.balance_only:parts.append('<p>이전 고속도로 19판·휴게소 7판에서 각각 약 30판을 목표로 조정했습니다. 앞 챕터의 실제 성장 상태를 이어받고, 적 체력·공격력과 코인 보상으로 난이도를 맞췄습니다. 도전 횟수로 승리를 막는 제한은 없습니다.</p><div class="grid">')
    else:parts.append(f'<p>첫 도전부터 완주까지 늘어난 시간을, 첫 도전 이후 강화 구매 방문 수로 나누면 챕터별 {growth}입니다. 한 번의 방문에서 여러 강화를 구매할 수 있으며, 강화 한 번마다 같은 시간이 늘어난다는 뜻은 아닙니다.</p><div class="grid">')
    replay_path=source/'clear-replays/results.json'
    replays=json.loads(replay_path.read_text(encoding='utf-8')) if replay_path.exists() else []
    for c in report['chapters']:
        replay=next((r for r in replays if r['chapter']==c['chapter']),None)
        if args.balance_only and c['chapter']>1:
            result=sorted((cohort/f'run-{c["clearRun"]:02}').glob('*-result.png'))[-1]
            note='위 표의 실제 첫 클리어 화면입니다.'
        elif replay:result=root/replay['result'];note='실제 클리어했던 구매 상태·시드로 다시 완주하고, 승리 화면까지 기다린 재검증입니다.'
        else:continue
        parts.append(f'<article><h3>{names[c["chapter"]]} 클리어</h3><a href="{copy(result)}"><img src="{copy(result)}" alt="{names[c["chapter"]]} 실제 승리 화면"></a><p class="note">{note}</p></article>')
    parts.append('</div>')
    pairs_path=source/'paired-upgrade-checks/results.json'
    if pairs_path.exists() and not args.balance_only:
        pairs=json.loads(pairs_path.read_text(encoding='utf-8'))
        gains=' / '.join(f'{p["secondsAdded"]:.1f}초' for p in pairs)
        parts.append(f'<p class="note">챕터마다 한 성장 지점에서 같은 보너스 시드로 구매 전후를 비교한 추가 검사에서는 증가량이 {gains}였습니다. 같은 적 무리에서 끝나는 구간과 한 번에 여러 구간을 넘는 편차가 남습니다. 10~15초는 성장 목표이며, 개별 강화 효과를 보장하는 수치는 아닙니다.</p>')
parts.append('</section>')
if args.balance_only:
    parts[0]=parts[0].replace('피격 표시와 3챕터 성장 검증','고속도로·휴게소 30판 성장 검증')
    start=parts[0].index('<nav>');end=parts[0].index('</nav>')+len('</nav>');parts[0]=parts[0][:start]+parts[0][end:]
    parts.append('</main></html>');(gallery/'index.html').write_text(''.join(parts),encoding='utf-8');print(gallery/'index.html');raise SystemExit(0)
damage=source/'lifecycle-burst-heal-hitch-safe/combined-damage.png'
parts.append(f'<section id="damage"><h2>체력을 잃었을 때 바로 읽을 수 있게</h2><div class="proof"><img class="portrait" src="{copy(damage)}" alt="연속 피해 125, 남은 체력 375"><div><p>상어 위에는 한 개의 선명한 피해 숫자, 하단에는 잃은 체력과 원인이 표시됩니다. 연속 피해는 합산하고, 프레임이 끊겨도 새 표시가 첫 화면 전에 사라지지 않게 했습니다.</p><p>적 접촉·탄환·투척물·도로 장애물·갈매기·구멍 등 공통 피해 경로에 적용했습니다. 사망 후 회복으로 되살아나거나 동시에 완주 처리되는 상태도 차단했습니다.</p><p class="note">사진은 피해 100+25를 발생시킨 표시 검사입니다. 별도 실제 도로 장애물 충돌에서는 500→0과 한 번의 알림을 확인했습니다.</p></div></div></section>')
parts.append('<section id="motion"><h2>휴게소 캐릭터 동작과 손에 든 도구</h2><p>기존 캐릭터 외형에 앞을 향한 공격, 피격 반응과 사망 동작을 적용했습니다. 컵은 손의 최종 자세에서 발사됩니다.</p><div class="grid">')
for role,label,rev in [('ParkingMarshal','주차 요원','v1'),('CoffeeVendor','커피 판매원','v2'),('SnackChef','요리사','v2')]:
    folder=source/f'showcase-native-{rev}'/role
    parts.append(f'<article><h3>{label}</h3><video controls muted loop playsinline preload="metadata" poster="{copy(folder/"idle.png")}"><source src="{video(folder)}" type="video/mp4"></video><p class="note">실제 Unity 프리팹의 동작 확인용 고정 카메라</p></article>')
parts.append('</div></section><section id="play"><h2>실제 진행에서 확인한 장면</h2><div class="grid">')
approved_segments=['고속도로-커브사격','휴게소-코너안내','휴게소-식당방어전','휴게소-마지막전투']
for folder in [source/'segments'/name for name in approved_segments]:
    if not (folder/'capture.txt').exists():continue
    frames=sorted(folder.glob('frame-*.jpg'))
    poster=frames[min(350,len(frames)-1)] if folder.name=='휴게소-코너안내' else frames[0]
    parts.append(f'<article><h3>{html.escape(folder.name)}</h3><video controls muted playsinline preload="metadata" poster="{copy(poster)}"><source src="{video(folder)}" type="video/mp4"></video><p class="note">성장 검증 도중 촬영한 실제 진행 장면</p></article>')
parts.append('</div></section></main></html>')
(gallery/'index.html').write_text(''.join(parts),encoding='utf-8');print(gallery/'index.html')
