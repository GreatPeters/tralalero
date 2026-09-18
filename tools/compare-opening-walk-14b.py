"""Generate one 14B walk shot and package it beside the preserved 5B result.

The input, prompts, seed, frame count, generation size and total step budget match
the existing shot. The 14B-specific VAE, Euler sampler, CFG3.5, shift5 and10/20
expert switch follow the official normal-speed template. No acceleration LoRA.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'map-concepts/opening-walk-14b-2026-09-12'
OLD = ROOT / 'map-concepts/opening-animation-2026-09-11'
COMFY = Path('C:/AI/ComfyUI-Creative-AMD/ComfyUI')
FFMPEG = Path('C:/AI/ComfyUI-Creative-AMD/venv/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe')
URL = 'http://127.0.0.1:8190'
OUT.mkdir(parents=True, exist_ok=True)

def request(path, data=None):
    req = urllib.request.Request(URL + path, data=None if data is None else json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=30) as response:
        return json.load(response)

def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False), encoding='utf-8')

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def node(kind, **inputs):
    return {'class_type': kind, 'inputs': inputs}

def prepare():
    source = ROOT / 'Assets/JH/UI/Opening/Animated/Story_04.png'
    old_clip = OLD / 'shot-04.mp4'
    original = json.loads((OLD / 'shot-04-workflow.json').read_text(encoding='utf-8'))
    image_name = 'codex-walk14b-comparison-20260912.png'
    for target in (OUT / 'source.png', COMFY / 'input' / image_name):
        if target.exists() and digest(target) != digest(source):
            raise RuntimeError(f'Refusing to overwrite different source: {target}')
        if not target.exists():
            shutil.copy2(source, target)
    old_copy = OUT / 'shot-04-5b-existing.mp4'
    if old_copy.exists() and digest(old_copy) != digest(old_clip):
        raise RuntimeError('Preserved 5B copy differs')
    if not old_copy.exists():
        shutil.copy2(old_clip, old_copy)
    seed = original['9']['inputs']['seed']
    graph = {
        '1': node('UNETLoader', unet_name='wan2.2_i2v_high_noise_14B_fp8_scaled.safetensors', weight_dtype='default'),
        '2': node('UNETLoader', unet_name='wan2.2_i2v_low_noise_14B_fp8_scaled.safetensors', weight_dtype='default'),
        '3': node('CLIPLoader', clip_name='umt5_xxl_fp8_e4m3fn_scaled.safetensors', type='wan', device='default'),
        '4': node('VAELoader', vae_name='wan_2.1_vae.safetensors'),
        '5': node('CLIPTextEncode', clip=['3', 0], text=original['4']['inputs']['text']),
        '6': node('CLIPTextEncode', clip=['3', 0], text=original['5']['inputs']['text']),
        '7': node('LoadImage', image=image_name),
        '8': node('WanImageToVideo', positive=['5', 0], negative=['6', 0], vae=['4', 0], width=512, height=896, length=121, batch_size=1, start_image=['7', 0]),
        '9': node('ModelSamplingSD3', model=['1', 0], shift=5),
        '10': node('ModelSamplingSD3', model=['2', 0], shift=5),
        '11': node('KSamplerAdvanced', model=['9', 0], add_noise='enable', noise_seed=seed, steps=20, cfg=3.5, sampler_name='euler', scheduler='simple', positive=['8', 0], negative=['8', 1], latent_image=['8', 2], start_at_step=0, end_at_step=10, return_with_leftover_noise='enable'),
        '12': node('KSamplerAdvanced', model=['10', 0], add_noise='disable', noise_seed=seed, steps=20, cfg=3.5, sampler_name='euler', scheduler='simple', positive=['8', 0], negative=['8', 1], latent_image=['11', 0], start_at_step=10, end_at_step=20, return_with_leftover_noise='disable'),
        '13': node('VAEDecodeTiled', samples=['12', 0], vae=['4', 0], tile_size=512, overlap=64, temporal_size=16, temporal_overlap=4),
        '14': node('SaveImage', images=['13', 0], filename_prefix='codex-walk14b-20260912/shot-04'),
    }
    save('workflow-api.json', graph)
    save('comparison-contract.json', {
        'source': str(source), 'sourceSha256': digest(source),
        'existingClip': str(old_clip), 'existingClipSha256': digest(old_clip),
        'generationSize': [512, 896], 'presentationSize': [576, 1024],
        'frames': 121, 'fps': 24, 'seconds': 121 / 24, 'seed': seed, 'totalSteps': 20,
        'samePositivePrompt': original['4']['inputs']['text'], 'sameNegativePrompt': original['5']['inputs']['text'],
        'old': {'model': 'Wan2.2 TI2V5B FP16', 'sampler': 'uni_pc', 'cfg': 5, 'shift': 8, 'generationSeconds': 567.35},
        'new': {'model': 'Wan2.2 I2V-A14B FP8 scaled', 'sampler': 'euler', 'cfg': 3.5, 'shift': 5, 'expertSwitchStep': 10, 'textEncoderDevice': 'default', 'lora': None},
        'comparisonScope': 'Same source/prompt/duration/resolution/seed/step budget; each model uses its own VAE and sampling recipe. A practical workflow comparison, not a parameter-count-only experiment.',
    })
    return graph

def generate(graph):
    if (OUT / 'generation-report.json').exists():
        print('Generation already completed; preserving it', flush=True)
        return
    if not (OUT / 'models-ready.json').exists():
        raise RuntimeError('Verified models are not ready')
    queue = request('/queue')
    ticket_path = OUT / 'request.json'
    if ticket_path.exists():
        ticket = json.loads(ticket_path.read_text(encoding='utf-8'))
        prompt_id = ticket['prompt_id']
        known = request('/history/' + prompt_id).get(prompt_id)
        queued = any(row[1] == prompt_id for row in queue['queue_running'] + queue['queue_pending'])
        if not known and not queued:
            raise RuntimeError('Existing request is no longer known to this server; preserve it and inspect before resubmitting')
    else:
        if queue['queue_running'] or queue['queue_pending']:
            raise RuntimeError('Server has another active task; refusing to disturb it')
        save('system-before.json', request('/system_stats'))
        began = time.time()
        try:
            ticket = request('/prompt', {'prompt': graph, 'client_id': 'codex-walk14b-20260912'})
        except urllib.error.HTTPError as error:
            (OUT / 'submission-error.txt').write_text(error.read().decode(), encoding='utf-8')
            raise
        ticket['submittedUnix'] = began
        save('request.json', ticket)
        prompt_id = ticket['prompt_id']
    print(f'14B walk queued: {prompt_id}', flush=True)
    deadline = time.monotonic() + 10800
    next_report = 0
    while time.monotonic() < deadline:
        history = request('/history/' + prompt_id).get(prompt_id)
        if history:
            save('history.json', history)
            if history.get('status', {}).get('status_str') == 'error':
                raise RuntimeError('Generation failed; inspect retained history.json')
            images = history.get('outputs', {}).get('14', {}).get('images')
            if images:
                if len(images) != 121:
                    raise RuntimeError(f'Expected121 frames, got {len(images)}')
                frames = OUT / 'frames-14b'; frames.mkdir(exist_ok=True)
                for i, frame in enumerate(images):
                    source = COMFY / 'output' / frame['subfolder'] / frame['filename']
                    target = frames / f'{i:04d}.png'
                    if not target.exists():
                        shutil.copy2(source, target)
                messages = history.get('status', {}).get('messages', [])
                starts = [data['timestamp'] for kind, data in messages if kind == 'execution_start']
                ends = [data['timestamp'] for kind, data in messages if kind == 'execution_success']
                elapsed = (ends[-1] - starts[0]) / 1000 if starts and ends else time.time() - ticket['submittedUnix']
                save('generation-report.json', {'promptId': prompt_id, 'generatedFrames': len(images), 'seconds': elapsed, 'systemAfter': request('/system_stats')})
                print(f'Generated121 frames in {elapsed:.2f}s', flush=True)
                return
        if time.monotonic() >= next_report:
            stats = request('/system_stats')
            with (OUT / 'memory-samples.jsonl').open('a', encoding='utf-8') as stream:
                stream.write(json.dumps({'unix': time.time(), 'stats': stats}) + '\n')
            print(f'Waiting: {(time.time()-ticket["submittedUnix"])/60:.1f}min elapsed', flush=True)
            next_report = time.monotonic() + 30
        time.sleep(5)
    raise TimeoutError('Generation still pending; retained request supports monitoring again')

def encode():
    if not (OUT / 'generation-report.json').exists():
        raise RuntimeError('Generation has not completed')
    frames = OUT / 'frames-14b'
    for name, scale in [('shot-04-14b-native.mp4', None), ('shot-04-14b-fp8.mp4', 'scale=576:1024:flags=lanczos,setsar=1')]:
        target = OUT / name
        if target.exists():
            continue
        command = [str(FFMPEG), '-hide_banner', '-loglevel', 'error', '-n', '-framerate', '24', '-i', str(frames / '%04d.png')]
        if scale:
            command += ['-vf', scale]
        subprocess.run(command + ['-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(target)], check=True)
    target = OUT / 'comparison-5b-vs-14b.mp4'
    if not target.exists():
        font = "fontfile='C\\:/Windows/Fonts/arialbd.ttf'"
        graph = f"[0:v]pad=576:1088:0:64:color=0x071523,drawtext={font}:text='5B - EXISTING':fontcolor=white:fontsize=27:x=24:y=19[a];[1:v]pad=576:1088:0:64:color=0x071523,drawtext={font}:text='14B FP8 - NEW':fontcolor=0x6ed9f3:fontsize=27:x=24:y=19[b];[a][b]hstack=inputs=2[v]"
        subprocess.run([str(FFMPEG), '-hide_banner', '-loglevel', 'error', '-n', '-i', str(OUT / 'shot-04-5b-existing.mp4'), '-i', str(OUT / 'shot-04-14b-fp8.mp4'), '-filter_complex', graph, '-map', '[v]', '-an', '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(target)], check=True)
    slow = OUT / 'comparison-half-speed.mp4'
    if not slow.exists():
        subprocess.run([str(FFMPEG), '-hide_banner', '-loglevel', 'error', '-n', '-i', str(target), '-vf', 'setpts=2*PTS,fps=24', '-an', '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(slow)], check=True)
    print('Encoded separate, side-by-side and half-speed comparison clips', flush=True)

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--generate', action='store_true')
    parser.add_argument('--encode', action='store_true')
    args = parser.parse_args()
    graph = prepare()
    if args.generate:
        generate(graph)
    if args.encode:
        encode()
