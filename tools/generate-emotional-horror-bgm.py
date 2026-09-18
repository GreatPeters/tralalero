"""Ten original emotional/creepy BGM studies using the installed local renderer."""
import importlib.util
import argparse
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("music_renderer", ROOT / "tools/generate-mobile-bgm-candidates.py")
renderer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(renderer)
renderer.OUT = ROOT / "map-concepts/emotional-horror-bgm-2026-09-15/raw"

DIRECTIONS = [
    ("01_candy_after_dark", "사탕빛이 꺼진 뒤", "아련한 오르골과 어두운 동화 왈츠",
     "76 BPM, lilting three-four dark fairytale waltz, tender memorable celesta and felt piano melody, wistful childhood nostalgia, flowing harp arpeggios, warm strings with sinister low cello undercurrents, a slightly out-of-tune music box echo answers the main melody, delicate ticking percussion, sweet aching beauty with a persistent uneasy shadow"),
    ("02_forgotten_kingdom", "잊힌 왕국의 문", "서정적인 관현악과 장엄한 불안감",
     "82 BPM, melancholic dark fantasy chamber orchestra, lyrical oboe and solo cello singing a broad original melody, wistful French horn responses, plucked harp and slow rolling timpani, fragile majesty of an abandoned kingdom, expressive minor harmonies, ominous low strings and restrained tremolo underneath, emotional yearning haunted by an ancient threat"),
    ("03_drowned_lullaby", "물속에 잠긴 자장가", "피아노 자장가 아래로 스며드는 공포",
     "68 BPM, intimate haunted lullaby for soft felt piano and fragile music box, simple unforgettable sorrowful melody, deep underwater ambience, bowed vibraphone and dark cello drones, sparse dissonant bell echoes and gently unstable pitch, long empty spaces between phrases, quietly heartbreaking and distinctly frightening, no percussion except a faint soft heartbeat"),
    ("04_lanterns_last_promise", "마지막 등불의 약속", "항구의 그리움과 따라오는 그림자",
     "88 BPM, haunted night harbor instrumental, wistful wooden flute leads a tender pentatonic minor melody, plucked zither and warm acoustic strings, soft wooden knocks and rounded bass pulse, nostalgic lantern-lit homecoming with something watching from the water, bowed bass tension and eerie distant glass notes, heartfelt melodic warmth and unresolved threatening harmonies"),
    ("05_broken_carousel", "멈춘 회전목마", "장난감 왈츠와 낡은 카니발의 섬뜩함",
     "90 BPM, dark nostalgic carousel waltz, delicate detuned celesta and toy piano, a beautiful sad original theme over pizzicato double bass, wheezing soft accordion, dry ticking percussion, occasional eerie chromatic wrong notes, slow string shadows underneath, abandoned childhood carnival at midnight, bittersweet charm and unmistakable creepy unease"),
    ("06_midnight_return", "돌아갈 수 없는 밤길", "전진하는 리듬과 쓸쓸한 신스 멜로디",
     "100 BPM, emotional nocturnal driving soundtrack, mournful electric piano and warm analog arpeggios, a clear yearning minor melody on distant solo violin, steady muted electronic pulse and deep bass, abandoned highway after midnight, cold detuned synth shadows and restrained dissonant string swells, bittersweet memories pushing forward through mounting supernatural dread"),
    ("07_empty_banquet", "아무도 없는 만찬", "고풍스러운 선율과 비어 있는 공간의 긴장",
     "78 BPM, haunted baroque chamber soundtrack, sorrowful harpsichord motif answered by lyrical bass clarinet and cello, slow pizzicato strings and quiet clockwork ticks, faded elegance of an empty banquet hall, emotional minor-key counterpoint, ominous bowed-metal resonance and creeping chromatic bass, beautiful fragile melody with sinister pauses and suspended endings"),
    ("08_black_tide_requiem", "검은 밀물의 진혼곡", "깊은 현악의 슬픔과 무거운 공포",
     "70 BPM, instrumental ocean gothic requiem, aching solo cello melody over restrained low strings, sparse resonant piano notes, quiet pipe-organ foundation and deep distant tom pulse, vast black water and loss, close dissonant string harmonics and trembling high violins, mournful lyrical beauty surrounded by palpable dread, slow breathing dynamics without explosive climaxes"),
    ("09_porcelain_memory", "금 간 도자기의 기억", "가녀린 추억의 멜로디와 불안한 오르골",
     "72 BPM, psychological horror nostalgia, exposed fragile music box melody doubled softly by felt piano, grieving viola countermelody, warm but warbling tape texture, delicate glass harmonics with unsettling semitone echoes, rounded low heartbeat bass, sense of a precious memory decaying in a locked room, emotional tenderness and chilling intimacy, very sparse arrangement"),
    ("10_gate_before_dawn", "새벽 직전의 문", "희망이 잠깐 비치는 서정적인 공포",
     "84 BPM, bittersweet dark fantasy game theme, memorable mournful piano melody growing into lyrical woodwinds and small string ensemble, harp droplets and restrained timpani pulse, a fleeting hopeful major chord returns to haunted minor, low cello ostinato and ominous dissonant bell echoes, emotional nostalgia facing an unknown terror, elegant melodic development and lingering unresolved suspense"),
]

renderer.JOBS = [
    dict(name=name, title=title, direction=summary, seed=26091501 + index, prompt=(
        "Original instrumental game background music. " + direction +
        ". A clear expressive original melody remains audible throughout, gentle musical evolution, "
        "creepy atmospheric tension, balanced uncluttered mix with room for gameplay sound effects, "
        "steady loop-friendly arrangement, no intro silence, no big finale."
    ))
    for index, (name, title, summary, direction) in enumerate(DIRECTIONS)
]

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--retry-track", type=int, choices=range(1, 11))
    args = parser.parse_args()
    if args.retry_track:
        job = dict(renderer.JOBS[args.retry_track - 1])
        job["seed"] += 1000
        job["prompt"] += (
            " Continuous musical bed from the first beat to the last beat, sustained quiet strings "
            "and an unbroken soft harp ostinato support every melodic phrase, no silent breaks, "
            "no pauses, no breakdown, no fade out."
        )
        renderer.JOBS = [job]
        renderer.OUT = ROOT / f"map-concepts/emotional-horror-bgm-2026-09-15/raw-retry-{args.retry_track:02}"
    renderer.main()
