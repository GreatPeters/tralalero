"""Verify and package the native-resolution UI kit and its actual-screen evidence."""
from pathlib import Path
import hashlib
import json
import zipfile
from PIL import Image, ImageDraw, ImageFont
from psd_tools import PSDImage

root=Path(__file__).resolve().parents[1]
work=root/"map-concepts/coastal-enamel-ui-2026-09-16"
preview=root/"tmp/image-previews/coastal-enamel-ui-2026-09-16"
art=root/"Assets/ShooterSurvival/UI/CoastalEnamel"
entries=json.loads((work/"sprite-manifest.json").read_text(encoding="utf-8"))
psd=PSDImage.open(work/"CoastalEnamel-Sprites.psd")
assert len(psd)==len(entries)==46
for entry,layer in zip(entries,psd):
    with Image.open(art/(entry["name"]+".png")) as image:
        image.verify()
    assert layer.width==entry["width"] and layer.height==entry["height"],entry["name"]
shots=[]
for path in sorted((preview/"final").rglob("*.png")):
    with Image.open(path) as image:
        size=image.size
        image.verify()
    shots.append(dict(path=path.relative_to(root).as_posix(),size=size,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
assert len(shots)>=60,len(shots)
for name in ("Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"):
    assert "COMPLETE" in (preview/"final"/name/"checks.txt").read_text()

labels=[("02-lobby","로비"),("12-hud","전투 HUD"),("03-upgrades","상시 강화"),("05-chapter-upgrades","챕터 강화"),
        ("06-skins","꾸미기"),("13-run-settings","설정"),("15-result","결과"),("01-story","영상")]
sheet=Image.new("RGB",(1480,1680),"#102346")
draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf",24)
for index,(file,label) in enumerate(labels):
    image=Image.open(preview/"final/Noryangjin_MapTool_Mode_SR18"/(file+".png"))
    image.thumbnail((340,770))
    x=index%4*370+15;y=index//4*840+50
    draw.text((x,y-35),label,font=font,fill="white")
    sheet.paste(image,(x,y))
sheet.save(preview/"actual-ui-overview.png")
with zipfile.ZipFile(work/"CoastalEnamel-UI-kit.zip","w",zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(art.glob("*.png")):
        archive.write(path,"PNG/"+path.name)
    for name in ("CoastalEnamel-Sprites.psd","sprite-manifest.json","README.md"):
        archive.write(work/name,name)
(work/"artifact-verification.json").write_text(json.dumps(dict(sprites=46,psd_layers=46,native_resolution_layers=True,actual_screens=shots),indent=2),encoding="utf-8")
print(json.dumps(dict(sprites=46,psd_layers=46,actual_screens=len(shots),kit_bytes=(work/"CoastalEnamel-UI-kit.zip").stat().st_size)))
