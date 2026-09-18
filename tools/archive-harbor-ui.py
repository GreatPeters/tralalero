"""Bundle the reviewed native PNGs, import metadata and layered source files."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "map-concepts/harbor-ui-production-2026-09-15"
path = WORK / "harbor-ui-asset-kit.zip"
if path.exists():
    raise FileExistsError("Preserve the previous package; choose a new version")
with ZipFile(path, "w", ZIP_DEFLATED, compresslevel=6) as archive:
    for source in sorted((ROOT / "Assets/ShooterSurvival/UI/HarborWorkshop").glob("*")):
        if source.is_file():
            archive.write(source, source.relative_to(ROOT))
    for name in ["README.md", "prompts.md", "sprite-manifest.json"]:
        archive.write(WORK / name, name)
    for source in sorted((WORK / "layered").glob("*")):
        archive.write(source, source.relative_to(WORK))
    for name in ["extract-harbor-ui-sprites.py", "import-harbor-ui-art.cs", "package-harbor-ui.py"]:
        archive.write(ROOT / "tools" / name, "tools/" + name)
    for name in ["ui-v4-main.png", "ui-v4-chapter-and-purchase.png", "start-ui-overlay.png", "start-actual-scene-composite.png", "sprite-alpha-review.png"]:
        archive.write(ROOT / "tmp/image-previews/harbor-ui-production-2026-09-15" / name, "previews/" + name)
with ZipFile(path) as archive:
    assert archive.testzip() is None
    print(f"Verified {len(archive.namelist())} files; {path.stat().st_size:,} bytes; {path}")
