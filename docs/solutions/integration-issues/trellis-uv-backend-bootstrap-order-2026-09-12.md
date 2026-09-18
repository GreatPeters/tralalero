---
title: Preserve ComfyUI initialization order when replacing the UV export backend
date: 2026-09-12
category: integration-issues
module: Local TRELLIS2 export automation
problem_type: integration_issue
component: tooling
severity: high
symptoms:
  - "A custom bootstrap started ComfyUI on its default port instead of the task port"
  - "Generation failed with NoneType having no attribute vbars_reset_watermark_limits"
  - "A reachable status endpoint did not prove that model execution worked"
root_cause: async_timing
resolution_type: code_fix
tags: [trellis, comfyui, bootstrap, uv, export, amd]
---

# Preserve ComfyUI initialization order when replacing the UV export backend

## Problem

The installed TRELLIS automation needed a separate UV export implementation. Its dependency manifest included nvdiffrast, whose inspected license restricts ordinary use to non-commercial research/evaluation. The existing app and dependency installation needed to remain recoverable while the task used an independently implemented UV path.

A first wrapper imported ComfyUI modules too early. The service could start and expose a status route while generation still failed before producing an asset.

## Symptoms

- Importing `server` before the normal entry point parsed arguments under the wrong initialization state and selected the default port.
- Importing `torch`/`server` before the installed allocator setup left `model_vbar.lib` unset. Graph execution raised `NoneType ... vbars_reset_watermark_limits`.
- Successful HTTP discovery and node enumeration were insufficient evidence of a usable runtime.

## What Didn't Work

- Starting the existing backend unchanged was unsuitable for this production path because of the inspected export dependency restriction.
- Moving argument parsing earlier fixed the port symptom but did not fix the allocator order.
- Eagerly importing the independent math implementation also imported torch too early.
- Retrying all image jobs against a merely reachable server repeated the same initialization failure. Those failed status entries remained until each job was actually retried.

## Solution

[run-trellis-uv-math.py](../../../tools/run-trellis-uv-math.py) performs only standard-library work before entering the installed ComfyUI entry point:

1. Install process-local compatibility modules with lazy functions.
2. Configure the task arguments and environment.
3. Enter ComfyUI through its normal `main.py` using `runpy`.
4. Add the diagnostic route after the normal server import, then restore the temporary import hook.
5. Import torch and [trellis_uv_math.py](../../../tools/trellis_uv_math.py) only when a UV operation is requested.

The math module implements 2D triangle coverage and barycentric interpolation from NumPy/Numba and torch primitives. It does not import NVIDIA raster source or binaries. Its compatibility names exist only inside this task process; the installed app files are not patched. Camera rendering, gradients and unsupported batch shapes are outside its contract and must fail explicitly.

Verification proceeds in layers: `--probe-imports`, a64×64 image canary through the actual graph executor, analytic UV square/winding/masked-pixel checks, then complete textured TRELLIS exports. The task status route identifies the implementation and counts raster/interpolation calls. Seven footwear and seven headwear production jobs completed through this path; retained source/export manifests identify the generated files.

## Why This Works

ComfyUI and the installed AMD allocator retain control over their initialization sequence. The compatibility facade does not import model/runtime code during bootstrap. The canary crosses the executor boundary that HTTP health checks miss, while the full export verifies the specific UV operation actually used by production.

Replacing this one dependency is not a blanket certification of every model, library or input's commercial rights. The inspected restriction is documented in the [official nvdiffrast license](https://raw.githubusercontent.com/NVlabs/nvdiffrast/main/LICENSE.txt); the task's generated inputs and remaining dependencies retain their own terms.

## Prevention

- Keep bootstrap code standard-library-only until the normal application entry point initializes runtime dependencies.
- Treat endpoint reachability, executor success and successful export as separate checks.
- Preserve failed logs and mark evaluation-only outputs so they cannot enter the production import directory by accident.
- Use [generate-skins-reststop.py](../../../tools/generate-skins-reststop.py) and the serialized production queue; retain the app's60-second inter-job cooldown.
- Stop only the backend owned by this task. Do not change the user's normal app settings to accommodate the wrapper.

## Related Issues

- [Runtime canary](../../../map-concepts/skins-reststop-2026-09-12/runtime-canary-result.json)
- [Production work log](../../exec-plans/completed/skins-progression-analytics-reststop-2026-09-12-log.md)
- [Presentation and progression need visual verification](../workflow-issues/verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md)
