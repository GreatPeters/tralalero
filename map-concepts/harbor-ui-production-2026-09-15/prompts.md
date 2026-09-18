# Harbor UI production prompts

Built-in `image_gen` used for generation/editing; user-requested cutting, chroma-key extraction and layer assembly use the reproducible local scripts.

## Surface atlas

Use case: compositing and background-extraction. Input is the approved Korean harbor workshop UI concept board, supplied as style/material reference. Create a PRODUCTION sprite atlas of reusable UI surfaces matching exactly its dark aged timber, brass rivets, parchment interiors, luminous gold bevels, restrained high-quality game art. No scenery, no characters, no letters, no numbers, no icons anywhere.
Output square 2048x2048 PNG with a REAL transparent background, NOT white and NOT a painted checkerboard. Exactly 2 columns x 4 rows of invisible 1024x512 cells. Center exactly one distinct UI surface wholly inside each cell. Keep at least 48px transparent padding from every cell edge; no guides or labels. All objects front-on, perfectly level, no perspective tilt and no cast shadows outside the object. Keep border details within a consistent narrow rim so centers and edges can stretch using nine-slicing.
Row1 col1: wide light parchment rounded rectangular upgrade card, dark thin brown outline and restrained worn brass edge, EMPTY flat text area.
Row1 col2: wide dark wooden title sign, brass border and corner bolts, EMPTY central wood planks, no hanging chains protruding.
Row2 col1: wide selected/action button of warm golden parchment with bright brass bevel, EMPTY.
Row2 col2: wide inactive/action button of rich dark brown wood with thin brass bevel, EMPTY.
Row3 col1: a SQUARE dark wood navigation tile, rounded brass corners with four visible bolts, EMPTY.
Row3 col2: a SQUARE compact dark brass back/settings plaque, beveled outline, EMPTY.
Row4 col1: long small wallet pill, cream parchment center with dark brass outline, EMPTY.
Row4 col2: wide decorative screen/window FRAME with thin aged brass and wood border, rounded corners and brass bolts, completely TRANSPARENT EMPTY center.
Ensure full transparency outside objects AND inside the final hollow frame. No text/icons burned in. Material quality and silhouette fidelity to reference are more important than ornament density. Each object must be completely separated for exact cell-based cutting.

## Surface key recovery

Precise background replacement edit of this 8-item UI sprite sheet. Keep EVERY existing UI object, all gold/wood/parchment detail, all positions, every edge and the 2-column by 4-row layout unchanged. Replace ALL gray checkerboard background pixels, including the inside of the hollow lower-right frame, with one perfectly flat uniform saturated chroma magenta RGB(255,0,255), hex #FF00FF. This is an opaque production chroma-key sheet, so DO NOT use transparency or a checkerboard. No shadows/glow on the magenta background and no magenta inside solid objects. Absolutely no other alterations, no text, no new objects. Crisp antialiased edges with minimal background color spill. Complete objects fully visible.

## Upgrade parts

Use case: compositing and background-extraction. Use the CENTER upgrade row illustrations in the supplied approved game UI board as the exact style and subject reference. Create a production icon sprite atlas, NO UI cards, NO buttons, NO text, NO numbers, NO labels.
Canvas landscape 1536x1024, invisible grid 3 equal columns by 2 equal rows. Each 512x512 cell contains one complete isolated object centered with ample empty padding. TRUE alpha transparency outside objects, no painted checkerboard, no opaque background. No object crosses a cell boundary. Rich realistic stylized game-item rendering, warm edge lighting, clear dark brown contours, same tactile detail as the reference:
Top-left: worn riveted steel shoe toe-cap armor.
Top-middle: green padded cushioning insole.
Top-right: black steel compression spring coil.
Bottom-left: blue/silver/red miniature rocket charm with a short contained flame.
Bottom-middle: single blue-and-white sneaker with a small number of blue speed strokes, no brand lettering.
Bottom-right: brown leather coin pouch with several gold coins.
Complete silhouettes. No hands, no scenery, no UI decorations, no letters. Keep all six objects separated and nonoverlapping for clean extraction.

