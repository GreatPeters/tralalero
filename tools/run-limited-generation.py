"""Run a task-owned generation entry point with bounded CPU use on Windows."""
import argparse
import ctypes
import json
import os
from pathlib import Path
import runpy
import sys
import time

parser = argparse.ArgumentParser()
parser.add_argument('--script', type=Path, required=True)
parser.add_argument('--record', type=Path, required=True)
parser.add_argument('--threads', type=int, default=4)
parser.add_argument('arguments', nargs=argparse.REMAINDER)
in_blender = 'bpy' in sys.modules and '--' in sys.argv
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if in_blender else None)
threads = max(1, min(args.threads, max(1, (os.cpu_count() or 2) - 2)))
for name in ('OMP_NUM_THREADS', 'MKL_NUM_THREADS', 'OPENBLAS_NUM_THREADS', 'NUMEXPR_MAX_THREADS', 'MAX_JOBS'):
    os.environ[name] = str(threads)
os.environ['TORCHINDUCTOR_COMPILE_THREADS'] = str(min(threads, 2))
os.environ['TOKENIZERS_PARALLELISM'] = 'false'
affinity = []
available = None
if os.name == 'nt':
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    kernel.GetCurrentProcess.restype = ctypes.c_void_p
    process = kernel.GetCurrentProcess()
    kernel.SetPriorityClass.argtypes = [ctypes.c_void_p, ctypes.c_ulong]
    if not kernel.SetPriorityClass(process, 0x00004000):
        raise ctypes.WinError(ctypes.get_last_error())
    process_mask, system_mask = ctypes.c_size_t(), ctypes.c_size_t()
    kernel.GetProcessAffinityMask.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_size_t), ctypes.POINTER(ctypes.c_size_t)]
    if not kernel.GetProcessAffinityMask(process, ctypes.byref(process_mask), ctypes.byref(system_mask)):
        raise ctypes.WinError(ctypes.get_last_error())
    allowed = [bit for bit in range(64) if process_mask.value & (1 << bit)]
    affinity = allowed[:max(1, min(8, len(allowed) - 2))]
    kernel.SetProcessAffinityMask.argtypes = [ctypes.c_void_p, ctypes.c_size_t]
    if not kernel.SetProcessAffinityMask(process, sum(1 << bit for bit in affinity)):
        raise ctypes.WinError(ctypes.get_last_error())
    class MemoryStatus(ctypes.Structure):
        _fields_ = [('length', ctypes.c_ulong), ('load', ctypes.c_ulong)] + [(name, ctypes.c_ulonglong) for name in ('total', 'available', 'pageTotal', 'pageAvailable', 'virtualTotal', 'virtualAvailable', 'extended')]
    memory = MemoryStatus(); memory.length = ctypes.sizeof(memory)
    if not kernel.GlobalMemoryStatusEx(ctypes.byref(memory)):
        raise ctypes.WinError(ctypes.get_last_error())
    available = memory.available
    if available < 8 * 1024**3:
        raise RuntimeError('Less than8GiB RAM available; do not start another heavy job.')
record = {'pid':os.getpid(), 'started':time.time(), 'script':str(args.script.resolve()),
          'threads':threads, 'logicalProcessors':affinity, 'priority':'below-normal', 'availableMemoryBytes':available}
args.record.parent.mkdir(parents=True, exist_ok=True)
args.record.write_text(json.dumps(record,indent=2),encoding='utf-8')
print(json.dumps(record),flush=True)
forwarded = [*args.arguments[1:]] if args.arguments[:1] == ['--'] else args.arguments
sys.argv = [str(args.script)] + (['--'] if in_blender else []) + forwarded
sys.path.insert(0,str(args.script.resolve().parent))
runpy.run_path(str(args.script.resolve()),run_name='__main__')
