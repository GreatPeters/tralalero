"""Repaint the shark's frowning mouth line as the reference's toothy grin.

The painted line sits on fragmented Meshy UV islands, so the mouth is drawn in
3D: every body texel gets its surface position (tools/tralalero-texel-map.py),
the old red line is diffused away, and a new open mouth with upper/lower teeth
is drawn by its side-profile (y, z) position on both cheeks.

Usage:
  python tools/repaint-tralalero-grin.py preview <out_dir>   # write PNGs only
  python tools/repaint-tralalero-grin.py install             # back up + overwrite all skins
Backups go to outputs/tralalero-reference-2026-10-01/before/BodyAtlas once.
"""
import sys,shutil,importlib
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'));tm=importlib.import_module('tralalero-texel-map')
ATLAS=ROOT/'Assets/ShooterSurvival/Resources/Cosmetics/BodyAtlas'
BACKUP=ROOT/'outputs/tralalero-reference-2026-10-01/before/BodyAtlas'
SIZE=2048
Y_FRONT,Y_CORNER=-2.70,-1.80
INTERIOR=np.array([74,20,26.]);TEETH=np.array([246,243,234.]);LINE=np.array([58,22,24.])
PX=1/330  # approx. texel size in model units around the mouth

def upper(y):
 s=np.clip((y-Y_FRONT)/(Y_CORNER-Y_FRONT),0,1);return 1.655+.08*s+.13*s**3
def opening(y):
 s=np.clip((y-Y_FRONT)/(Y_CORNER-Y_FRONT),0,1);return .19*np.sin(np.pi*np.clip(s*.95+.05,0,1))**.9

def masks(P,N,M,original):
 y,z=P[...,1],P[...,2]
 r,g,b=(original[...,k].astype(int) for k in range(3))
 red=M&(r>95)&(r-g>32)&(r-b>32)&(y<-1.75)&(y>-2.8)&(z>1.5)&(z<1.85)
 # Dilate the old line by 2 texels to swallow its anti-aliased halo.
 old=red.copy()
 for _ in range(2):
  grown=old.copy();grown[1:]|=old[:-1];grown[:-1]|=old[1:];grown[:,1:]|=old[:,:-1];grown[:,:-1]|=old[:,1:];old=grown&M
 inside_y=(y>=Y_FRONT-.02)&(y<=Y_CORNER+.02)
 U=upper(y);O=opening(y);L=U-O
 # Signed distances (model units) to the mouth's upper/lower edges.
 du=U-z;dl=z-L
 band=M&inside_y&(z>1.45)&(z<1.95)&(np.abs(P[...,0])>.0)
 return old,band,du,dl,O

def coverage(d):return np.clip(d/PX+.5,0,1)

def teeth(y,depth,height,period,phase):
 t=((y-Y_FRONT)/period+phase)%1.0;half=.5*np.clip(1-depth/np.maximum(height,1e-6),0,1)
 return (np.abs(t-.5)<half)&(depth>=0)&(depth<=height)

def diffuse_fill(img,hole,M,iterations=60):
 out=img.astype(np.float64).copy();known=M&~hole
 for _ in range(iterations):
  if not (hole&~known).any():break
  acc=np.zeros_like(out);cnt=np.zeros(hole.shape)
  for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):
   k=np.roll(known,(dy,dx),(0,1));v=np.roll(out,(dy,dx),(0,1))
   acc+=v*k[...,None];cnt+=k
  fill=hole&~known&(cnt>0);out[fill]=acc[fill]/cnt[fill][:,None];known|=fill
 return out

def repaint(img,old,band,du,dl,O,P,M):
 out=diffuse_fill(img,old,M)
 y=P[...,1];open_cov=np.minimum(coverage(du),coverage(dl))*band
 # Teeth only where the mouth is open enough to read.
 up=teeth(y,du,np.clip(O*.42,0,.065),.068,0.0)&band&(O>.05)
 low=teeth(y,dl,np.clip(O*.34,0,.05),.068,.5)&band&(O>.06)
 color=np.where((up|low)[...,None],TEETH,INTERIOR)
 out=out*(1-open_cov[...,None])+color*open_cov[...,None]
 # Thin toon outline around the opening (and a short smile crease past the corner).
 edge=np.minimum(np.abs(du),np.abs(dl));ring=band&(edge<.009)&(du>-.009)&(dl>-.009)
 crease=M&(y>Y_CORNER-.01)&(y<Y_CORNER+.08)&(np.abs(P[...,2]-(upper(np.full_like(y,Y_CORNER))+.35*(y-Y_CORNER)))<.007)&(np.abs(P[...,0])>.3)
 line=(ring|crease);out[line]=out[line]*.25+LINE*.75
 return np.clip(out,0,255).astype(np.uint8)

def main():
 mode=sys.argv[1] if len(sys.argv)>1 else 'preview'
 pos,nrm,uv,tris,w,bones=tm.load_body();P,N,M=tm.texel_maps(SIZE,pos,nrm,uv,tris)
 original=np.asarray(Image.open(ATLAS/'skin_original/Albedo.png').convert('RGB'))
 old,band,du,dl,O=masks(P,N,M,original)
 print('old line texels',int(old.sum()),'mouth band texels',int((band&(du>0)&(dl>0)).sum()))
 skins=sorted(p.parent.name for p in ATLAS.glob('*/Albedo.png'))
 if mode=='preview':
  out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True)
  for s in skins:
   img=np.asarray(Image.open(ATLAS/s/'Albedo.png').convert('RGB'))
   Image.fromarray(repaint(img,old,band,du,dl,O,P,M)).save(out/f'{s}-Albedo.png')
  print('preview',out)
 elif mode=='install':
  BACKUP.mkdir(parents=True,exist_ok=True)
  for s in skins:
   src=ATLAS/s/'Albedo.png';copy=BACKUP/s/'Albedo.png';copy.parent.mkdir(exist_ok=True)
   if not copy.exists():shutil.copy2(src,copy)
   img=np.asarray(Image.open(copy).convert('RGB'))  # always paint from the untouched backup
   Image.fromarray(repaint(img,old,band,du,dl,O,P,M)).save(src)
  print('installed',len(skins),'skins; backups',BACKUP)
 elif mode=='restore':
  for s in skins:shutil.copy2(BACKUP/s/'Albedo.png',ATLAS/s/'Albedo.png')
  print('restored',len(skins))

if __name__=='__main__':main()
