"""Animate the four reviewed opening keyframes using local Wan2.2, then encode H.264.

Native graph: https://docs.comfy.org/tutorials/video/wan/wan2_2
The frame sequence is retained so subject motion can be inspected separately from cuts.
"""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import time
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('--shots', nargs='+', type=int, default=[1, 2, 3, 4])
parser.add_argument('--frames', type=int, default=121)
parser.add_argument('--steps', type=int, default=20)
parser.add_argument('--ffmpeg', default='C:/AI/ComfyUI-Creative-AMD/venv/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe')
args = parser.parse_args()
root = Path.cwd()
comfy = Path('C:/AI/ComfyUI-Creative-AMD/ComfyUI')
output = root / 'map-concepts/opening-animation-2026-09-11'
output.mkdir(parents=True, exist_ok=True)
prompts = {
    1: 'A polished animated fantasy film shot. The blue shark actively propels itself out of the water toward the glowing sneaker, opening and closing its jaws, its tail and fins moving strongly. The sneaker gently rotates as the shark reaches for it. Water splashes and drops fall naturally. Gulls flap their wings and fly past. Continuous character animation, stable camera, preserve the shrine and character identity.',
    2: 'A polished animated fantasy film shot. The huge purple stone shoe deity slowly closes its enormous hands toward the little blue shark while its face scowls and its mouth moves angrily. The frightened shark flinches, looks upward, blinks and shuffles its THREE shoe-wearing legs. Purple lightning crawls around all THREE shoes. Continuous believable character movement, preserve exactly three feet, stable camera.',
    3: 'A polished animated fantasy film shot. The gigantic purple stone deity moves its pointing hand toward the fish market and speaks angrily, moving its mouth and brows. The little three-legged blue shark turns its head toward the market, sighs and slumps its shoulders. Gulls fly and flap in the sky, market flags and ropes sway, harbor water ripples. Preserve all THREE shoes and distinct identities. Stable camera, continuous body motion.',
    4: 'A polished animated fantasy film shot. The funny determined blue shark walks forward along the fish-market road using its THREE short legs and THREE blue sneakers in a clear alternating walking cycle, body bobbing, arms swinging, tail swaying. Camera gently tracks alongside to keep the whole shark in frame. Gulls flap and fly through the sky. Continuous footstep animation, no sliding, preserve the three-legged anatomy and character face.'
}

def request(path, data=None):
    req = urllib.request.Request('http://127.0.0.1:8190' + path,
        data=None if data is None else json.dumps(data).encode(),
        headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=30) as response:
        return json.load(response)

def node(kind, **inputs):
    return {'class_type': kind, 'inputs': inputs}

for shot in args.shots:
    clip = output / f'shot-{shot:02d}.mp4'
    if clip.exists():
        print(f'Preserving existing {clip}', flush=True)
        continue
    source = root / f'Assets/JH/UI/Opening/Animated/Story_{shot:02d}.png'
    image_name = f'codex-opening-{shot:02d}.png'
    shutil.copy2(source, comfy / 'input' / image_name)
    prefix = f'codex-opening-20260911/shot-{shot:02d}'
    graph = {
        '1': node('UNETLoader', unet_name='wan2.2_ti2v_5B_fp16.safetensors', weight_dtype='default'),
        '2': node('CLIPLoader', clip_name='umt5_xxl_fp8_e4m3fn_scaled.safetensors', type='wan', device='default'),
        '3': node('VAELoader', vae_name='wan2.2_vae.safetensors'),
        '4': node('CLIPTextEncode', clip=['2',0], text=prompts[shot]),
        '5': node('CLIPTextEncode', clip=['2',0], text='still image, slideshow, freeze frame, static pose, text, subtitles, speech bubble, watermark, distorted face, fused limbs, extra limbs, morphing, flickering, camera shake, blurry, low quality'),
        '6': node('LoadImage', image=image_name),
        '7': node('Wan22ImageToVideoLatent', vae=['3',0], width=512, height=896, length=args.frames, batch_size=1, start_image=['6',0]),
        '8': node('ModelSamplingSD3', model=['1',0], shift=8),
        '9': node('KSampler', model=['8',0], positive=['4',0], negative=['5',0], latent_image=['7',0], seed=202609110+shot, steps=args.steps, cfg=5, sampler_name='uni_pc', scheduler='simple', denoise=1),
        '10': node('VAEDecodeTiled', samples=['9',0], vae=['3',0], tile_size=512, overlap=64, temporal_size=16, temporal_overlap=4),
        '11': node('SaveImage', images=['10',0], filename_prefix=prefix),
    }
    (output / f'shot-{shot:02d}-workflow.json').write_text(json.dumps(graph, indent=2), encoding='utf-8')
    submitted = request('/prompt', {'prompt': graph})
    prompt_id = submitted['prompt_id']
    (output / f'shot-{shot:02d}-request.json').write_text(json.dumps(submitted), encoding='utf-8')
    print(f'Shot{shot} queued: {prompt_id}', flush=True)
    deadline = time.monotonic() + 7200
    while time.monotonic() < deadline:
        history = request('/history/' + prompt_id).get(prompt_id)
        if history:
            (output / f'shot-{shot:02d}-history.json').write_text(json.dumps(history, indent=2), encoding='utf-8')
            if history.get('status', {}).get('status_str') == 'error':
                raise RuntimeError(f'Shot{shot} failed; inspect retained history.')
            images = history.get('outputs', {}).get('11', {}).get('images')
            if images:
                frames = output / f'shot-{shot:02d}-frames'; frames.mkdir(exist_ok=True)
                for i, image in enumerate(images):
                    shutil.copy2(comfy / 'output' / image['subfolder'] / image['filename'], frames / f'{i:04d}.png')
                subprocess.run([args.ffmpeg, '-hide_banner', '-loglevel', 'error', '-framerate', '24', '-i', str(frames/'%04d.png'), '-vf', 'scale=576:1024:flags=lanczos,setsar=1', '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(clip)], check=True)
                print(f'Shot{shot} completed: {len(images)} generated frames', flush=True)
                break
        time.sleep(10)
    else:
        raise TimeoutError(f'Shot{shot} is still pending; prompt {prompt_id}')
