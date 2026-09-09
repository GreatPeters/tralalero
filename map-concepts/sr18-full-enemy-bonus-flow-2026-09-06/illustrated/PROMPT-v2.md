# 전체 배치 일러스트 — 경로 보정 프롬프트

제작: 기본 제공 이미지 생성 도구. 첫 후보는 시작점을 맵 아래쪽으로 옮기고 일부 동선을 추가하여 원본 경로와 달랐다. 이 수정은 경로 도면을 최우선 참조로 고정하고, 17개 경로점을 명시한다. 생성 이미지는 여전히 정확한 배치도·씬 캡처가 아니라 검토용 컨셉이다.

## 프롬프트

Edit task: correct ROAD GEOMETRY and label anchors, retaining the attractive illustrated game-art treatment.
Image 1 = authoritative route blueprint, EVERY path segment and its topology must follow this.
Image 2 = previous illustrated candidate, use ONLY its visual treatment (wooden planks, blue-white fish stalls, small human figures, gold circular BonusWall discs, water). Its route layout and label anchors are WRONG: do NOT preserve them.
Make a new clear ORTHOGRAPHIC TOP-DOWN full-stage illustrated placement map based exactly on Image1, no perspective tilt, no rotated map. NOT a chart, table, or flowchart; actual illustrated timber roads with characters and golden disc objects on them.
CRITICAL geometry: the start is a SHORT INWARD-FACING DEAD-END STUB in the LOWER-MIDDLE-LEFT of the map, ABOVE the bottom of the large loop. It is NOT at the bottom edge and NOT a little bottom platform. The full single continuous route is a polyline with these exact schematic drawing coordinates. Uniformly scale and translate ALL points together into your canvas (never independently move them). Coordinates are drawing pixels, x rightward, y downward, no need to print coordinates:
START (430,1146) -> (362,1146) -> (362,942) -> (746,942) -> (746,826) -> (619,826) -> (619,1588) -> (255,1588) -> (255,1076) -> (810,1076) -> (810,820) -> (1002,820) -> (1002,543) -> (1216,543) -> (1216,670) -> (852,670) -> FINISH (852,286).
This means:
1 short stub going LEFT, then UP, then RIGHT, then UP, then LEFT;
2 VERY LONG DOWN side of big loop, LEFT along bottom, UP along big loop left edge;
3 first bridge RIGHT across the two vertical roads at y1076; then UP, RIGHT, UP;
4 small upper-right loop RIGHT across its top, DOWN its right edge;
5 second bridge LEFT along y670 crossing x1002; then UP final straight to finish.
DO NOT ADD any other paths, branches, square islands, lower loops, or left-side connectors. Do not fill the inside of any loop with pavement. Both crossings on the first bridge and crossing on the second bridge must read as OVERPASSES, NOT turnable branch crossroads. Bridge surface remains BROWN TIMBER like the rest, not futuristic cyan metal. Add discrete white travel arrows in the above exact order, especially DOWN on the large loop's right side and LEFT on its bottom side.
Enemy and fixed golden BonusWall placements match the red dots and green diamonds in Image1. Each red dot becomes a small group of 1–4 existing-looking human enemy figures with faint red floor rings, not dots, no printed IDs. Large stocky elite figures appear intermittently in the middle/later combat groups. One female boss at the last red dot on the final straight. The LAST golden disc on the final straight is AFTER the female boss, and finish is AFTER that disc at the actual top endpoint. Each green diamond becomes a single or paired ornate upright golden circular magic disc, not a coin lying flat or a chest. Keep total fixed bonus LOCATIONS at nine. Do not print E/F codes. Shark in blue shoes at the exact start stub (430,1146).
Blue-and-white striped fish-market stalls line road edges sparingly enough that the road and enemies remain readable. Calm pale blue water, visual clarity, no long paragraphs. Keep whole route uncropped. Strong silhouettes. Let route dominate canvas. Only text: title "노량진 전체 배치", label "시작" with leader to exact start stub, label "보스" pointing to female boss, label "출구" pointing to exact final endpoint after golden disc, small footer "배치 컨셉 · 씬 미적용". Remove ALL other callout labels and ALL panel-style legends from Image2. No fabricated game UI. This is one clean game-level illustration, not a flowchart.
