# Google Play account and cloud progress setup

The project includes official Google Play Games Unity plugin 2.2.1 pinned to the upstream tag. `PlayAccountService` connects wallet, chapter unlocks/rewards/last-played chapter, permanent/chapter upgrades, tutorial and cosmetic ownership/equipment to the authenticated Play Games player. Device audio/control/consent and Editor debug preferences are not copied to the cloud. There is one save slot, `tralalero_progress_v1`, with a 128 KiB application payload limit.

## Required console configuration

1. Create or select this game's Play Games Services project in Play Console. Enable Saved Games. The real numeric Games application ID is required; the Firebase project number is not interchangeable.
2. Link the Android package and OAuth Android client with the actual release/app-signing SHA-1 and the debug/test signer as appropriate. Use the existing signing configuration; do not change package identity or signing keys to make login work.
3. In Unity, open **Tools → Shooter Survival → Google Play Account Setup**, enter that Games application ID, and apply. The helper uses the official SDK's settings/manifest generator. Server OAuth client secrets are unnecessary for Saved Games and must not be placed in Unity.
4. Register test accounts, publish the required Games configuration to the test audience, and distribute an internal-test build. Verify authentication, two-device save/restore, a guest/cloud conflict, offline recovery, and account deletion with disposable test accounts.

Current 2026-10-06 state: the mzkoreagames Cloud project `mzkoreagames-tralalero-pgs` has Games application ID `1024030137281`, applied through the official Unity setup helper. Saved Games was enabled and the console confirmed the change was saved. OAuth consent and Android credentials remain pending. The code, SDK, five-scene UI and local tests are implemented; real Google authentication/cloud writes/deletion have not been exercised. The Firebase Analytics installation remains separate. Do not claim a production connection from mock tests or an Editor screenshot.

## Storage and scale

Google documents Saved Games storage as free, with 3 MB per save file. The game's bounded progression payload fits well below that. No general database or paid Firebase Auth/Firestore service was added. Firestore's free 50,000 daily reads/20,000 writes would not meet the requested 500,000 DAU even at one read/write per user.

Saved Games is suitable for this per-player cloud-save use case; it is not unlimited storage, a lifetime service guarantee, or proof of support for 500,000 DAU. Review the actual project's API quotas in Google Cloud/Play Console and request increases before that scale. The implementation batches changes, polls local state every five seconds, debounces ten seconds and retries after sixty seconds. Cloud reconciliation runs at a real lobby, not in the middle of a run or its pause. Unchanged snapshots produce no commit.

Sources: [Saved Games](https://developer.android.com/games/pgs/savedgames), [Unity setup](https://developer.android.com/games/pgs/unity/unity-start), [PGS quota management](https://developer.android.com/games/pgs/quota), [Firebase pricing](https://firebase.google.com/pricing).

## Conflict and deletion behavior

No balances or purchases are added across saves. The player chooses one entire progression snapshot when local/cloud versions diverge. A fingerprint comparison is repeated at write time so a newer unseen cloud revision cannot be blindly overwritten after a choice. Account ID/schema/key/type/size checks run before applying remote preferences. Another player's local progress is kept in a separate hashed cache rather than uploaded into the new account.

Options exposes **Google Play 로그인 / 계정 동기화** and **계정 탈퇴**, with a second confirmation. It deletes this game's save slot and local progression/cache; it does not delete the Google account or the general Play Games profile. The official Unity SDK's `Delete` is fire-and-forget, so the adapter waits for the native SnapshotsClient Task and verifies refreshed slot absence, rejects stale metadata, and preserves local state on failure. A persisted deletion journal prevents a restart or retry from re-uploading the old progress. Local deletion is performed only after the remote sequence succeeds. Close this game on other devices before deletion; an unrelated device's old offline save can otherwise recreate state. A universal multi-device revocation guarantee would require an authoritative backend.

The client progress store is not anti-cheat authority. Existing Firebase Analytics/AdMob are not game account databases; deleting progress does not erase Google's retained analytics, ads, Play history or profile data. Production account policy/data-safety disclosures and any required public deletion URL remain console/release work, not certified by this implementation. [Google account deletion guidance](https://support.google.com/googleplay/android-developer/answer/13327111), [Play Games data controls](https://support.google.com/googleplay/answer/9130646).

## Verification and recovery

Run `dotnet build Assembly-CSharp.csproj -nologo`, `dotnet build Assembly-CSharp-Editor.csproj -nologo`, and the narrow Editor classes `GameProgressSnapshotTests` and `PlayAccountServiceTests`. These test serialization/validation, guest preservation, explicit conflicts, write races, failed deletion, deletion journaling, run eligibility and late callbacks through an injected backend. They do not certify Android JNI or real server behavior.

Native UI captures and the five-scene material/protected-state audit are under `outputs/essential-proposals-2026-10-06`. Do not rerun the original full art installers. The account installer is idempotent for already installed panels, but does not redesign existing panels on a second call.

## Explicit save in Options — 2026-10-06

Options now provides **지금 저장**. It flushes existing progression preferences to the device and displays a device-save timestamp separately from the account status. For an authenticated, loaded account in the lobby it also requests cloud reconciliation; during a run it saves only locally. Conflict resolution, busy operations and a pending deletion block this action. A local success never claims a server acknowledgement. The bounded cloud whitelist includes chapter 1–5 best progress, normalized to 0..1.

The layout was rendered in an isolated EditMode preview at 1080×2340 and 1080×1440 with sample status text. This is layout evidence, not an Android sign-in test.
