"""Export lively BGM candidates with the existing measured preview pipeline."""
import html
import importlib.util
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location("processor",ROOT/"tools/prepare-emotional-horror-bgm.py")
processor=importlib.util.module_from_spec(spec);spec.loader.exec_module(processor)
processor.FOLDER=ROOT/"map-concepts/harbor-polish-2026-09-15/audio"
processor.OUT=processor.FOLDER/"listen"
if __name__=="__main__":
    processor.main()
    copies=["통통 튀는 현악과 신비로운 항구 산책","경쾌한 마림바와 사탕빛 동화","탄력 있는 신스와 자정의 달리기","피리와 나무 타악기의 수산시장 소동","태엽 같은 리듬과 장난스러운 추격","비브라폰과 쓸쓸하면서 가벼운 스윙","아코디언과 유령 축제의 활기","빠른 현악 리듬과 바닷길 모험","일렉트릭 피아노와 황혼의 휴게소","서정적인 목관과 운동화 원정대"]
    jobs=json.loads((processor.FOLDER/"raw/bgm-requests.json").read_text(encoding="utf-8"))["jobs"]
    path=processor.OUT/"index.html";page=path.read_text(encoding="utf-8")
    page=page.replace("감성과 공포 · BGM 10곡","항구 모험 · 경쾌한 BGM 10곡").replace("아련한 멜로디와 으스스한 분위기를 섞은 후보입니다.","경쾌한 리듬에 신비롭고 아련한 분위기를 섞은 새 후보입니다.")
    for job,copy in zip(jobs,copies):page=page.replace(html.escape(job["direction"]),copy)
    path.write_text(page,encoding="utf-8")
    rows=["# 경쾌한 항구 BGM 후보 10곡","","[비교 청음 페이지](listen/index.html)","","| 번호 | 제목 | 방향 | MP3 |","|---|---|---|---|"]
    for i,(job,copy) in enumerate(zip(jobs,copies),1):rows.append(f'| {i:02} | {job["title"]} | {copy} | [듣기](listen/{job["name"]}.mp3) |')
    rows.extend(["","각 후보는 로컬 Stable Audio3 Small-Music으로 생성했다. BPM·악기 설명은 제작 지시이며 실제 연주의 전사나 청음 평가가 아니다. WAV/MP3/OGG는 listen에, 원본·프롬프트·seed는 raw에, 측정값은 signal-report.json에 있다. 게임의 현재 BGM 교체는 이 후보 제작 스크립트가 수행하지 않는다."])
    (processor.FOLDER/"README.md").write_text("\n".join(rows)+"\n",encoding="utf-8")
