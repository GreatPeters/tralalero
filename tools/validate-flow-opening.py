from pathlib import Path
import argparse,subprocess,json,re,hashlib
root=Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser(description='Decode Flow originals and extract frames for visual review.')
parser.add_argument('--folder',type=Path,default=root/'map-concepts/flow-opening-2026-09-12')
parser.add_argument('--previews',type=Path,default=root/'tmp/image-previews/flow-opening-2026-09-12')
parser.add_argument('--seconds',type=float,nargs='+',default=[1,4,7])
args=parser.parse_args()
ffmpeg=Path('C:/AI/ComfyUI-Creative-AMD/venv/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe')
folder=args.folder;previews=args.previews;previews.mkdir(parents=True,exist_ok=True)
records=[]
clips=sorted(folder.glob('*.mp4'))
if not clips:
    parser.error(f'No MP4 files found in {folder}')
for clip in clips:
    result=subprocess.run([str(ffmpeg),'-hide_banner','-i',str(clip),'-progress','pipe:1','-nostats','-f','null','-'],capture_output=True,text=True,check=True)
    (folder/(clip.stem+'-decode.log')).write_text(result.stderr,encoding='utf-8')
    frame=int(re.findall(r'^frame=(\d+)',result.stdout,re.M)[-1]);size=re.search(r'Video:.*? (\d{3,5})x(\d{3,5})',result.stderr)
    record={'file':clip.name,'frames':frame,'size':[int(v) for v in size.groups()],'audio':'Audio:' in result.stderr,'bytes':clip.stat().st_size,'sha256':hashlib.sha256(clip.read_bytes()).hexdigest()}
    assert frame>=190 and record['size']==[720,1280],record
    for second in args.seconds:
        output=previews/f'{clip.stem}-{second:g}s.png'
        subprocess.run([str(ffmpeg),'-hide_banner','-loglevel','error','-y','-ss',str(second),'-i',str(clip),'-frames:v','1',str(output)],check=True)
    records.append(record)
(folder/'validation.json').write_text(json.dumps(records,indent=2),encoding='utf-8');print(json.dumps(records,indent=2))
