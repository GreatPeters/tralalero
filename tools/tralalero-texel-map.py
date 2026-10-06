"""Rasterize the shark body UVs into texel -> 3D position / normal maps.

Shared by the face-repaint tools. Coordinates are the S22 "view" frame:
x = right(+)/left(-), y = tail(+)/snout(-), z = up, ground at z~0.1.
"""
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'outputs/s22-polish-2026-10-01/player-geometry-before.json'

def load_body():
 d=json.loads(SRC.read_text(encoding='utf-8'))
 v=np.array(d['vertices'],dtype=np.float64);S=2.8/(v[:,1].max()-v[:,1].min())
 pos=np.stack([v[:,0]*S,-v[:,2]*S,v[:,1]*S],1)
 n=np.array(d['normals'],dtype=np.float64);nrm=np.stack([n[:,0],-n[:,2],n[:,1]],1)
 uv=np.array(d['uv'],dtype=np.float64);tris=np.array(d['triangles'][0],dtype=np.int64).reshape(-1,3)
 w=np.array(d['weights'],dtype=np.float64);bones=d['bones']
 return pos,nrm,uv,tris,w,bones

def texel_maps(size,pos,nrm,uv,tris):
 """Return (P[h,w,3], N[h,w,3], mask[h,w]) by barycentric rasterization."""
 P=np.zeros((size,size,3));N=np.zeros((size,size,3));M=np.zeros((size,size),bool)
 px=np.stack([uv[:,0]*size,(1-uv[:,1])*size],1)
 for t in tris:
  a,b,c=px[t];x0=int(max(0,np.floor(min(a[0],b[0],c[0]))));x1=int(min(size-1,np.ceil(max(a[0],b[0],c[0]))))
  y0=int(max(0,np.floor(min(a[1],b[1],c[1]))));y1=int(min(size-1,np.ceil(max(a[1],b[1],c[1]))))
  if x1<x0 or y1<y0:continue
  gx,gy=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
  den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
  if abs(den)<1e-12:continue
  l0=((b[1]-c[1])*(gx-c[0])+(c[0]-b[0])*(gy-c[1]))/den;l1=((c[1]-a[1])*(gx-c[0])+(a[0]-c[0])*(gy-c[1]))/den;l2=1-l0-l1
  inside=(l0>=-.02)&(l1>=-.02)&(l2>=-.02)
  if not inside.any():continue
  ys,xs=np.nonzero(inside);ys2,xs2=ys+y0,xs+x0
  L=np.stack([l0[inside],l1[inside],l2[inside]],1)
  P[ys2,xs2]=L@pos[t];nn=L@nrm[t];N[ys2,xs2]=nn/np.linalg.norm(nn,axis=1,keepdims=True);M[ys2,xs2]=True
 return P,N,M
