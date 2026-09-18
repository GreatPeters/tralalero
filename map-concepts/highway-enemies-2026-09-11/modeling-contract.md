# Highway enemy production contract

- Six distinct characters: ConeMechanic, TrafficPatrol, TollgateChief, TireBruiser, AsphaltWorker, DeliveryRider.
- Preserve reviewed TRELLIS geometry, outfits, props, UVs and PBR textures. Do not repeat the rejected4k-triangle decimation.
- Use approximately2.2m authored height, with per-enemy gameplay scale separate from collider bounds. Existing meshes are approximately23k triangles each.
- Add a named deform skeleton and six generic actions matching the existing enemy animation contract (idle, walk, run, attack_loop, attack_once, die).
- Keep carried props attached to the appropriate hand and avoid broad torso/leg deformation. Review idle, opposite walking strides, attack and death from side/front in addition to a multiview export sheet.
- Inputs and reconstruction already complete; current stages are articulation, export and visual/runtime validation. Source FBX/GLB files remain untouched; rigged siblings and deterministic Python are the deliverables.
- Runtime prefabs must activate, face the player, move, attack, take damage and die using the existing combat/event systems. All six must appear in the Highway enemy palette and the chapter.
