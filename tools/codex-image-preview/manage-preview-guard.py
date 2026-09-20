"""Install/status/restore a version-pinned, reversible Codex webview safeguard."""
import argparse
import hashlib
import json
from pathlib import Path

VERSION = "26.908.40401"
ASSET = "codex-local-image-preview-guard.js"
TAG = f'    <script src="./{ASSET}"></script>\n'


def digest(data):
    return hashlib.sha256(data).hexdigest()


def manage(action, extension, backup_root):
    package = json.loads((extension / "package.json").read_text(encoding="utf-8"))
    if package.get("publisher") != "openai" or package.get("name") != "chatgpt":
        raise ValueError("Not the OpenAI Codex extension")
    if package["version"] != VERSION:
        raise ValueError("Unverified extension version; inspect the new image handlers first")
    webview = extension / "webview"
    index = webview / "index.html"
    asset = webview / ASSET
    backup = backup_root / extension.name
    manifest = backup / "manifest.json"
    content = index.read_bytes()
    script = Path(__file__).with_name("preview-guard.js").read_bytes()
    if action == "status":
        print(json.dumps({"extension": str(extension), "version": VERSION,
                          "installed": TAG.encode() in content,
                          "script_matches": asset.exists() and asset.read_bytes() == script,
                          "backup": str(backup)}, indent=2))
        return
    if action == "restore":
        state = json.loads(manifest.read_text(encoding="utf-8"))
        if digest(content) != state["patched_sha256"]:
            raise ValueError("index.html changed after installation; refusing to overwrite it")
        original = (backup / "index.html").read_bytes()
        if digest(original) != state["original_sha256"]:
            raise ValueError("Backup checksum mismatch")
        index.write_bytes(original)
        # Keep the unused script and backup: restoration never deletes user files.
        print("Restored original index.html. Reopen the Codex view to apply.")
        return
    if TAG.encode() in content:
        if not asset.exists() or asset.read_bytes() != script:
            raise ValueError("Existing guard differs; inspect it before replacing")
        print("Guard already installed; no changes.")
        return
    anchor = b'    <script type="module" crossorigin src="./assets/index-8e8701a2aad8.js"></script>'
    if content.count(anchor) != 1:
        raise ValueError("Unexpected entry point; refusing to patch")
    if asset.exists() and asset.read_bytes() != script:
        raise ValueError("A different file already occupies the guard path")
    patched = content.replace(anchor, TAG.encode() + anchor)
    state = {"version": VERSION, "extension": str(extension),
             "original_sha256": digest(content), "patched_sha256": digest(patched),
             "script_sha256": digest(script)}
    if manifest.exists():
        prior = json.loads(manifest.read_text(encoding="utf-8"))
        if prior != state or (backup / "index.html").read_bytes() != content:
            raise ValueError("An existing backup belongs to different contents")
    else:
        backup.mkdir(parents=True, exist_ok=True)
        with (backup / "index.html").open("xb") as stream:
            stream.write(content)
        with manifest.open("x", encoding="utf-8") as stream:
            json.dump(state, stream, indent=2)
    asset.write_bytes(script)
    index.write_bytes(patched)
    print(f"Installed guard. Backup: {backup}. Reopen the Codex view to apply.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["install", "status", "restore"])
    parser.add_argument("--extension", type=Path, default=Path.home() / ".vscode" / "extensions"
                        / f"openai.chatgpt-{VERSION}-win32-x64")
    parser.add_argument("--backup-root", type=Path,
                        default=Path.home() / ".codex" / "backups" / "image-preview-guard")
    args = parser.parse_args()
    manage(args.action, args.extension.resolve(), args.backup_root.resolve())
