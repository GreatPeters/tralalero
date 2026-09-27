// Full-run storyboards precede the existing detailed proposal; no game behavior is changed.
let flowView = 'sheet';
function renderFullFlow() {
  const c = plans[active - 1], art = FLOW_ART.find(a => a.id === active);
  const host = document.getElementById('full-flow');
  host.innerHTML = `<div class="flow-heading"><div><span class="eyebrow">FULL RUN / 시작 → 사건 → 선택 → 합류 → 마무리</span><h2>0${c.id}. ${E(c.title)} · 전체 흐름</h2><p>한 장의 그림으로 보거나, 장면별로 크게 볼 수 있습니다.</p></div><div class="flow-links"><a href="${art.src}" target="_blank" rel="noopener">그림 크게 열기 ↗</a><a href="${art.src}" download>이 흐름도 저장 ↓</a><a href="고속도로-전체흐름-그림5장.zip" download>5장 모두 받기 ↓</a></div></div><div class="flow-switches"><button data-flow-view="sheet" aria-pressed="${flowView === 'sheet'}">전체 흐름 한 장</button><button data-flow-view="scenes" aria-pressed="${flowView === 'scenes'}">6장면 크게 보기</button><small>모든 그림은 미적용 기획도입니다 · 실제 맵 거리나 게임 화면이 아닙니다</small></div><figure class="flow-sheet" ${flowView === 'sheet' ? '' : 'hidden'}><a href="${art.src}" target="_blank" rel="noopener"><img src="${art.src}" alt="${E(c.title)}의 시작부터 300초 종료까지 6장면과 좌우 분기, 보상, 합류를 연결한 전체 흐름도"></a><figcaption>위쪽 1 → 2 → 3, 아래쪽 4 → 5 → 6 순서입니다. 하단 분기 그림에서 좌우 선택의 차이를 봅니다.</figcaption></figure><div class="flow-scene-mode" ${flowView === 'scenes' ? '' : 'hidden'}><div class="flow-scenes">${art.scenes.map((src, i) => `<figure><div class="flow-scene-title"><b><i>${i + 1}</i>${E(c.timeline[i][0])}</b><span>다음 장면 ${i < 5 ? '→' : '✓'}</span></div><img src="${src}" alt="${E(art.captions[i][0])}"><figcaption><strong>${E(art.captions[i][0])}</strong><p>${E(art.captions[i][1])}</p></figcaption></figure>`).join('')}</div><img class="flow-branches" src="${art.branches}" alt="${E(c.title)}의 좌우 경로 보상과 합류 연결도"></div>`;
  host.querySelectorAll('[data-flow-view]').forEach(button => button.onclick = () => {
    flowView = button.dataset.flowView;
    host.querySelector('.flow-sheet').hidden = flowView !== 'sheet';
    host.querySelector('.flow-scene-mode').hidden = flowView !== 'scenes';
    host.querySelectorAll('[data-flow-view]').forEach(b => b.setAttribute('aria-pressed', b.dataset.flowView === flowView));
  });
}
