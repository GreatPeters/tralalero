"""Independent inference-only UV rasterization and barycentric interpolation.

Implements only the2D operations used by this TRELLIS export workflow. No NVIDIA
source/binary is imported. Unsupported camera/differentiable rendering fails closed.
"""
import sys
import types
import numpy as np
import torch
from numba import njit

COUNTS={'uv_rasterizations':0,'interpolations':0}

@njit(cache=True)
def raster_cpu(uv,faces,height,width):
    out=np.zeros((height,width,4),np.float32)
    for index in range(len(faces)):
        a,b,c=faces[index]
        x0=(uv[a,0]+1)*width*.5-.5;y0=(uv[a,1]+1)*height*.5-.5
        x1=(uv[b,0]+1)*width*.5-.5;y1=(uv[b,1]+1)*height*.5-.5
        x2=(uv[c,0]+1)*width*.5-.5;y2=(uv[c,1]+1)*height*.5-.5
        denominator=(y1-y2)*(x0-x2)+(x2-x1)*(y0-y2)
        if abs(denominator)<1e-12:continue
        left=max(0,int(np.ceil(min(x0,x1,x2))));right=min(width-1,int(np.floor(max(x0,x1,x2))))
        bottom=max(0,int(np.ceil(min(y0,y1,y2))));top=min(height-1,int(np.floor(max(y0,y1,y2))))
        for y in range(bottom,top+1):
            for x in range(left,right+1):
                wa=((y1-y2)*(x-x2)+(x2-x1)*(y-y2))/denominator
                wb=((y2-y0)*(x-x2)+(x0-x2)*(y-y2))/denominator
                wc=1-wa-wb
                if wa>=-1e-6 and wb>=-1e-6 and wc>=-1e-6:
                    out[y,x,0]=wa;out[y,x,1]=wb;out[y,x,3]=index+1
    return out

def uv_rasterize(uv,faces,resolution,*args,**kwargs):
    if args or kwargs:raise RuntimeError('Unsupported UV raster options')
    if uv.ndim!=2 or uv.shape[1]!=2:raise RuntimeError('Only2D UV coordinates are supported')
    h,w=(resolution,resolution) if isinstance(resolution,int) else tuple(resolution)
    image=raster_cpu(uv.detach().float().cpu().numpy(),faces.detach().long().cpu().numpy(),int(h),int(w))
    COUNTS['uv_rasterizations']+=1
    return torch.from_numpy(image).to(uv.device).unsqueeze(0),None

class UVContext:
    def __init__(self,*args,**kwargs):pass

def rasterize(context,positions,faces,resolution,*args,**kwargs):
    if args or any(key not in ('grad_db',) for key in kwargs):raise RuntimeError('Unsupported raster options')
    if positions.ndim!=3 or positions.shape[0]!=1 or positions.shape[2]!=4:
        raise RuntimeError('Only one batch of homogeneous UV coordinates is supported')
    p=positions[0]
    if not torch.allclose(p[:,2],torch.zeros_like(p[:,2])) or not torch.allclose(p[:,3],torch.ones_like(p[:,3])):
        raise RuntimeError('Camera/depth rendering is intentionally unsupported')
    return uv_rasterize(p[:,:2],faces,resolution)

def interpolate(attributes,raster,faces,rast_db=None,diff_attrs=None):
    if rast_db is not None or diff_attrs is not None:raise RuntimeError('Differentiable interpolation is unsupported')
    if attributes.ndim!=3 or attributes.shape[0]!=1 or raster.shape[0]!=1:
        raise RuntimeError('Only single-batch texture baking is supported')
    triangle=raster[...,3].long()-1
    valid=triangle>=0
    corners=faces.long()[triangle.clamp(min=0)]
    weights=torch.stack((raster[...,0],raster[...,1],1-raster[...,0]-raster[...,1]),dim=-1)
    values=attributes[0][corners]
    result=(values*weights[...,None]).sum(dim=-2)
    result=torch.where(valid[...,None],result,torch.zeros_like(result))
    COUNTS['interpolations']+=1
    return result,None

def install():
    if any(name.startswith('nvdiffrast') for name in sys.modules):
        raise RuntimeError('Proprietary rasterizer already imported; start a fresh process')
    package=types.ModuleType('nvdiffrast');package.__path__=[];package.__file__=__file__
    api=types.ModuleType('nvdiffrast.torch');api.__file__=__file__
    api.RasterizeCudaContext=UVContext;api.RasterizeGLContext=UVContext
    api.rasterize=rasterize;api.interpolate=interpolate
    package.torch=api;sys.modules['nvdiffrast']=package;sys.modules['nvdiffrast.torch']=api
    uv=types.ModuleType('uv_raster');uv.__file__=__file__;uv.rasterize=uv_rasterize;sys.modules['uv_raster']=uv

def self_test(device='cpu'):
    uv=torch.tensor([[-1.,-1.],[1.,-1.],[-1.,1.],[1.,1.]],device=device)
    faces=torch.tensor([[0,1,2],[1,3,2]],dtype=torch.int32,device=device)
    rast,_=uv_rasterize(uv,faces,8)
    values,_=interpolate(uv.unsqueeze(0),rast,faces)
    axis=(torch.arange(8,device=device)+.5)/8*2-1
    yy,xx=torch.meshgrid(axis,axis,indexing='ij');expected=torch.stack((xx,yy),-1)
    assert torch.all(rast[...,3]>0)
    assert torch.allclose(values[0],expected,atol=1e-6)
    reversed_rast,_=uv_rasterize(uv,faces.flip(1),8)
    reversed_values,_=interpolate(uv.unsqueeze(0),reversed_rast,faces.flip(1))
    assert torch.allclose(reversed_values,values,atol=1e-6)
    sparse,_=uv_rasterize(uv,faces[:1],8)
    sparse_values,_=interpolate(uv.unsqueeze(0),sparse,faces[:1])
    assert torch.all(sparse_values[sparse[...,3]==0]==0)
    print('Independent UV raster/interpolation tests passed:',device,flush=True)

if __name__=='__main__':
    self_test()
    if torch.cuda.is_available():self_test('cuda')
