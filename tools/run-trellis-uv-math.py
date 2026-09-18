"""Start the installed TRELLIS backend with independent UV-only raster math."""
import json
import builtins
import os
from pathlib import Path
import runpy
import sys
import types

ROOT=Path(__file__).resolve().parent.parent
COMFY=Path('C:/AI/TRELLIS2-AMD/ComfyUI')
os.environ.update(HIP_VISIBLE_DEVICES='0',PYTORCH_NO_CUDA_MEMORY_CACHING='1',ATTN_BACKEND='sdpa',HF_HUB_OFFLINE='1',TRANSFORMERS_OFFLINE='1')
# Keep this bootstrap stdlib-only: ComfyUI must initialize its allocator before
# torch, server, execution or model_vbar are imported.
def implementation():
    import trellis_uv_math
    return trellis_uv_math
class UVContext:
    def __init__(self,*args,**kwargs):pass
package=types.ModuleType('nvdiffrast');package.__path__=[];package.__file__=str(ROOT/'tools/trellis_uv_math.py')
api=types.ModuleType('nvdiffrast.torch');api.__file__=package.__file__
api.RasterizeCudaContext=UVContext;api.RasterizeGLContext=UVContext
api.rasterize=lambda *a,**kw:implementation().rasterize(*a,**kw)
api.interpolate=lambda *a,**kw:implementation().interpolate(*a,**kw)
package.torch=api;sys.modules['nvdiffrast']=package;sys.modules['nvdiffrast.torch']=api
uv=types.ModuleType('uv_raster');uv.__file__=package.__file__;uv.rasterize=lambda *a,**kw:implementation().uv_rasterize(*a,**kw);sys.modules['uv_raster']=uv
if '--probe-imports' in sys.argv:
    assert 'torch' not in sys.modules and 'server' not in sys.modules
    print('Bootstrap verified: no early torch/server imports');raise SystemExit(0)
sys.path.insert(0,str(COMFY))
os.chdir(COMFY)
sys.argv=[str(COMFY/'main.py'),'--listen','127.0.0.1','--port','8189','--use-pytorch-cross-attention','--disable-pinned-memory']
original_import=builtins.__import__
def observe_server_import(name,*args,**kwargs):
    result=original_import(name,*args,**kwargs)
    if name=='server' and hasattr(result,'PromptServer'):
        builtins.__import__=original_import
        original_init=result.PromptServer.__init__
        def init_with_status(self,*a,**kw):
            original_init(self,*a,**kw)
            from aiohttp import web
            @self.routes.get('/task-uv-raster-status')
            async def raster_status(request):
                module=sys.modules.get('trellis_uv_math')
                return web.json_response({'implementation':'independent-uv-math-v1',
                    'module':str(ROOT/'tools/trellis_uv_math.py'),
                    'nvdiffrastModule':sys.modules['nvdiffrast'].__file__,
                    'counts':module.COUNTS if module else {'uv_rasterizations':0,'interpolations':0}})
        result.PromptServer.__init__=init_with_status
    return result
builtins.__import__=observe_server_import
print('TASK UV RASTER: independent barycentric math, no NVIDIA raster library imported',flush=True)
runpy.run_path(str(COMFY/'main.py'),run_name='__main__')
