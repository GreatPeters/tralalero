"""Create separate Korean PA and merchant voices, with restrained role-specific mix."""
import asyncio,json,subprocess,wave
from pathlib import Path
import edge_tts,imageio_ffmpeg
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/s22-polish-2026-10-01/voices';OUT.mkdir(parents=True,exist_ok=True)
LINES={
 'auction-call-live':('auction','싱싱한 광어 나왔습니다! 삼만 원! 더 없습니까?'),
 'auction-scatter':('merchant','상어다! 비켜! 빨리 비켜!'),
 'auction-sold-live':('auction','낙찰!'),
 'broadcast-1':('pa','시장 안으로 상어가 들어왔습니다. 통로를 비워 주세요.'),
 'broadcast-2':('pa','운반 차량이 지나갑니다. 안전선 밖으로 비켜 주세요.'),
 'broadcast-3':('pa','경매장 통로에 물건을 놓지 마세요.'),
 'broadcast-4':('pa','잠시 뒤 경매가 시작됩니다. 안쪽으로 이동해 주세요.'),
 'container-warning':('pa','작업 중입니다. 낙하물에 주의하세요.'),
 'door-closing':('pa','문이 곧 닫힙니다. 주의해 주세요!'),
 'gull-warning':('pa','갈매기 주의! 바닥의 그림자를 피하세요!'),
 'merchant-catch':('merchant','놓치지 마! 이쪽을 막아!'),
 'merchant-look':('auction','어이! 거기 서!'),
 'merchant-union':('merchant','상어 잡아! 통로를 막아!'),
 'truck-reverse':('pa','차량이 후진합니다. 뒤로 물러나 주세요.'),
}
VOICES={'pa':'ko-KR-SunHiNeural','auction':'ko-KR-InJoonNeural','merchant':'ko-KR-HyunsuMultilingualNeural'}
async def main():
 allowed={x['ShortName'] for x in await edge_tts.list_voices()}
 if not set(VOICES.values())<=allowed:raise RuntimeError('Selected voice unavailable')
 records=[]
 for name,(role,text) in LINES.items():
  mp3=OUT/(name+'.mp3');target=OUT/(name+'.wav')
  if not mp3.exists():await edge_tts.Communicate(text,VOICES[role],rate='+4%' if role=='pa' else '+12%',pitch='+0Hz' if role=='pa' else '+6Hz').save(str(mp3))
  filters='highpass=f=230,lowpass=f=4800,loudnorm=I=-19:TP=-2:LRA=7' if role=='pa' else 'highpass=f=90,loudnorm=I=-17:TP=-1.5:LRA=8'
  if name=='auction-sold-live':filters+=',atempo=1.2'
  if not target.exists():subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-i',str(mp3),'-af',filters,'-ar','24000','-ac','1','-c:a','pcm_s16le',str(target)],check=True)
  with wave.open(str(target)) as f:duration=f.getnframes()/f.getframerate()
  records.append({'name':name,'role':role,'voice':VOICES[role],'text':text,'seconds':duration,'provider':'Microsoft Edge online neural TTS via edge-tts; synthetic, not recorded human acting'})
  print(name,round(duration,2),flush=True)
 (OUT/'manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
asyncio.run(main())
