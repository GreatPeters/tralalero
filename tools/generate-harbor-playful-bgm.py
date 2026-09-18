"""Ten lively, melodic mystery cues for the shark arcade game."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("renderer", ROOT / "tools/generate-mobile-bgm-candidates.py")
renderer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(renderer)
renderer.OUT = ROOT / "map-concepts/harbor-polish-2026-09-15/audio/raw"
directions = [
    ("01_lantern_shuffle", "등불 아래 발걸음", "112 BPM, bouncy pizzicato strings, playful celesta hook, rounded walking bass, crisp restrained woodblock rhythm, wistful woodwind countermelody, mischievous haunted harbor"),
    ("02_candy_dash", "사탕빛 질주", "116 BPM, catchy music box and marimba melody, nimble acoustic bass, light swinging drums, nostalgic fairytale harmony with a few eerie chromatic notes, brisk cartoon adventure"),
    ("03_midnight_sneakers", "자정의 운동화", "118 BPM, lively muted electric piano and analog arpeggios, warm grooving bass, tight soft electronic drums, emotional minor melody, mysterious neon night run"),
    ("04_market_mischief", "수산시장 소동", "108 BPM, cheeky bamboo flute motif, plucked zither and pizzicato cello, energetic wooden percussion, bouncy rhythm, warm nostalgic melody and secretive minor harmonies"),
    ("05_clockwork_chase", "태엽 추격전", "120 BPM, bright clockwork celesta ostinato, nimble staccato strings, buoyant bassoon melody, jaunty acoustic bass and crisp rim clicks, playful suspense with a touching lyrical bridge"),
    ("06_saltwater_swing", "소금바람 스윙", "110 BPM, swinging vibraphone and muted piano, bouncing upright bass, light brush drums, wistful clarinet melody, spooky seaside detective adventure, warm and lively"),
    ("07_haunted_festival", "유령들의 축제", "114 BPM, lively toy theatre theme, tuneful accordion and glockenspiel, pizzicato strings and jaunty bass, gentle snare march, nostalgic festival turned mysteriously haunted"),
    ("08_tide_runners", "밀물을 달리는 상어", "122 BPM, flowing harp and plucked strings over an energetic rounded bass pulse, catchy lyrical flute hook, light orchestral percussion, hopeful minor adventure with ominous low string accents"),
    ("09_roadside_twilight", "황혼의 휴게소", "106 BPM, groovy muted guitar and warm electric piano, bouncing soft drums and analog bass, memorable bittersweet celesta motif, curious empty roadside cafe, playful creeping tension"),
    ("10_shoes_of_destiny", "운동화 원정대", "116 BPM, brisk small-orchestra adventure, memorable lyrical oboe melody, sparkling celesta, rhythmic pizzicato strings, warm horns and restrained percussion, emotional storybook heroism with mischievous dark fantasy shadows"),
]
renderer.JOBS = [dict(name=name,title=title,direction=direction,seed=26091551+i,prompt=(
    "Original instrumental arcade game background music, lively and rhythmically buoyant, " + direction +
    ", clear singable original melody, steady upbeat groove supporting fast gameplay, mild creepy mystery "
    "and nostalgic emotion, continuous instrumental backing for the entire track, uncluttered mix, "
    "gentle percussion, loop-friendly, no silence, no slow ambient section, no breakdown, no fade-out, no big finale"
)) for i,(name,title,direction) in enumerate(directions)]
if __name__ == "__main__": renderer.main()
