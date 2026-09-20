# Bigger seagull and comic collision tumble

Status: complete.

Applied +50% bird size and +30% shadow diameter, preserved early landing, and added an outward/upward three-turn bird impact arc alongside the existing two-turn shark spin. Cancel ordinary motion before handing control to the hit arc, disable repeat contact and restore the Animator on reset.

Four physical gameplay cases plus a mid-descent/pause/retry probe passed. All 14 focused tests and both C# builds passed. The visual refinement freezes the spread-wing pose during tumbling. Full evidence and reproduction: `map-concepts/seagull-impact-tumble-2026-09-20/README.md`.
