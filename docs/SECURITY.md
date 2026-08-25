# Security

## Trust Boundaries
- Unity editor state is mutable and should be treated as an external runtime boundary.
- The official Unity CLI/Pipeline command surface can execute scene, asset, and editor operations. Treat it as privileged.
- Local scripts and docs are authoritative only when committed and versioned.
- The editable game-data workbook is an Editor-only developer input. Its RSA
  private signing key stays outside the project and version control.
- Firebase Analytics client configuration is an external project identifier boundary. BigQuery IAM and service-account credentials stay outside the Unity client and this repository.

## Current Assumptions
- Work is performed on a local developer machine, not an untrusted shared editor host.
- The supported Codex path uses Unity CLI discovery of an authenticated per-editor Pipeline endpoint; it does not store a reusable Pipeline port in project files.
- The separate `com.youngwoocho02.unity-cli-connector` package exposes a localhost HTTP command surface for pre-existing workflows. It is not a Codex MCP registration and remains a distinct legacy trust boundary.
- Runtime debug harnesses are intended for editor or development builds, not production player builds.

## Guardrails
- Do not hardcode ports or environment-sensitive settings in multiple places.
- Prefer explicit recovery instructions over silent fallback behavior.
- Keep privileged editor operations behind documented tools and menu entries where possible.
- Do not restore the removed CoderGamester `mcp-unity` bridge or treat the retained HTTP connector as a Codex MCP fallback.
- Never restore `Data.xlsx` under `Assets/StreamingAssets`; that would ship the raw enemy, upgrade, and character values.
- Player builds consume only the encrypted, RSA-signed `Resources/GameData/Data.bytes` archive. Runtime loading verifies the signature before decryption and fails closed when bytes change.
- Never store `GameDataSigningKey.json` anywhere under `Assets` or elsewhere in
  the repository. Use the external local path or
  `TRALALERO_GAME_DATA_SIGNING_KEY_PATH`.
- Never place a Google Cloud service-account JSON, Google Cloud private key, or
  BigQuery write credential under `Assets`, `GooglePackages`, or any
  player-build input.
- Analytics events contain a pseudonymous Firebase app-instance identifier, game state, and virtual-world coordinates. GA4 export can also include automatic device/app, coarse geography, traffic-source, and consent metadata. Do not describe this dataset as anonymous, and do not add email, phone, real name, exact real-world location, or another direct identifier as an event parameter.

## Game-Data Protection Limit

- The external RSA private key prevents a normal player installation or source
  checkout from producing a newly signed replacement archive. The AES wrapper
  prevents opening the archive merely by renaming it, but its key is necessarily
  present in the player and therefore provides obfuscation, not confidentiality.
- This is client-side tamper resistance, not an absolute anti-cheat boundary. A determined attacker can reverse-engineer or patch the executable and bypass local verification.
- Values that must be impossible for a player to forge require a server-authoritative source and server-side validation. Do not represent the local archive as equivalent to that guarantee.

## Analytics Trust Limit

- `google-services.json` is Firebase client configuration, not an administrator credential, but it must match Android app ID `com.mzkoreagames.tralaleroshooter` and the reviewed project/app destination recorded in `ProjectSettings/FirebaseAnalyticsDestination.json`.
- GitHub secret scanning ignores only the three Firebase-generated client configuration artifacts listed in `.github/secret_scanning.yml`: the source `google-services.json`, generated desktop JSON, and generated Android XML. Do not broaden these exclusions with directories or wildcards.
- Keep the Google Cloud API key restricted to the Firebase APIs used by this client. Firebase Security Rules and App Check remain the authorization and abuse-prevention boundaries; an API key is not authorization.
- Never commit a Google Cloud service-account file or any value containing `private_key`, `client_secret`, or another server credential. Those are not covered by the client-configuration exclusions.
- The Android manifest starts Analytics collection disabled, removes the Advertising ID permission, disables Advertising ID collection, and disables ad-personalization signals. Runtime collection state is persisted; disabling it clears the unsent event queue and unfinished-round checkpoint instead of re-enabling on the next launch.
- A modified client can forge `chapter`, progress, coins, play time, position, upgrade state, and even arbitrary event volume. BigQuery round logs are product analytics, not an authoritative audit ledger.
- Do not use client telemetry by itself to grant rewards, settle the economy, ban users, or make anti-cheat decisions. Those decisions require server-validated state and a server-owned logging path.
- Local pending-event and active-round PlayerPrefs improve delivery after restarts; they are deliberately not treated as trusted game state.
- Before release, expose `FirebaseAnalyticsRuntime.SetCollectionEnabled(...)` through the shipped privacy/consent UI as required by the applicable legal basis and regions. Align collection, retention, deletion, and withdrawal behavior with the privacy notice.
