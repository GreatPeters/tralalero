# Essential visual proposals and Google Play account integration

## Authority

User requests on 2026-10-06: apply `Downloads/트랄랄레오_슈터_필수_제안_문서.docx` to all chapters 1–5; use TRELLIS/Meshy if necessary; then integrate Google Play login and account progress, prefer Google Play's platform when sufficient, and add an account deletion button to Options. The requested storage preference is no expiry, ample capacity, and 500,000 DAU. This is a preference to investigate, not a proven service guarantee.

The actual DOCX has 71 text paragraphs and nine embedded images. Extracted source is preserved in `outputs/essential-proposals-2026-10-06/source-text.txt`. It proposes FlatKit surface rules, reduced detail/shading, preserved harbor colors, stronger Ch4 facade depth and Ch5 portal/pier/display contrast, separate glass/water/emission rules, selective asset reuse and regression checks. Its staged approval wording is superseded by the user's explicit full application instruction. Do not rename the existing Jamsil chapter to Seongsu merely because the proposal describes that reference identity.

## Execution units

1. Audit the five current saved scenes against the proposal and previous 2026-10-04/05 toon work. Preserve implemented facade/model improvements; only correct current evidence-backed gaps. Capture fresh native evidence and protect gameplay, cameras, lights, colliders and rigs.
2. Install the official pinned Play Games Unity SDK. Implement account-bound progression snapshots, login/load/save, offline retry, explicit conflict selection and account isolation. Include wallet, chapter rewards/unlocks, permanent/chapter upgrades and cosmetics. Keep device/consent/debug preferences outside cloud progress.
3. Add Options account status, login/sync and deletion confirmation using existing navy/ivory UI. Deletion clears this game's data, not the person's Google account; verify remote deletion before clearing local progress. Disable account mutations during a run and prevent late save callbacks from recreating deleted progress.
4. Run narrow native Unity tests and C# builds; render the actual account UI; verify five scenes and record console configuration and actual Android login limits.

## Service decision

Google's current Saved Games documentation states no storage charge and a 3 MB per-file limit. A small progression snapshot fits. This avoids a shared Firebase Firestore free-tier database whose 50,000 reads/20,000 writes per day cannot cover 500,000 daily users. No promise of unlimited service lifetime or guaranteed 500,000 DAU is made. Request/quota and Play Console credentials remain external configuration requirements.

## Progress

- Source document read including embedded palette diagram.
- Existing Ch1–4/Ch5 toon conversions and later bounded fixes identified; original authoring installers must not be rerun.
- Official Unity Pipeline reachable; original open scene is clean ShoeTower.
- Existing branch and 1,186 pre-existing worktree changes preserved; no commit/push requested.

Implementation and verification receipts will be appended here; document decisions above remain stable.

## Applied and verified on 2026-10-06

### Visual proposal trace

| Proposal | Current application |
| --- | --- |
| Preserve Ch1 sea/wood/awnings | Existing harbor palette/material work retained; two remaining opaque bird/dolphin material copies now use the same FlatKit family. |
| Ch2/3 reduce mixed detail and shading | Existing Ch2 toon road/vehicles/apartment/wall work and Ch3 rounded storefront/pier work retained. Ch2 has no active URP/Lit renderer; Ch3's remaining opaque prop pole received an isolated FlatKit copy. |
| Ch4 facade depth/entry proportions/activity | Existing 73 decorated storefronts, eight deeper niches, queues, benches, gardens, canonical tower and branch geometry retained. Seven material copies align security mechanisms and hold markings with the common opaque rule. |
| Ch5 distinguish portals/piers/displays | Existing 56 rounded shop portals, cream/coral/teal bands, thicker piers and varied plinths retained. No active URP/Lit renderer remains in the fresh audit; the restored-camera 1F directed view shows these current assets. |
| Separate glass/water/emission | Transparent gull warning, glass, water, emission, UI shaders and original outline actors preserved. |
| Reuse good models, generate only if needed | No new AI model or paid generation. Ten owned FlatKit material copies replace 68 remaining opaque Lit material slots (Ch1 8, Ch3 1, Ch4 59). Geometry, source textures and original materials remain. |
| Preserve collisions/rigs/route and compare fairly | Protected serialized components, physical ancestor transforms and skinned mesh/bone references total 14,915 entries across five scenes. Hashes match before/after material edits, after reopen and after account UI installation. Initial same-pose Edit renders are retained; the occluded Ch5 Edit start image is excluded from acceptance. Four later native 1F samples are explicit directed fixtures, not route clears. |

