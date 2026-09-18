"""Generate two Wan candidates per chapter entrance; retain latents before decoding."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import time
import urllib.request

parser=argparse.ArgumentParser()
parser.add_argument('--only',nargs='*')
args=parser.parse_args()
root=Path(__file__).resolve().parent.parent
comfy=Path('C:/AI/ComfyUI-Creative-AMD/ComfyUI')
output=root/'outputs/chapters-polish-2026-09-12/transitions'
output.mkdir(parents=True,exist_ok=True)
base='http://127.0.0.1:8190'
ffmpeg='C:/AI/ComfyUI-Creative-AMD/venv/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
common = ('Continuous cinematic cartoon animation. Preserve the exact shark design: it has TWO legs, each wearing one blue-and-gold sneaker, '
          'plus ONE long tail ending inside a third matching sneaker. The shoe-wearing tail is the third ground support. '
          'Show the continuous tail curving from the rear body down into its shoe. No third leg, no extra unshod tail. '
          'Keep both leg shoes and the tail shoe separate and planted in their starting positions for the entire shot. '
          'The shark does not walk. Locked camera, no cuts, no added writing. Real facial, arm and environmental animation.')
prompts = {
    'highway-A-v3': common + ' The shark on the safe shoulder watches the blue bus and yellow truck drive rapidly past with rotating wheels. '
        'It follows the vehicles with frightened eyes, hunches its shoulders and raises its small hands to its chest. '
        'Keep the two legs and shoe-wearing tail completely still while the upper body reacts. The vehicles keep moving along the road.',
    'highway-B-v3': common + ' The red car rapidly passes leftward along the highway, followed by the distant bus. '
        'The shark recoils slightly above the waist, worried eyes following the traffic and hands gripping each other. '
        'All three shoes remain fixed on the shoulder; the third shoe is attached only to the end of the long tail.',
    'reststop-A-v3': common + ' The GIANT shark stays about twice the height of the adult humans on the same ground. '
        'It calmly looks down and tilts its head slightly toward the frightened diners. The man raises his free hand defensively, '
        'the woman pulls her cup close and leans back with wide terrified eyes, and the chef raises his hands in alarm. '
        'Everyone is afraid of the giant shark and looks directly at it. Keep the lower shark body, two legs and shoe-wearing tail motionless. '
        'Only shark head, eyes and arms make small movements; humans react with faces, hands and upper bodies. No walking or bouncing.',
    'reststop-B-v3': common + ' The towering shark calmly watches the much smaller adult people, maintaining twice their height. '
        'The tray carrier recoils and trembles, the cup holder raises a defensive hand, and the sandwich eater shrinks back behind them. '
        'All people stare directly up at the enormous shark in fear. The chef flinches behind the kiosk. '
        'The shark subtly changes its expression and hand position but its two legs and shoe-wearing tail stay motionless. No walking.'
}

def request(path,data=None):
    payload=None if data is None else json.dumps(data).encode()
    req=urllib.request.Request(base+path,data=payload,headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(req,timeout=45) as response:
        body=response.read()
        return json.loads(body) if body else {}

def node(kind,**inputs): return {'class_type':kind,'inputs':inputs}

def execute(graph,folder,label):
    (folder/(label+'-workflow.json')).write_text(json.dumps(graph,indent=2),encoding='utf-8')
    submitted=request('/prompt',{'prompt':graph})
    prompt_id=submitted['prompt_id']
    (folder/(label+'-request.json')).write_text(json.dumps(submitted,indent=2),encoding='utf-8')
    began=time.monotonic();deadline=began+7200
    print(json.dumps({'candidate':folder.name,'stage':label,'promptId':prompt_id}),flush=True)
    while time.monotonic()<deadline:
        history=request('/history/'+prompt_id).get(prompt_id)
        if history:
            (folder/(label+'-history.json')).write_text(json.dumps(history,indent=2),encoding='utf-8')
            if history.get('status',{}).get('status_str')=='error':
                raise RuntimeError(json.dumps(history.get('status'),ensure_ascii=True))
            if history.get('status',{}).get('completed'):
                return history,time.monotonic()-began
        time.sleep(15)
    raise TimeoutError('Wan generation timed out; inspect the owned queue before resuming')

def saved_path(item):
    base_path=(comfy/'output').resolve()
    path=(base_path/item.get('subfolder','')/item['filename']).resolve()
    if not path.is_relative_to(base_path): raise RuntimeError('Unexpected output path')
    return path

available=request('/object_info/UNETLoader')['UNETLoader']['input']['required']['unet_name'][0]
model='wan2.2_ti2v_5B_fp16.safetensors'
if model not in available: raise RuntimeError('Expected installed5B Wan model missing')
if 'LoadLatent' not in request('/object_info/LoadLatent'): raise RuntimeError('LoadLatent unavailable')
for index,(name,prompt) in enumerate(prompts.items(),1):
    if args.only and name not in args.only: continue
    folder=output/name;folder.mkdir(exist_ok=True)
    clip=folder/(name+'-5B.mp4')
    if clip.exists(): raise RuntimeError('Preserve prior candidate; inspect before retrying: '+str(clip))
    image=root/'map-concepts/chapters-polish-2026-09-12/references/transitions'/(name+'.png')
    digest=hashlib.sha256(image.read_bytes()).hexdigest()
    image_name='codex-chapter-'+name+'-'+digest[:10]+'.png'
    shutil.copy2(image,comfy/'input'/image_name)
    negative='static image, slideshow, freeze frame, camera zoom only, third leg, three legs, extra tail, forked free tail tip, fused limbs, extra limbs, disappearing feet, overlapping shoes, changing character scale, distorted faces, flickering, unstable camera, added text, speech bubbles'
    if name.startswith('reststop'): negative+=', small shark, humans taller than shark, smiling unafraid diners'
    graph={
        '1':node('UNETLoader',unet_name=model,weight_dtype='default'),
        '2':node('CLIPLoader',clip_name='umt5_xxl_fp8_e4m3fn_scaled.safetensors',type='wan',device='default'),
        '3':node('VAELoader',vae_name='wan2.2_vae.safetensors'),
        '4':node('CLIPTextEncode',clip=['2',0],text=prompt),
        '5':node('CLIPTextEncode',clip=['2',0],text=negative),
        '6':node('LoadImage',image=image_name),
        '7':node('Wan22ImageToVideoLatent',vae=['3',0],width=576,height=1024,length=121,batch_size=1,start_image=['6',0]),
        '8':node('ModelSamplingSD3',model=['1',0],shift=8),
        '9':node('KSampler',model=['8',0],positive=['4',0],negative=['5',0],latent_image=['7',0],seed=2026091300+index,
                 steps=24,cfg=5,sampler_name='uni_pc',scheduler='simple',denoise=1),
        '10':node('SaveLatent',samples=['9',0],filename_prefix='codex-chapter-entry-20260912/'+name)
    }
    history,sampling=execute(graph,folder,'sample')
    latent_items=history['outputs']['10']['latents']
    if len(latent_items)!=1: raise RuntimeError('Expected one retained latent')
    latent=saved_path(latent_items[0]);shutil.copy2(latent,folder/'sample.latent')
    latent_name='codex-chapter-'+name+'-'+digest[:10]+'.latent'
    shutil.copy2(latent,comfy/'input'/latent_name)
    # The sample is durable now. Unload models before full temporal VAE decoding.
    request('/free',{'unload_models':True,'free_memory':True})
    decode={
        '1':node('LoadLatent',latent=latent_name),
        '2':node('VAELoader',vae_name='wan2.2_vae.safetensors'),
        '3':node('VAEDecode',samples=['1',0],vae=['2',0]),
        '4':node('SaveImage',images=['3',0],filename_prefix='codex-chapter-entry-20260912/'+name+'-frames')
    }
    try:
        decoded,decoding=execute(decode,folder,'decode-full')
        mode='full'
    except RuntimeError as error:
        if 'out of memory' not in str(error).lower(): raise
        request('/free',{'unload_models':True,'free_memory':True})
        decode['3']=node('VAEDecodeTiled',samples=['1',0],vae=['2',0],tile_size=512,overlap=64,temporal_size=128,temporal_overlap=8)
        decoded,decoding=execute(decode,folder,'decode-spatial-tiles')
        mode='spatial-tiles-temporal128'
    frames=decoded['outputs']['4']['images']
    if len(frames)!=121: raise RuntimeError('Expected121 animation frames, got'+str(len(frames)))
    frame_folder=folder/'frames';frame_folder.mkdir(exist_ok=True)
    for number,item in enumerate(frames): shutil.copy2(saved_path(item),frame_folder/f'{number:05d}.png')
    subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-n','-framerate','24','-i',str(frame_folder/'%05d.png'),
                    '-frames:v','121','-c:v','libx264','-crf','18','-preset','slow','-threads','4','-pix_fmt','yuv420p',
                    '-movflags','+faststart',str(clip)],check=True)
    report={'name':name,'model':model,'source':str(image.relative_to(root)),'sourceSha256':digest,'frames':121,'fps':24,
            'width':576,'height':1024,'samplingSeconds':sampling,'decodingSeconds':decoding,'decodeMode':mode,
            'clipSha256':hashlib.sha256(clip.read_bytes()).hexdigest(),'visualReview':'pending'}
    (folder/'result.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report),flush=True)
