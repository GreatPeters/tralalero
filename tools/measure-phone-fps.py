"""Read only the game's Android SurfaceFlinger presentation timestamps."""
import argparse
import json
import re
import shlex
import subprocess
import time
from pathlib import Path

parser=argparse.ArgumentParser();parser.add_argument('output');parser.add_argument('--seconds',type=float,default=10);args=parser.parse_args()
adb=['C:/Program Files/Unity/Hub/Editor/6000.2.6f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe','-s','R5CT60Y9RSM']
def shell(command):return subprocess.check_output(adb+['shell',command]).decode('utf-8',errors='replace')
layers=shell('dumpsys SurfaceFlinger --list').splitlines()
line=next(l for l in layers if 'SurfaceView[' in l and 'tralaleroshooter' in l and '(BLAST)' in l)
layer=re.search(r'RequestedLayerState\{(.+?) parentId=',line).group(1)
def sample():
    raw=shell('dumpsys SurfaceFlinger --latency '+shlex.quote(layer))
    values=[]
    for line in raw.splitlines()[1:]:
        fields=line.split()
        if len(fields)==3:
            actual=int(fields[1])
            if 0<actual<2**63-1:values.append(actual)
    return values
initial=sample();assert initial,'No presented frames'
threshold=max(initial);frames=set();started=time.monotonic()
while time.monotonic()-started<args.seconds:
    time.sleep(.65)
    frames.update(t for t in sample() if t>threshold)
ordered=sorted(frames);assert len(ordered)>10,'Too few gameplay frames'
durations=[(b-a)/1e6 for a,b in zip(ordered,ordered[1:])];sorted_ms=sorted(durations)
battery='\n'.join(l.strip() for l in shell('dumpsys battery').splitlines() if re.match(r'^\s*(level|temperature|USB powered):',l))
report={'layer':layer,'frames':len(ordered),'seconds':(ordered[-1]-ordered[0])/1e9,'fps':(len(ordered)-1)*1e9/(ordered[-1]-ordered[0]),'meanMs':sum(durations)/len(durations),'p95Ms':sorted_ms[int(len(sorted_ms)*.95)],'over33ms':sum(t>33.5 for t in durations),'battery':battery,'frameDurationsMs':durations}
path=Path(args.output);path.parent.mkdir(parents=True,exist_ok=True)
with path.open('x',encoding='utf-8') as output:json.dump(report,output,indent=2)
print(json.dumps({k:v for k,v in report.items() if k!='frameDurationsMs'}))
