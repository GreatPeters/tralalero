# C UI revision generation prompts

Tool: built-in `image_gen` (editing local image references).

## Initial revision

Use case: ui-mockup, compositing edit.
Create a revised high-fidelity Korean mobile game UI concept board with three portrait screens side by side, landscape 1536x1024 composition as in input image 1.
Input image 1 is the EDIT TARGET, the brown/brass "C · 항구 공방" three-screen board. Input image 2 is the user's ORIGINAL GAME START UI and is the authoritative reference for the LEFT screen's camera, interaction and layout.
Preserve the lovingly textured dark wood, brass rivets, parchment and warm amber lights of image 1. Revise only the LEFT start screen and CENTER upgrade screen. Preserve the RIGHT equipment-shop screen's layout, shark, two normal legs, tail-tip sneaker (no extra leg), all four product cards, selected red shoes, all prices, bottom purchase action priced 200, and decorative workshop. Keep output sharp, complete and readable; no annotation arrows outside screens.

LEFT / GAME START:
Replace the side-profile posing shark scene with actual in-game over-the-shoulder elevated rear-view gameplay composition based closely on image 2. A wooden dock path recedes from bottom center to the upper center, turquoise water and Noryangjin fish-market stalls flank it. The small player shark is near the BOTTOM center, seen FROM BEHIND facing up the route, with blue shoes. The main visible subject is the playable path ahead, not a model showcase. Preserve this exact gameplay-camera impression. Show coin 550, gem 400 and settings at top. A modest chapter sign below says "CHAPTER 01" and "노량진 수산시장". Across the lower-middle OPEN PATH, float the original-style cream bold instruction text in two lines: "좌우로 움직여" / "게임시작". Beneath it show ONE white fingertip gesture icon with a short thin horizontal motion trail and small left-right arrows directly attached to that gesture, all one instructional graphic. No separate giant left or right navigation buttons. No enclosing large opaque instruction card, no "밀어서 출발", no play/start button, no slider, no character chooser. The bottom navigation is the existing three richly illustrated tabs "꾸미기", "강화", "스토리". This must feel like the paused first moment of a running game that immediately starts when the player moves horizontally. No shark facing the viewer or side-on fashion pose.

CENTER / SHOE WORKSHOP:
Preserve title "신발 개조소", back button and the goggled cobbler with tools, but make the cobbler/header art shorter to provide more UI space. Remove the two separate side-by-side sword and heart stat boxes entirely. Instead below the cobbler make ONE full-width parchment summary panel headed "현재 능력치", containing TWO stacked text lines spanning the same panel: sword icon "공격력 12" then heart icon "체력 60". This summary has NO arrows or next-value forecasts and is visually separate from the list below.
Replace the two-column upgrade grid with ONE VERTICAL SCROLLABLE LIST of full-width compact parchment upgrade rows. Each row contains its own part illustration at left, its name and specific effect in the middle, a small numeric level label, and a coin-cost action at right. The effect arrow belongs ONLY inside the relevant row. No circular level dots, pips, star ratings or fixed five-slot gauges anywhere on the upgrade screen. Numeric levels use different REAL per-item maxima.
Show these five full rows plus the top edge of another row to communicate more items below, with a thin scroll indicator:
1) metal toe cap illustration; name "강철 앞코"; level "Lv. 3 / 60"; effect "공격력 +12 → +16"; coin button "150".
2) green cushioning insole illustration; name "충격 흡수 깔창"; level "Lv. 3 / 60"; effect "체력 강화"; coin button "200".
3) black spring illustration; name "스프링 코일"; level "Lv. 7 / 30"; effect "공격 속도 +35% → +40%"; coin button "250".
4) rocket charm illustration; name "미사일 장식"; level "Lv. 2 / 30"; effect "미사일 지속 시간"; coin button "300".
5) blue sneaker with speed streaks; name "측면 부스터"; level "Lv. 3 / 10"; effect "좌우 이동 +15% → +20%"; coin button "350".
A partial following row shows a coin bag and name "코인 주머니", suggesting more upgrades down the scroll.
Give each row clearly legible bold Korean. Keep each label semantically attached to its own part. Maxima 60/60/30/30/10 are essential, NOT all five or all the same. No attack-left/health-right column association.
No extra tutorial paragraphs or developer labels in the UI. Keep the shop right screen unchanged. Produce the finished revised image only.

## Targeted correction

Edit this supplied three-screen Korean game UI board with ONLY two exact localized typography/layout corrections. Preserve all other pixels/composition, wood/brass styling, all left gameplay start art and text, the whole right shop, and all five full upgrade rows.
1. Inside the CENTER screen's single full-width parchment "현재 능력치" summary panel, keep its existing bounds but replace the left-right attack/health arrangement with TWO VERTICALLY STACKED, left-aligned compact rows. The title stays centered on top. First line: small sword icon, exact text "공격력 12". Second line DIRECTLY BELOW the first and aligned to the same x position: small heart icon, exact text "체력 60". Use compact typography to fit under title, with sensible line spacing. Both values must be in the same vertical column, never side by side. No next-stat arrows here. Do not enlarge the panel or move the five full upgrade rows.
2. At the very bottom, the partially visible "코인 주머니" row must show the readable level text "Lv. 1 / 30" instead of "Lv. 1 / 20". Preserve the partially clipped row indicating scrolling.
Everything else remains exactly as provided. Especially preserve gameplay rear-camera left screen and its direct-start gesture, distinct upgrade maxima 60,60,30,30,10, no level dots, and right selected-shoe purchase price 200.