`style/before.json`, `style/applied.json`, `final-world-audit/before.json` and native PNGs contain the evidence. Pre-change scene copies are `style/*.before.unity`. The saved camera, lights, meshes, rig, collision, routes, rewards and combat properties match. Existing actor expressiveness, distant repetition, shallow displays and real-device performance are not certified by this bounded pass.

### Account and UI

- Installed official `com.google.play.games` 2.2.1 through the existing Unity Pipeline/UPM path. No legacy Unity bridge or extra database was added.
- Added validated player-bound progression, account-isolated backups, local/cloud and SDK conflicts, repeated fingerprint comparison before commit, lobby eligibility, delayed-read/late-callback handling and restart-safe deletion journaling.
- The native deletion adapter waits for the delete Task, checks freshly loaded slot absence and rejects stale metadata. Native string/object callback types and IL2CPP-preserved methods were reviewed and compiled; actual JNI execution remains unverified.
- All five current scenes include login/sync, status, account deletion and confirmation/conflict UI. UI uses a private font/atlas and existing navy/ivory panel art. New run startup waits for account transitions and records `chapter_last_played`.
- Options and deletion confirmation were visually inspected at 1080×2340 and 1080×1440. Middle-dot glyph boxes were corrected to commas/slashes. Destructive confirmation has a distinct warm tint; cancel was executed, deletion was never executed against a real account.
- The minimized GameView backbuffer was stale even after state changed. It is retained as excluded evidence. Accepted UI PNGs are native Play camera renders with temporary overlay routing and immediate camera/canvas restoration, not physical-screen captures.

### Verification

