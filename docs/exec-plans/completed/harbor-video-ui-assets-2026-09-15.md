# Harbor video UI asset separation

Completed: 2026-09-15. Scope is both A/B video concepts as separated, reviewable UI assets, not replacement of the game's currently installed video UI.

- Preserved concepts and generated text-free plates with chroma-key movie apertures.
- Cut/reconstructed 25 UI assets plus one real movie-frame placeholder. Thirteen surfaces have nine-slice settings; remaining decorations retain their authored aspect/alpha.
- Created two presentation-only Unity prefabs with editable TMP labels and three button/label groups each, two layered PSDs, position/text manifests and transparent overlay previews.
- Reassembled visuals use the real movie's 2-second frame. Corrected Pillow's no-upscale thumbnail behavior to match native FitInParent, retained rounded frame corners when punching only the backing, and separated a flat center strip for the long native progress fill.
- Native preview scenes initially rendered blank due to their unique scene-culling masks; added matching camera masks and a dark-image rejection gate, then visually inspected successful A/B renders.
- Saved/reopened PSD layer counts and verified actual native movie rectangles at 9:16. Editor build passed with zero warnings/errors. Original scene remains clean in Edit Mode; no preference or playback changes.
- Artifact index: `map-concepts/harbor-video-ui-2026-09-15/production/README.md`.
- Follow-up selection: A with four B-style scene indicators. Changed only A's progress entries, regenerated A's 21-layer PSD and prefab, retained B's prefab hash and v1 exports, and verified four actual opening scene boundaries / four indicators / one gold state / no continuous bar. V2 packages carry the revision.
