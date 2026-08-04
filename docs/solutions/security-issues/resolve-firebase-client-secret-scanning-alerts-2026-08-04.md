---
title: Resolve GitHub secret-scanning alerts for Firebase client configuration
date: 2026-08-04
category: security-issues
module: Repository security and Firebase client configuration
problem_type: security_issue
component: development_workflow
symptoms:
  - "GitHub secret scanning opened a Google API Key alert for Assets/google-services.json."
  - "The same Firebase client key appeared in three tracked Firebase client configuration artifacts."
root_cause: config_error
resolution_type: config_change
severity: low
related_components: [tooling, documentation]
tags: [firebase, github-secret-scanning, api-key, client-configuration, path-exclusion, security-validation]
---

# Resolve GitHub secret-scanning alerts for Firebase client configuration

## Problem

GitHub secret scanning reported a Google API key in the Firebase Unity client configuration. The repository needs that configuration to generate the desktop and Android Firebase artifacts, so deleting the file or rotating the key without classifying it first could break builds and installed clients.

The reported value was a Firebase client API key, not an administrator credential. The reviewed files contained no service-account structure, `private_key`, `client_secret`, or privileged identity, and the Google Cloud key was restricted to Firebase-related APIs.

## Symptoms

- GitHub reported a `Google API Key` in `Assets/google-services.json`.
- The same client identifier appeared in the source file and two Firebase-generated artifacts.
- The Firebase Unity build contract required the tracked source configuration to remain available.

## What Didn't Work

- Deleting `google-services.json` would have broken the existing Firebase setup and generated configuration flow.
- Rotating or revoking the key was rejected because it was a public-by-design client identifier embedded in distributed clients, not a privileged credential. Rotation could break existing installations without improving authorization.
- Rewriting Git history was rejected as destructive and ineffective for a client identifier that is intentionally shipped in the application.
- A broad directory or wildcard exclusion was rejected because it could hide a genuine credential added later.
- An Android application restriction using only the local signing certificate was rejected until the release and Google Play signing certificates can also be verified.

If the finding had contained a service-account key, private key, OAuth client secret, or another privileged credential, revocation or rotation and a history-cleanup assessment would have been required.

## Solution

First classify the finding and verify its boundaries without printing the key:

1. Confirm the files are Firebase client configuration only.
2. Confirm no `private_key`, `client_secret`, service-account email, or equivalent privileged field exists.
3. Confirm the Google Cloud key is limited to the Firebase APIs used by the client.
4. Confirm unrelated high-risk APIs are not enabled for that key.

Then configure exact path exclusions in `.github/secret_scanning.yml`:

```yaml
# Firebase client API keys are public application identifiers, not server secrets.
# Authorization still depends on Firebase Security Rules, App Check, and API restrictions.
paths-ignore:
  - "Assets/google-services.json"
  - "Assets/StreamingAssets/google-services-desktop.json"
  - "Assets/Plugins/Android/FirebaseApp.androidlib/res/values/google-services.xml"
```

Keep the configuration on the default branch so it acts as repository policy. Add the same file to active branches that contain the Firebase client artifacts.

The repository harness enforces the exclusion as an exact allowlist. `tools/validate-agent-harness.ps1` fails if the file is missing, a path is added or removed, or a wildcard is introduced:

```powershell
powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1
```

Record the trust boundary in `docs/SECURITY.md`: Firebase Rules and App Check provide authorization and abuse resistance; the client API key does not. Administrative credentials must never be placed in one of the excluded files.

Finally, verify the GitHub alert rather than assuming the configuration worked. Alert `#1` was confirmed through the GitHub API with sanitized output:

```text
state: resolved
resolution: hidden_by_config
```

## Why This Works

Firebase client API keys identify a Firebase project for API routing and quota; they do not independently authorize access to protected Firebase data. Access remains controlled by Firebase Security Rules, authentication, IAM, and optionally App Check.

Exact exclusions suppress the known client-configuration finding without disabling scanning elsewhere. The validation script turns those three paths into a durable contract, so a later broadening of the scanner blind spot fails local and automated verification.

Not adding an Android application restriction yet avoids blocking valid production clients whose release or Google Play signing identities have not been confirmed. That restriction can be added after every shipped signing certificate is known.

## Prevention

- Keep secret-scanning exclusions exact and review every requested expansion.
- Run `tools/validate-agent-harness.ps1` after changing repository security configuration.
- Periodically audit the Google Cloud API restriction list for unrelated APIs.
- Verify release and Google Play signing certificates before adding Android application restrictions.
- Consider Firebase App Check for additional abuse resistance.
- Treat new findings according to credential type: client configuration may be intentionally public; server credentials require immediate containment.
- Verify that GitHub reports `hidden_by_config` instead of relying only on a successful push.

## Related Issues

- [Firebase Analytics and BigQuery setup](../../firebase-analytics-bigquery.md)
- [Repository security boundaries](../../SECURITY.md)
- [Unblock layered Unity Firebase Android player build failures](../build-errors/unblock-layered-unity-firebase-android-player-build-failures-2026-07-31.md)
- [Firebase API key guidance](https://firebase.google.com/docs/projects/api-keys)
- [GitHub secret-scanning path exclusions](https://docs.github.com/en/code-security/how-tos/secure-your-secrets/customize-leak-detection/exclude-folders-and-files)
- [GitHub guidance for removing sensitive data](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)
