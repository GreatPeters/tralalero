import asyncio,json,shutil,subprocess,wave
from pathlib import Path
import edge_tts,imageio_ffmpeg
root=Path(__file__).resolve().parents[1];out=root/'outputs/s22-polish-2026-10-01/voices';backup=out/'original-long-sale';backup.mkdir(exist_ok=True)
for ext in ('mp3','wav'):
 p=out/f'auction-sold-live.{ext}'
 if not (backup/p.name).exists():shutil.copy2(p,backup/p.name)
asyncio.run(edge_tts.Communicate('낙찰!','ko-KR-InJoonNeural',rate='+12%',pitch='+6Hz').save(str(out/'auction-sold-live.mp3')))
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-i',str(out/'auction-sold-live.mp3'),'-af','highpass=f=90,loudnorm=I=-17:TP=-1.5:LRA=8,atempo=1.2','-ar','24000','-ac','1','-c:a','pcm_s16le',str(out/'auction-sold-live.wav')],check=True)
with wave.open(str(out/'auction-sold-live.wav')) as f:seconds=f.getnframes()/f.getframerate()
assert seconds<1.65,seconds
manifest=json.loads((out/'manifest.json').read_text(encoding='utf-8'))
for r in manifest:
 if r['name']=='auction-sold-live':r['text']='낙찰!';r['seconds']=seconds
(out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(out/'auction-sold-live.wav',root/'Assets/ShooterSurvival/Audio/NoryangjinRevamp/S22Polish/auction-sold-live.wav')
print('Short sale cue seconds:',seconds)
