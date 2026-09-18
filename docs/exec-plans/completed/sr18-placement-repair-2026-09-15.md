# SR18 placement repair

Status: completed for U89.

- Preserved the dirty scene's disk and unsaved versions and a fresh preference baseline.
- Repaired the swapped/shared first-pair activation references; kept both manually moved actor positions.
- Protected workbook-managed map-tool assignments with one-to-one swaps, no-op re-clicks, preflight validation and Undo. Retained generic multi-target behavior.
- Resized/grounded the oversized gantry, connected visibility references, supported grounded explicit occluders and removed one exact duplicate decorative perch from active rendering.
-15 native tests passed. Live checks passed for100placement rows, all50enemy/50spot mappings, two independent activations, retry reset and gantry hide/restore; console errors0.
-66 preferences restored without mismatches;141coins/0gems preserved. Evidence and recovery paths: `map-concepts/sr18-placement-repair-2026-09-15/README.md`.
