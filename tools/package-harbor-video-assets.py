"""Verify and package final A/B video UI source assets without overwriting a release."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib
import json
import tarfile
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "map-concepts/harbor-video-ui-2026-09-15/production"
ART = ROOT / "Assets/ShooterSurvival/UI/HarborVideo"
PREVIEW = ROOT / "tmp/image-previews/harbor-video-ui-2026-09-15/production"
OUTPUT = WORK / "harbor-video-ui-AB-assets-v2.zip"
if OUTPUT.exists():
    raise FileExistsError("Preserve the previous package; choose a new version before packaging")

manifest = json.loads((WORK / "sprite-manifest.json").read_text(encoding="utf-8"))
assert len(manifest["sprites"]) == 26
for row in manifest["sprites"]:
    image = Image.open(ART / row["file"])
    assert image.mode == "RGBA" and image.size == (row["width"], row["height"]), row["name"]

# Prevent exporting stale prefabs after an Editor domain reload or partial build.
verified_native_assets = 0
with tarfile.open(WORK / "HarborVideoUI_AB_v2.unitypackage", "r:gz") as package:
    names = set(package.getnames())
    for entry in package.getmembers():
        if not entry.name.endswith("/pathname"):
            continue
        path = package.extractfile(entry).read().decode("utf-8").strip()
        asset = entry.name.rsplit("/", 1)[0] + "/asset"
        if asset not in names or not (ROOT / path).is_file():
            continue
        assert hashlib.sha256(package.extractfile(asset).read()).digest() == hashlib.sha256((ROOT / path).read_bytes()).digest(), path
        verified_native_assets += 1
assert verified_native_assets == 28, verified_native_assets

with ZipFile(OUTPUT, "w", ZIP_DEFLATED, compresslevel=6) as archive:
    archive.writestr("README.md", "# Harbor video UI A/B asset kit\n\nOpen `map-concepts/harbor-video-ui-2026-09-15/production/README.md` for the asset inventory, source/reconstruction notes and playback hookup map.\n\nPNG/meta files and two presentation prefabs: `Assets/ShooterSurvival/UI/HarborVideo/`.\n\nTwo layered PSDs and all generation sources are included. Unity labels are editable TMP components; PSD labels are separate raster layers. The existing KERIS SDF font is a project dependency.\n")
    for folder in [ART, WORK]:
        for source in sorted(folder.rglob("*")):
            if not source.is_file() or source == OUTPUT or source.suffix in (".unitypackage", ".zip") or "evidence" in source.relative_to(folder).parts:
                continue
            archive.write(source, source.relative_to(ROOT))
    for source in sorted(PREVIEW.glob("*.png")):
        archive.write(source, source.relative_to(ROOT))
    for relative in ["tools/prepare-harbor-video-assets.py", "tools/extract-harbor-ui-sprites.py", "tools/render-harbor-video-prefabs.cs", "tools/package-harbor-video-assets.py", "Assets/ShooterSurvival/Editor/HarborVideoAssetBuilder.cs", "Assets/ShooterSurvival/Editor/HarborVideoAssetBuilder.cs.meta"]:
        archive.write(ROOT / relative, relative)
with ZipFile(OUTPUT) as archive:
    assert archive.testzip() is None
    report = {"zip": str(OUTPUT), "bytes": OUTPUT.stat().st_size, "files": len(archive.namelist()), "verified_native_assets": verified_native_assets}
(WORK / "package-verification-v2.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report))