- `dotnet build Assembly-CSharp.csproj -nologo`: zero errors, two existing warnings.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`: zero errors, zero warnings.
- `tools/validate-agent-harness.ps1`: passes.
- `GameProgressSnapshotTests`: 15/15 passes; `PlayAccountServiceTests`: 13/13 passes. Serialized local backup, a new cloud revision while the choice is visible, and late conflict callbacks are included. These are mock/backend-local checks, not real Google sign-in or server deletion.
- Android native adapter branch: separate Pipeline dry-run compiles with no diagnostics; no execution or Android player build is implied.
- Current editor console: zero captured errors after final checks. Original ShoeTower is restored in clean Edit Mode. No commit, push or phone installation occurred in this task.
- [Gallery](http://127.0.0.1:18886/): nine inspected native PNGs, served bytes verified against originals. Full-resolution copies are also under `tmp/image-previews/essential-proposals-2026-10-06/`.

### Preference verification correction

The first ephemeral `JsonUtility` fixture snapshot wrote `{}` despite returning count 89, and restoration failed before writing preferences. The test backend had introduced three new account keys; only those known preview-owned keys and its matching cache were removed. Wallet values stayed 33,031 coins / 30 jewels. Exact restoration of every original key from that first run is not provable; no historical snapshot was borrowed and no missing values were guessed. The empty file is preserved.

The corrected reflection-based JSON fixture round-trips its actual data before use. A new baseline was captured after that recovery; the second UI/mall fixture restored all 89 captured keys, including absence, with zero mismatches (`ui-preview-restoration.json`). This later baseline is not misrepresented as the missing first one. The ce-compound headless learning is [the round-trip gate](../../solutions/workflow-issues/round-trip-check-preference-snapshots-before-unity-play-2026-10-06.md).

## Remaining external prerequisite

The real Play Games Services game ID, Android OAuth/package/signer linkage, Saved Games enablement and test audience must be configured in Play Console. Current package is `com.mzkoreagames.tralaleroshooter`; existing signing identity was left intact. The user was asked for the existing Games ID/console link. No ID was fabricated from the Firebase project.

Until that configuration is supplied, real account authentication/cloud save/delete and 500,000-DAU quota suitability remain incomplete. The editor helper and [configuration guide](../../google-play-account-setup.md) make the remaining step concrete. Storage is currently documented by Google as free with 3 MB per slot; unlimited lifetime/capacity or guaranteed traffic is not claimed. The user requested implementation; all available local implementation and review work is complete, while production activation is pending this external input.

Final browser inventory exposed no enabled browser/app surfaces, so an existing authenticated Play Console session could not be inspected through the available UI tools. The configuration prerequisite is concrete, not an approval stop. `final-summary.json` verifies 28 passing tests, native-branch compilation, corrected 89-key restoration, five protected-state matches, one installed account panel per scene and the gallery's HTTP 200 response.

## Follow-up: user requests game service ID linking

The user asked “이거 게임 서비스 ID 연동하고 싶은데”. The official Unity Pipeline is reachable and `PlayAccountSetup.Open()` opened the existing local configuration window. `PlayGamesSettings.mAppId` is still empty; no fabricated ID was applied. The Android package remains `com.mzkoreagames.tralaleroshooter`, and its existing Firebase project is `tralaleroshooter`.

Chrome became available during this follow-up. Opening the supplied policy URL led to the current signed-in developer account, not the developer account in that URL. The two cached Google identities expose different developer accounts: one has identity/merchant verification failures with app creation disabled, and the other is shown as closed for inactivity in March 2024. Neither matches the supplied developer account ending `9850`. Searching the first account for the exact game package produced no result. No app, Games project, OAuth credential, publication, payment or account verification was created or modified.

A Google sign-in page for Play Console was opened and retained for user handoff. Required input is the Google identity owning the intended usable developer account, or the already-existing real Games application ID. The local ID/manifest application and server verification remain pending that input; the cached disabled accounts must not be substituted. Plugin discovery yielded no relevant direct Play Console connector. [Account-selection lesson](../../solutions/workflow-issues/verify-play-console-account-before-linking-game-service-2026-10-06.md).

### Correct publisher reached; Cloud ownership checkpoint

The user signed into the intended `mzkoreagames` developer account and approved the app's developer-policy/export-law declarations. The new **Tralalero Shooter** app draft now exists under that publisher, app ID `4974755087251654771`, package `com.mzkoreagames.tralaleroshooter`, Korean/game/free. It is a draft, not a production release. The exact locally installed APK signer was verified from the existing October 5 release APK; public signer references are in `outputs/play-games-link-2026-10-06/local-signing-reference.json`. No signing key/password was read or changed.

The existing Firebase/Cloud project `tralaleroshooter` is owned by a different cached Google identity. The publisher identity `mzkoreagames@gmail.com` has no selectable owned Cloud project in the PGS configuration page. The live IAM table confirms the existing human owner and service-agent roles; a broad project-owner grant has not been saved.

User steering: “mzkoreagames 이걸로 하고 힢은데?” The publisher remains **mzkoreagames**. To use that same identity with the existing Firebase project, the IAM grant form is prepared for `mzkoreagames@gmail.com`, role **Owner**, preserving the existing owner. An action-time permission question explains that this grants management access to the existing Firebase/analytics resources as well. This is pending explicit approval or direct user completion. No substitute Cloud project or Games ID has been fabricated. After approval: verify the new role, refresh PGS's Cloud list, select the existing project, obtain approval for any newly presented service terms/credential creation, then apply the actual Games ID through the official Unity setup helper.

### Account inactivity warning checked

The user's screenshot shows the same `mzkoreagames` publisher at risk of inactivity closure, with a displayed action deadline of **October 31**. Live account details show both contact email and contact phone as **verified**. No identity/contact/payment information was edited and no verification code was requested or submitted.

The remaining documented account-retention step is an actual new app/update artifact upload. The new Tralalero Shooter entry is still a draft; creation alone does not certify that step. Google's [inactive-account guide](https://support.google.com/googleplay/android-developer/answer/11605267?hl=ko) explicitly allows internal app sharing/internal testing/closed testing before general release. The authenticated internal-app-sharing page was opened under `mzkoreagames`; it presents its own agreement/age declaration before upload. No agreement was accepted and no artifact was uploaded. Do not claim the warning resolved or promise an immediate disappearance without console confirmation. Account-retention upload and PGS/Firebase ownership linking are separate pending checkpoints.

### Latest account steering overrides the pending IAM grant

User says “dlwlgud1dlwlgud1 말고”. The unsaved IAM Owner grant was canceled, with no role saved. Google Cloud was switched through the visible account menu to `mzkoreagames@gmail.com`; the live page confirms that identity. Do not resume the rejected first-account workflow or infer approval from its older pending question.

This identity has not completed Google Cloud first-use enrollment. The current welcome dialog requires agreement to Google Cloud Platform and related service/API terms, with Korea selected; marketing email is unchecked. A separate action-time confirmation asks whether to accept those terms and declare commercial game-operation use. No enrollment agreement, new Cloud project, API credential, billing activation, or existing Firebase configuration change has been performed. The intended continuation is an mzkoreagames-owned game-service setup; existing analytics data/configuration must be handled explicitly rather than silently replaced.

### Google Cloud enrollment approved and project created

User says “승인 눌렀어”. The first-use welcome dialog was completed, and the live page identifies `mzkoreagames@gmail.com`. Created `TralaleroShooter-PGS`, Cloud project ID **mzkoreagames-tralalero-pgs**, under that account. The completed creation notification and IAM table confirm that mzkoreagames is its sole human Owner. No free-trial enrollment, billing-account link, old-project IAM grant or old analytics configuration change was performed.

The new project is now visible and selected in the Tralalero Shooter PGS setup page. The final **Use** action explicitly accepts the Play Games Services, Google Cloud Platform and Google API terms. A separate question names those three agreements and requests action-time approval; the project selection is prepared but Use has not been clicked by the agent. No real Games ID or Android OAuth client exists yet, and Unity's AppId remains empty. Continue from this exact selected project after approval or direct user completion.

### PGS created by direct user action; billing checked

The live PGS configuration summary now confirms a created project and shows actual Games ID **1024030137281**, backed by `mzkoreagames-tralalero-pgs`. OAuth consent/client setup is still incomplete and Saved Games is disabled. The project is not published. Do not confuse the Store app ID with this Games ID.

At the user's pricing question, the project's live Billing page explicitly states **no billing account is linked**. The $300 banner is an optional 90-day Google Cloud trial offer, not a cost already incurred. No trial signup, paid upgrade or billing link was performed by the agent. PGS Saved Games storage is separately documented as no-charge with a 3 MB per-file limit; the trial credit is not the proposed game-save funding mechanism.

`tools/configure-play-account.cs` was prepared to back up settings/manifest, invoke the existing official SDK setup helper, and verify AppId. The application attempt was rejected by its Edit Mode guard because Unity is now in a user-controlled Play session in Noryangjin; no setting or manifest was changed. `unity-game-id-apply.json` records the failed guarded attempt. Apply the verified ID only after the user's Play session ends; do not stop their active run or claim the ID is already installed.

## Correction — 2026-10-06 evening

The DOCX applied above was the 4 October toon-style report (its extracted text is in `source-text.txt`). The current `Downloads/트랄랄레오_슈터_필수_제안_문서.docx` is the 15-item gameplay proposal, which was not applied by this plan. It is tracked in [essential-15-proposals-2026-10-06.md](essential-15-proposals-2026-10-06.md).

## Explicit settings save follow-up (2026-10-06)

- Added runtime Save Now button to the existing Options account panel across all five installed scenes. Device flush timestamp and cloud status are separate. Guest and mid-run saves stay local; authenticated lobby saves request reconciliation. Deletion journals/conflicts prevent unsafe saves.
- Added chapter 1-5 best-progress keys to cloud snapshots and validated 0..1 bounds.
- Applied Games app ID `1024030137281` through the official SDK helper. Enabled Saved Games in the mzkoreagames project; console confirmed changes saved and Google Drive API enabled. OAuth consent, Android credentials and real-device verification remain pending. No billing was enabled.
- Verification: editor project build passed (0 errors, 2 pre-existing warnings); PlayAccountServiceTests 17/17 and GameProgressSnapshotTests 19/19 passed. Receipts are under `outputs/play-games-link-2026-10-06`. The first shared-runner receipt belonged to another suite; it was replaced only after checking all test identities.
- Isolated EditMode previews at 1080x2340 and 1080x1440 show three readable account buttons and separate status lines. Sample status is not real save/auth evidence. Current scene and preferences were not used as a manual test fixture. No updated APK has been built or deployed for this follow-up.
- Reusable review lesson: [save coverage and shared test identity](../../solutions/workflow-issues/verify-save-fields-and-test-identity-2026-10-06.md).
