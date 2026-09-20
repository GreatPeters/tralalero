---
title: Protect Codex image clicks without changing thread state
date: 2026-09-19
category: workflow-issues
module: Codex VS Code image previews
problem_type: workflow_issue
component: assistant
severity: medium
applies_when:
  - Clicking a Codex image leaves the conversation title visible but the body blank
  - A local extension safeguard must remain reversible and version bounded
tags: [codex, vscode, image-preview, blank-conversation, local-safeguard]
---

# Protect Codex image clicks without changing thread state

## Context

Clicking an image left the Codex conversation title visible but the body blank. The PNGs displayed in Chrome. A separate gallery helped access images but did not repair the panel. The normal renderer/Codex logs did not contain a click-time exception, and native VS Code UI control was unavailable.

Read-only inspection of `openai.chatgpt-26.908.40401-win32-x64` found generated thumbnails use `data-testid="generated-image-preview"` and markdown images use `data-markdown-image-preview-trigger="true"`. The generated-image handler can open an editor panel and change full-width/content state. This is a plausible path, **not a confirmed upstream root cause**. Do not represent the isolated browser fixture as reproduction of the vendor defect.

## Guidance

A removable safeguard is installed **on disk**. It captures unmodified primary clicks on the two inspected thumbnail triggers before React receives them, copies the displayed image source into a native modal dialog, and leaves conversation routing, DOM and drafts untouched. Close/Escape restores focus. An image-load failure retains a visible close button. The displayed source may be a lower-resolution generated preview rather than the full original.

- `tools/codex-image-preview/preview-guard.js`: independent click handler/viewer.
- `tools/codex-image-preview/manage-preview-guard.py`: version-pinned install/status/restore.
- `tools/codex-image-preview/preview-guard.test.html`: isolated browser fixture.
- `tools/codex-image-preview/test_manage_preview_guard.py`: installer safety tests.

```powershell
python tools/codex-image-preview/manage-preview-guard.py status
python tools/codex-image-preview/manage-preview-guard.py install
python tools/codex-image-preview/manage-preview-guard.py restore
```

The installer inserts one same-origin script before the original application module in `webview/index.html`; it does not modify bundled vendor JavaScript. The existing CSP permits the script and inline styles; no security policy changes are required. Original bytes and checksums live in `~/.codex/backups/image-preview-guard/<extension-folder>/`. Unexpected versions/entry points, backup conflicts and post-install edits fail closed. Restore does not delete files.

**Activation:** save editor work, then `Ctrl+Shift+P` → `Developer: Reload Window`, and reopen the existing conversation. A currently loaded panel still runs the old code. The on-disk install does not automatically recover an existing blank view.

**Update boundary:** a Codex update may replace the directory and remove the safeguard. Do not disable updates or blindly patch unknown builds. Inspect and test the new version first. This is a local mitigation, not an official upstream fix or a guarantee against every blank-panel cause.

## Why This Matters

An image preview need not change conversation routing. The guard isolates that transition and retains an exit even on image failure. No conversation files, credentials, caches or model context are deleted or rewritten. A working external gallery does not establish that the original panel was repaired.

## When to Apply

- Verify extension version and markup before installing.
- Distinguish thumbnail clicks from text-only PNG links; this guard covers thumbnails. No answer to the clarification had arrived when installation completed.
- Keep original files and a verified external gallery available while native verification is pending.

## Examples and verification

```powershell
node --check tools/codex-image-preview/preview-guard.js
python tools/codex-image-preview/test_manage_preview_guard.py
python -m http.server 6754 --bind 127.0.0.1 --directory tools/codex-image-preview
```

Open `/preview-guard.test.html?baseline=1` and run the checks: the **artificial** routing handler hides the conversation and the relevant assertions fail. Open `/preview-guard.test.html`: **16 checks pass**, covering both trigger types, interception, preserved conversation/draft, image decoding, zoom, close/focus restoration, unrelated controls and disabled triggers. Escape was separately exercised in Chrome and restored the conversation. Visual inspection exposed inherited thumbnail sizing; explicit viewer dimensions corrected it, followed by a complete fixture rerun.

**4 installer tests pass:** byte-exact rollback/idempotent install/reinstall, refusal to overwrite later edits, unknown-version rejection and changed-entrypoint rejection. Live-directory status returned `installed=true` and `script_matches=true`. This verifies on-disk installation. **Actual VS Code image-click verification after Reload Window remains outstanding.** No Unity builds were required.

## Related

- [Preserve generated images](persist-generated-image-before-next-prompt-2026-05-16.md)
- [Ground VFX concepts in reusable parts](ground-vfx-concepts-in-reusable-parts-2026-09-19.md)

Captured with `ce-compound mode:headless`; related local docs reviewed, no additional session scan or GitHub issue search performed.
