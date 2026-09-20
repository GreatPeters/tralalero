# Seagull contact and penalty

Status: completed. [Implementation and evidence](../../../map-concepts/seagull-contact-2026-09-20/README.md).

User requests reliable contact with the visible bird, two visual revolutions like oil, and roughly 20% health loss. The implementation uses maximum-health percentage to match the existing fallen-pole percentage convention; this basis was an implementation choice stated to the user.

Findings: a 1.5m fixed cube missed animated wing/body regions; damage contact was disabled on the landing frame, before a possible physics callback. Generic parent-player lookup also accepted invisible weapon/visual colliders. The old damage was a flat value with no spin.

Changes: 13 bone-following trigger boxes generated from real sitting/flying skin samples, owned by the root kinematic body; descent/landing/departure contact, root-player-only consumption once per encounter, value=20 as maximum-HP percent, 1.2s existing 720-degree visual spin. The seagull's movement/animation respects the simulation clock and game pause. Retry resets the standalone cosmetic spin.

Baseline assets: `map-concepts/seagull-contact-2026-09-20/before/`. The workbook already supplies Seagull value=20, so no workbook mutation is needed.

Verification: new contact tests plus oil/reset regressions and real moving hit, lateral dodge, and landed-contact probes. The first probe's contact/dodge results passed, but its final synthetic movement lock exposed a probe-only background weapon coroutine; the clean replay stops those coroutines and initializes the dummy holdout array. No gameplay holdout changes are required.

Results: 15 focused tests, both assemblies and agent harness pass. Native moving and landed contacts each produced 500→400 HP, exactly one damage event and 720° visible spin. Dodge produced no damage or spin. All three kept route yaw unchanged. Native HUD and 6-second close-up recording verified; Editor restored to the saved SR18 scene.
