const byId = id => document.getElementById(id);
const escapeText = text => String(text).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));

function showIdea(id) {
  const plan = SIMPLE_PLANS.find(p => p.id === id) || SIMPLE_PLANS[0];
  byId('planNav').querySelectorAll('button').forEach(b => b.setAttribute('aria-pressed', Number(b.dataset.id) === plan.id));
  byId('idea').innerHTML = `
    <header class="idea-title"><small>${plan.id}안 · ${escapeText(plan.name)}</small><h2>${escapeText(plan.headline)}</h2></header>
    <ol class="comic">${plan.steps.map(([title, description], i) => `
      <li><span class="step-number">${i+1} / ${['무슨 일이 생기나요?','나는 어떻게 하나요?','그러면 어떻게 되나요?'][i]}</span>
      <img src="simple/idea-${plan.id}-${i+1}.svg" alt="${escapeText(title)}. ${escapeText(description)}" width="500" height="410">
      <h3>${escapeText(title)}</h3><p>${escapeText(description)}</p></li>`).join('')}
    </ol>
    <p class="fun"><strong>재미있는 점</strong>${escapeText(plan.fun)}</p>
    <section class="run-flow"><h3>한 판은 이렇게 흘러가요</h3><ol>${plan.flow.map(s=>`<li><span>${escapeText(s)}</span></li>`).join('')}</ol></section>
    <section class="choice-example"><h3>${escapeText(plan.choiceTitle)}</h3><p>갈림길 한 번을 예로 들면 이렇습니다.</p><div class="choice-pair">${plan.choices.map((c,i)=>`
      <article class="choice"><img loading="lazy" src="simple/idea-${plan.id}-choice-${i+1}.svg" width="260" height="150" alt="${escapeText(c.name)}">
      <div><small>${escapeText(c.side)}</small><h4>${escapeText(c.name)}</h4><p>${escapeText(c.text)}</p></div></article>`).join('')}</div>
      <p class="merge">어느 쪽이든 다시 큰길로 합류해요</p>
    </section>
    <details><summary>실수하면 어떻게 되나요?</summary><p>${escapeText(plan.mistake)}</p></details>
    <details><summary>기존 모델과 자세한 기획 보기</summary><h4>다시 쓸 수 있는 것</h4><p>${escapeText(plan.reuse)}</p><h4>새로 만들어야 하는 것</h4><p>${escapeText(plan.new)}</p>
      <div class="more-links"><a href="details.html#concept-${plan.id}" target="_blank" rel="noopener">${escapeText(plan.original)} · 상세 기획 ↗</a><a href="flow/plan-${plan.id}.svg" target="_blank" rel="noopener">자세한 전체 흐름 그림 ↗</a><a href="고속도로-개편-5안-상세.md" download>전체 상세 기획 내려받기 ↓</a></div>
    </details>`;
  document.title = `${plan.id}안 ${plan.name} — 고속도로 쉬운 그림 설명`;
  history.replaceState(null, '', `#concept-${plan.id}`);
}

for (const plan of SIMPLE_PLANS) {
  const button = document.createElement('button');
  button.dataset.id = plan.id;
  button.innerHTML = `<b>${plan.id}</b><span>${escapeText(plan.name)}</span>`;
  button.setAttribute('aria-pressed', 'false');
  button.addEventListener('click', () => showIdea(plan.id));
  byId('planNav').append(button);
}

function openFromHash() {
  const match = location.hash.match(/^#concept-([1-5])(?:~5)?$/);
  showIdea(match ? Number(match[1]) : 1);
}
window.addEventListener('hashchange', openFromHash);
openFromHash();
