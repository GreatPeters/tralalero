"""Make compact loop masters and original, deterministic arcade one-shots."""
from pathlib import Path
import json
import math
import subprocess
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "map-concepts/mobile-presentation-2026-09-14/audio"
OUT = ROOT / "Assets/ShooterSurvival/Resources/Audio/Mobile"
MASTERS = SOURCE / "processed"
FFMPEG = Path("C:/AI/ComfyUI-Creative-AMD/venv/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe")
RATE = 22050
RNG = np.random.default_rng(260914)

def read(path):
    with wave.open(str(path), "rb") as stream:
        samples = np.frombuffer(stream.readframes(stream.getnframes()), dtype="<i2").astype(np.float64) / 32768
        return samples.reshape(-1, stream.getnchannels()), stream.getframerate()

def write(path, data, rate):
    data = np.asarray(data)
    if data.ndim == 1: data = data[:, None]
    with wave.open(str(path), "wb") as stream:
        stream.setnchannels(data.shape[1]); stream.setsampwidth(2); stream.setframerate(rate)
        stream.writeframes((np.clip(data, -.98, .98) * 32767).astype("<i2").tobytes())

def loop(data, rate, seconds=1.6):
    count = int(rate * seconds)
    phase = np.linspace(0, math.pi / 2, count)[:, None]
    seam = data[-count:] * np.cos(phase) + data[:count] * np.sin(phase)
    return np.concatenate((data[count:-count], seam))

def finish(data, peak=.72):
    data = data - np.mean(data, axis=0)
    scale = peak / max(.000001, np.max(np.abs(data)))
    return data * min(scale, 4)

def encode(source, target):
    subprocess.run([str(FFMPEG), "-hide_banner", "-loglevel", "error", "-y", "-i", str(source),
        "-c:a", "libvorbis", "-q:a", "3", str(target)], check=True, creationflags=subprocess.CREATE_NO_WINDOW)

def tone(frequency, seconds, decay=12, start=0, amount=1, end_frequency=None):
    count = int(seconds * RATE); t = np.arange(count) / RATE
    if end_frequency is None: phase = t * frequency
    else: phase = frequency*t + (end_frequency-frequency)*t*t/(2*seconds)
    envelope = np.minimum(t/.003, 1) * np.exp(-t*decay)
    sound = amount * np.sin(2*np.pi*phase) * envelope
    if start: sound = np.pad(sound, (int(start*RATE), 0))
    return sound

def noise(seconds, decay=12, smooth=1):
    count = int(seconds*RATE); data = RNG.uniform(-1, 1, count)
    if smooth > 1: data = np.convolve(data, np.ones(smooth)/smooth, mode="same")
    t = np.arange(count)/RATE
    return data * np.minimum(t/.004, 1) * np.exp(-decay*t)

def mix(*sounds):
    out = np.zeros(max(len(s) for s in sounds))
    for sound in sounds: out[:len(sound)] += sound
    return out

def bell(frequency, seconds=.45, start=0):
    return mix(tone(frequency,seconds,9,start,.7),tone(frequency*2.01,seconds,15,start,.22),tone(frequency*3.97,seconds,24,start,.07))

