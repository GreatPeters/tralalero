param([int]$Port=8190)
$ErrorActionPreference='Stop'
$env:HIP_VISIBLE_DEVICES='0'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class Walk14BExecutionState {
    [DllImport("kernel32.dll")]
    public static extern uint SetThreadExecutionState(uint flags);
}
'@
[void][Walk14BExecutionState]::SetThreadExecutionState([Convert]::ToUInt32('80000001',16))
try {
    & 'C:/AI/ComfyUI-Creative-AMD/venv/Scripts/python.exe' `
        'C:/AI/ComfyUI-Creative-AMD/ComfyUI/main.py' `
        --listen 127.0.0.1 --port $Port `
        --extra-model-paths-config 'C:/AI/ComfyUI-Creative-AMD/extra_model_paths.yaml' `
        --use-pytorch-cross-attention --disable-pinned-memory `
        --disable-dynamic-vram
    exit $LASTEXITCODE
}
finally {
    [void][Walk14BExecutionState]::SetThreadExecutionState([Convert]::ToUInt32('80000000',16))
}