## Parts key recovery

Precise background replacement only. Keep all SIX item illustrations, exact positions, proportions, colors and the 3-column by 2-row layout in the supplied sprite sheet. Replace ALL gray checkerboard and gray background, including enclosed gaps inside rocket cord loops, coin-pouch handle loops and spring gaps, with perfectly flat uniform saturated chroma magenta RGB(255,0,255), #FF00FF. Do not change silver metal, silver sneaker sections or bright highlights on the objects. Only their background becomes magenta. Opaque chroma-key production sheet: DO NOT draw checkerboard or transparency. No shadows on the background. Crisp complete silhouettes, minimal magenta spill, ample separation. No text, no labels, no extra objects.

## Equipment icons

Create a production 3-column by 2-row sprite sheet, landscape 1536x1024, based on the equipment in the RIGHT panel of this game UI reference. Match the exact realistic stylized warm wood-and-brass game-art quality, worn material texture and bold dark outlines. Every object fully isolated on perfectly flat opaque saturated MAGENTA #FF00FF, including holes and spaces BETWEEN a pair of shoes. No checkerboard, no transparency simulation, no cast shadows on background. Each 512x512 invisible cell contains one whole item with padding, no overlap.
Top-left: pair of red classic high-top sneakers with worn silver riveted toe guards, as in reference.
Top-middle: pair of green harbor worker boots with brass rivets and buckles.
Top-right: pair of dark brown steel armored boots with riveted toe caps.
Bottom-left: pair of black pirate sneakers with small skull-and-crossbones badges and brass eyelets.
Bottom-middle: isolated smooth gray shark dorsal fin, a simple equipment-tab icon.
Bottom-right: isolated dark gray baseball cap, a simple equipment-tab icon.
No parchment cards, no buttons, no text, no labels, no numbers, no currency, no borders. Complete distinct silhouettes for exact cutting and background key removal.

## Main UI revision

Revise the supplied approved three-screen Korean Harbor Workshop UI concept (image 1). Image 2 is an ACTUAL UNITY GAME CAMERA CAPTURE, authoritative for the left panel's scenery. Image 3 is the matching extracted UI surfaces and icons, for style consistency.
Keep the wood/brass/cream craft style and three portrait screens side by side. Landscape 1536x1024.
LEFT: Use the actual simple low-poly scene of image 2 as the background, matching its blue-striped fish stalls, wide brown dock path, turquoise water, camera angle, player shark position and its actual low-poly appearance. Do NOT redraw it as a realistic harbor illustration. This must visibly be the real low-poly gameplay scene with separate high-quality 2D HUD overlays on top. Avoid a heavy enclosing decorative frame across the scene. Use slim coin/gem/settings controls at top, a compact wooden "CHAPTER 01 / 노량진 수산시장" sign below. Preserve a clear play path. Float the cream text "좌우로 움직여" / "게임시작" and unified horizontal finger gesture over the actual scene, without an opaque large white instruction panel. Bottom three wood tiles with shoe/sword/scroll icons and labels "꾸미기", "강화", "스토리". Add a narrow bright GREEN health bar with dark rim immediately ABOVE the actual shark. No 3D scenery replacement, no giant fashion-model shark. Do not replace the game's actual shark with the shop shark.
CENTER: Keep the excellent approved vertical one-row-per-upgrade shop and goggled cobbler. Add a clearly visible pair of tabs just below the "신발 개조소" title: selected gold tab "상시 업그레이드", inactive brown tab "챕터 업그레이드". Slightly shorten merchant art to fit. Keep the approved ONE common full-width current-stat panel with heading "현재 능력치" and the horizontal values "공격력 12" and "체력 60" together, since the user selected this version. Below: full-width parchment rows with distinct numbered levels: 강철 앞코 Lv.3/60 공격력+12→+16; 충격 흡수 깔창 Lv.3/60 체력 강화; 스프링 코일 Lv.7/30 공격 속도+35%→+40%; 미사일 장식 Lv.2/30 미사일 지속 시간; 측면 부스터 Lv.3/10 좌우 이동+15%→+20%; show partial next coin row at bottom to imply scrolling. No level dots. Keep part images and coin price buttons.
RIGHT: Preserve warm workshop, shark with TWO legs plus a blue sneaker on tail-tip, skin/shoes/hat tabs and 2x2 shoe cards. Use explicit text for EVERY equipment effect, no effect represented by an icon alone. Red shoes: "클래식 하이탑", "공격력 +15%", state "보유 중", selected check mark. Green boots: "항구 작업화", "체력 +16%", diamond price 400. Steel boots: "강철 부츠", "공격력 +32%", diamond price 600. Black skull shoes: "유물 스니커즈", "코인 획득 +32%", diamond price 900. Right screen's bottom action for the selected OWNED red shoes reads ONLY "장착", with NO diamond icon and NO price. Never combine 구매 and 장착 in one label. Currency icons on price fields remain allowed. No extra annotations or developer copy on this main board.
High-quality finished readable Korean UI concept.