def main():
    OUT.mkdir(parents=True, exist_ok=True); MASTERS.mkdir(parents=True, exist_ok=True)
    report = {"seed":260914,"synth":"numpy deterministic additive/noise synthesis", "music":[],"effects":[]}
    for name in ("a_midnight_tide", "b_ghost_arcade"):
        data, rate = read(SOURCE / (name+".wav"))
        original = {"name":name,"rms_db":float(20*np.log10(np.sqrt(np.mean(data*data))+1e-9)),"peak":float(np.max(np.abs(data))),"seconds":len(data)/rate}
        master = finish(loop(data,rate,2), .68)
        write(MASTERS/(name+"-loop.wav"),master,rate)
        encode(MASTERS/(name+"-loop.wav"),MASTERS/(name+"-loop.ogg"))
        original.update(loop_seconds=len(master)/rate, seam_delta=float(np.max(np.abs(master[-1]-master[0]))))
        report["music"].append(original)
    # B was prompted for the more rhythmic, mischievous arcade treatment; keep A as a playable alternative.
    (OUT/"music.ogg").write_bytes((MASTERS/"b_ghost_arcade-loop.ogg").read_bytes())
    for name in ("harbor","traffic"):
        data,rate=read(SOURCE/"sfx-source"/(name+".wav"));data=finish(loop(data,rate,1),.5)
        write(MASTERS/(name+".wav"),data,rate);encode(MASTERS/(name+".wav"),OUT/(name+".ogg"))
    sounds = {
        "click":mix(tone(420,.09,45),noise(.07,65,4)*.3),
        "open":mix(tone(360,.15,17,end_frequency=650),bell(720,.18,.045)*.25),
        "close":tone(580,.14,22,end_frequency=300),
        "tab":mix(tone(760,.085,45),tone(1140,.09,40,.025,.3)),
        "denied":mix(tone(170,.18,18),tone(140,.19,18,.16)),
        "coin":mix(bell(1320,.35),bell(1760,.28,.055)*.6),
        "upgrade":mix(bell(659,.55),bell(830,.55,.085),bell(988,.65,.17)),
        "equip":mix(tone(440,.14,18),bell(880,.35,.055)),
        "shot":mix(noise(.13,38,3),tone(180,.13,32,amount=.9,end_frequency=60)),
        "enemy_hit":mix(noise(.16,28,7)*.5,tone(230,.16,28,end_frequency=110)),
        "enemy_death":mix(noise(.33,15,8)*.7,tone(210,.32,11,end_frequency=55)),
        "player_hit":mix(noise(.25,20,12)*.8,tone(100,.27,12),tone(160,.20,18,.07,.5)),
        "player_death":mix(tone(380,.65,5,end_frequency=65),noise(.48,8,12)*.4),
        "knife":mix(noise(.27,14,4),tone(1450,.32,14,.035,.16)),
        "throw":mix(noise(.26,12,15),tone(240,.2,14,end_frequency=390)*.2),
        "missile":mix(noise(.5,8,10),tone(110,.4,8,end_frequency=420)*.4),
        "explosion":mix(noise(.75,8,16),tone(65,.7,7,amount=.9,end_frequency=28)),
        "warning":mix(tone(880,.18,13),tone(880,.18,13,.24)),
        "bonus":mix(bell(784,.45),bell(1046,.5,.07)),
        "heal":mix(bell(523,.55),bell(659,.55,.10),bell(784,.60,.20)),
        "pause":mix(tone(440,.16,18),tone(330,.2,16,.08)),
        "resume":mix(tone(330,.16,18),tone(440,.2,16,.08)),
        "chapter":mix(bell(523,.8),bell(659,.8,.12),bell(784,1,.24),bell(1046,1.1,.36)),
        "victory":mix(bell(523,.9),bell(659,.9,.12),bell(784,1,.24),bell(1046,1.1,.48),tone(262,1.4,3,.48,.3)),
        "defeat":mix(bell(392,.85),bell(330,.95,.22),bell(261,1.2,.44)),
        "holdout":mix(tone(196,.4,7),tone(246,.4,7,.2),tone(294,.6,6,.4)),
    }
    # The generated knife recording is retained; its transient informs a quieter textured layer.
    knife,kr=read(SOURCE/"sfx-source/knife.wav");mono=np.mean(knife,axis=1)
    window=int(.02*kr);energy=np.convolve(mono*mono,np.ones(window)/window,mode="same")
    onset=max(0,int(np.argmax(energy))-int(.035*kr));length=int(.32*kr)
    excerpt=mono[onset:onset+length]
    excerpt=np.interp(np.arange(int(.32*RATE))/RATE,np.arange(len(excerpt))/kr,excerpt)
    excerpt*=np.linspace(1,0,len(excerpt))**1.5
    sounds["knife"]=mix(sounds["knife"],finish(excerpt,.32))
    for name,data in sounds.items():
        data=finish(data,.72)
        # Taper both ends to avoid digital clicks; preserve intended short percussive attack.
        count=min(int(.007*RATE),len(data)//3)
        data[:count]*=np.linspace(0,1,count);data[-count:]*=np.linspace(1,0,count)
        write(OUT/(name+".wav"),data,RATE)
        report["effects"].append({"name":name,"seconds":len(data)/RATE,"peak":float(np.max(np.abs(data))),"rms_db":float(20*np.log10(np.sqrt(np.mean(data*data))+1e-9))})
    report["selection"]="b_ghost_arcade: rhythm-oriented candidate matching the chosen playful ocean arcade direction; A retained as alternate. This choice is based on prompt/art direction and signal checks, not a claimed human listening test."
    (SOURCE/"processing-report.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
    print(json.dumps({"effects":len(sounds),"bytes":sum(p.stat().st_size for p in OUT.iterdir() if p.is_file()),"music":report["music"]},indent=2))

if __name__=="__main__":main()
