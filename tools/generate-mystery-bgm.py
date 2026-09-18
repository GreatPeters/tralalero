"""Five new original music candidates; reuse the installed, bounded CPU renderer."""
import importlib.util
from pathlib import Path

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("music_renderer", root / "tools/generate-mobile-bgm-candidates.py")
renderer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(renderer)
renderer.OUT = root / "map-concepts/combat-feedback-2026-09-14/audio/candidates"
directions = [
    ("01_moonlit_aquarium", "78 BPM, luminous underwater mystery, delicate celesta arpeggios, warm felt piano, floating glass harmonics, rounded sub bass, sparse brushed drums, original wistful D minor melody"),
    ("02_lantern_market", "92 BPM, curious lantern-lit night market, bamboo flute fragments, plucked zither, pizzicato strings, soft wooden percussion, pentatonic minor motif, mischievous rhythmic groove"),
    ("03_starlit_highway", "104 BPM, mysterious midnight highway, analog synth arpeggios, airy electric piano, shimmering pads, pulsing warm bass, restrained electronic drums, hypnotic minor-key driving motif"),
    ("04_clockwork_rest_stop", "86 BPM, enchanted abandoned roadside cafe, slightly detuned music box, vibraphone and pizzicato double bass, ticking rim percussion, playful chromatic question-answer melody, intimate dreamlike atmosphere"),
    ("05_tidal_observatory", "72 BPM, magical ocean observatory, haunting soft flute, glass bells, slowly shifting orchestral strings, gently plucked harp, restrained deep toms, mysterious hopeful modal melody, wide open space"),
]
renderer.JOBS = [dict(name=name, seed=26091421 + i, prompt=(
    "Original instrumental looping background soundtrack for a whimsical shark arcade game, " + direction +
    ", clear uncluttered mix leaving room for game effects, stable musical pulse, evolving small motifs, seamless loop feel, no intro silence, no big finale"
)) for i, (name, direction) in enumerate(directions)]

if __name__ == "__main__":
    renderer.main()
