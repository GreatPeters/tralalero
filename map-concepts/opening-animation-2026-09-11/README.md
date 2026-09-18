# Animated opening

The delivered movie contains generated character and environmental motion rather than pan/zoom transitions over still images.

- Four revised portrait keyframes retain the supplied shark, shrine, shoe deity and curse story; three-legged anatomy was corrected before video generation.
- Local Wan2.2 TI2V5B generated121 frames per shot at24fps. Subjects move: the shark reaches/bites, the deity gestures, expressions change, and the shark walks while water/gulls move.
- Final movie: `Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4`,20.1667seconds,576×1024,H.264. It is silent; dialogue remains editable Korean UI text.
- `OpeningStoryUI` automatically plays the movie, supports replay/next/skip, blocks gameplay during the story and releases its render target on closing. The lobby video button opens it again.

[Movie report](movie-report.json) records the output. Each `shot-XX-workflow.json`, request/history file and generated frame directory retains provenance. `tools/generate-opening-video.py` follows the [official native ComfyUI workflow](https://docs.comfy.org/tutorials/video/wan/wan2_2); model downloads were pinned and SHA256 checked. Video generation ran locally on the existing ComfyUI installation.

The keyframes were made with built-in ImageGen from the original four-panel art. Briefs requested a single9:16 shot for each story beat, removed speech bubbles/text, preserved character identity and reserved the lower area for subtitles. No paid video API was used.