## Main revision correction

Edit this supplied three-panel UI board with ONLY two corrections. LEFT PANEL: there are mistakenly two green health bars. Keep the ONE green bar immediately above the shark's head (around x250,y648). Remove ONLY the lower extra green bar crossing the shark's back/waist (around x242,y798) and restore the shark surface under it. CENTER PANEL: the partially visible bottom coin pouch row currently says 'Lv. 1 / 20'; replace that with exact 'Lv. 1 / 30'. Keep ALL other text, tabs, prices, actual low-poly gameplay scene, artwork, character placements and right '장착' action unchanged.

## Chapter and purchase state

Create a companion UI state mockup from the approved input board, showing TWO portrait screens side by side, landscape 1024x1024. Same aged timber/brass/parchment craft style, identical cobbler and shop aesthetic. This companion demonstrates the chapter-upgrade tab and an UNOWNED equipment purchase.
LEFT screen: "신발 개조소" title, back control, two tabs "상시 업그레이드" inactive dark and "챕터 업그레이드" selected gold. Short goggled-cobbler header. A small helpful line "챕터를 해금하면 특별 강화가 열립니다". Below THREE full-width spacious chapter cards, clearly distinct from the small repeatable regular upgrade list.
Card1 unlocked, warm parchment: "CHAPTER 01 · 노량진", separate explicit lines "공격력 +100%" and "체력 +200%", coin action "강화 500". Small note "1회 강화".
Card2 locked, subdued but readable: lock icon, "CHAPTER 02 · 고속도로", effects "공격력 +200%" and "체력 +400%", disabled action "챕터 2 해금 후 가능".
Card3 locked: lock icon, "CHAPTER 03 · 휴게소", effects "공격력 +300%" and "체력 +600%", disabled action "챕터 3 해금 후 가능".
Put a discreet footer outside or along the bottom screen border reading "강화 수치·가격은 시안 예시" so these intentionally LARGE bonuses are proposals, not claimed final balance. No level dots or ordinary 60-level scales on these one-time chapter upgrades.
RIGHT screen: preserve equipment shop setup, shark preview, footwear tabs and 2x2 shoe cards from input. Now select UNOWNED green "항구 작업화" instead of red. Move the gold outline and selected check to the green boots. Cards use explicit stat text: red "공격력 +15%" and "보유 중", green "체력 +16%" and diamond 400, steel "공격력 +32%" diamond600, black "코인 획득 +32%" diamond900. Bottom large action reads "구매" with blue diamond icon and "400". No word 장착 on this unowned purchase action. Header wallet gem600 so the purchase is available. Maintain two legs plus tail-tip sneaker on preview shark, no extra leg. Keep all Korean text clear and professional.

