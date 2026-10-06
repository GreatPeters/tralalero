---
module: Chapters45 environment authoring
problem_type: workflow
tags: [unity, art-review, native-camera, mesh-preservation, floating-point]
---

# Judge environment art from the game camera, and preserve exact mesh matching

The earlier asset catalog and product close-ups looked complete while actual Ch5 movement showed a large empty wall and low isolated booths. Compare the source photographs with ordinary game-camera frames before declaring environment art complete. Prioritize silhouettes, connected floors, meaningful depth and value structure over prop counts. Keep human art review distinct from functional test pass counts.

The follow-up used append-only native mesh revisions to replace only shallow Ch4 architectural primitives in combined scenery. Every target cube still had to match all12 triangles. A float-offset spatial hash lookup found only10 faces of a thin display panel, despite nearly coincident stored coordinates. Do not relax the triangle-count or geometric acceptance tolerance to proceed. Index with rounded integer cell coordinates, and enumerate neighboring integer cells rather than adding0.01f world offsets and flooring again. The corrected lookup retained the geometric tolerance and matched all3103 target cubes/37236triangles; original mesh assets remained unchanged.

When reading multiple Unity scenes, materialize or serialize LINQ projections while their scene is still open. Deferred projections over Unity objects can become invalid after the next OpenScene call. The architecture probe now serializes each scene row before restoring or switching scenes.

A Pipeline30-second response timeout can occur while the main-thread operation continues. Do not immediately submit the installer again. Inspect the operation's installation receipt and finished captures, then call a narrow idle/saved-scene query. Both normal completion and reopened protected-state validation are needed. Initial installers reject duplicate owned roots; use an incremental tool for later edits.

Fine tower glazing that works close-up became subpixel noise at distance. Use the existing scenery-distance groups for close detail, and apply the same floor/distance rule in stationary captures. Confirm again in actual play. Rendering all decorative groups merely because they share a floor can misrepresent real visibility and performance.

Evidence: `docs/reviews/chapter45-environment-art-2026-10-03.md`, `outputs/department-store-2026-10-02/trellis-recovery/environment-qa-20261003/`, `tools/improve-chapter-environment.cs`.


## Follow-up: distinguish building form from added facade detail

User feedback still found the shops temporary after height/color/window additions. The independent reviewer agreed: keeping an improvement does not mean the visual brief is fulfilled. In game-camera views, prioritize a few visible buildings with distinct massing and true openings over multiplying small ornaments. Eight foreground/intersection buildings were given chamfered glass corners, deep punched stone windows and stepped terraces. Recessed doors replaced an empty shelf crossing the old entrance. The rest of the street remains repetitive; record this limit rather than claiming complete resolution.

Combined meshes can contain two opposite-facing triangles at a shared cornice boundary. Position-only corner matching returned fourteen faces instead of twelve and correctly stopped before saving. Preserve the full-twelve-face and distance-tolerance checks, and filter by triangle orientation relative to the target cube center so an adjacent building's opposite-facing face is retained. Original asset files remain unchanged.

The first custom polygon slab had reversed perimeter winding, making its side disappear from below. A close camera exposed the problem before final acceptance. Correct outward side winding and include a bottom cap; then inspect both the game camera and angled building views. Do not solve missing geometry by disabling backface culling.

After the final save, repeat both affected city choices and focused/directed regression. An unchanged mall scene hash allows its prior ordinary routes to remain valid; describe this scope explicitly. User-facing Korean reports must be authored as UTF-8 without piping non-ASCII source through an uncertain shell encoding. Compare HTTP text after normalizing Windows line endings, and verify downloaded image hashes.
