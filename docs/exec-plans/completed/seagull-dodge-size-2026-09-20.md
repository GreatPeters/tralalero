# Small, avoidable seagull landing footprint

Status: complete.

The user requested a landing shadow about the size of the small circles marked in the screenshot. The oversized scene override made it 13.824m wide, while the animated bird itself spanned roughly 5m.

Changed warning diameter to 0.6–1.6m and rig scale to 1.4 with matching inherited contact shapes. Applied through native Unity APIs to the source prefab and SR18 placement; removed the generator's old oversized override. Kept the previously requested spin and percentage penalty.

Verification: 11 relevant tests passed, both C# builds passed, and four native physical cases verified contact, left/right avoidance and landed contact. An unrelated stale road-count assertion failed equally against the before/final serialized count; see the evidence record. Full details, backups, limits and videos: `map-concepts/seagull-dodge-size-2026-09-20/README.md`.
