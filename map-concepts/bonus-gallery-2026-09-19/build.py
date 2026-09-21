from pathlib import Path
import json, html, shutil

root = Path(__file__).resolve().parents[2]
out = root / "tmp/image-previews/bonus-gallery-2026-09-19"
out.mkdir(parents=True, exist_ok=True)
groups = [
    ("context", "bonus-context-revision-2026-09-19", "맥락 수정", "boards"),
    ("practical", "bonus-practical-altars-2026-09-19", "프리팹 활용", "concepts"),
    ("sacred", "bonus-sacred-altars-2026-09-19", "신성한 제단", "concepts"),
    ("original", "bonus-concepts-2026-09-19", "초기 이펙트", "concepts"),
]
figures = []
for key, folder, label, field in groups:
    data = json.loads((root / "map-concepts" / folder / "prompts.json").read_text(encoding="utf-8-sig"))
    for entry in data[field]:
        name = entry["file"]
        source = root / "tmp/image-previews" / folder / name
        dest = out / "images" / key / name
        dest.parent.mkdir(parents=True, exist_ok=True)
        if not dest.exists():
            shutil.copy2(source, dest)
        title = html.escape(entry["id"] + " · " + entry["name"])
        relative = "images/" + key + "/" + name
        figures.append(f'<figure data-group="{key}"><button class="preview" aria-label="{title} 확대"><img src="{relative}" alt="{title}" loading="lazy" width="1536" height="1024"></button><figcaption><div><strong>{title}</strong><small>{label}</small></div><a href="{relative}" target="_blank" rel="noopener">원본 PNG 열기 ↗</a></figcaption></figure>')
template = Path(__file__).with_name("template.html").read_text(encoding="utf-8")
(out / "index.html").write_text(template.replace("__FIGURES__", "\n".join(figures)), encoding="utf-8")
print(f"Gallery: {out / 'index.html'}; {len(figures)} images")

