"""Frame actual equipment renders tightly and cut four matching workshop icons."""
from pathlib import Path
import json
import runpy
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[1]
WORK=ROOT/"map-concepts/harbor-ui-live-2026-09-15"
ART=ROOT/"Assets/ShooterSurvival/UI/HarborWorkshop"
OUT=ART/"EquipmentIcons"
OUT.mkdir(parents=True,exist_ok=True)
sources=json.loads((WORK/"cosmetic-icon-sources.json").read_text(encoding="utf-8"))
report=[]
for row in sources:
    original=Image.open(ROOT/row["path"]).convert("RGBA")
    bounds=original.getchannel("A").point(lambda value:255 if value>12 else 0).getbbox()
    if bounds is None: raise ValueError("Empty equipment image: "+row["id"])
    cropped=original.crop(bounds)
    image=Image.new("RGBA",(cropped.width+24,cropped.height+24));image.paste(cropped,(12,12))
    image.save(OUT/(row["key"]+".png"))
    report.append({"key":row["key"],"source":row["path"],"source_size":original.size,"bounds":bounds,"output_size":image.size})
remove_key=runpy.run_path(str(ROOT/"tools/extract-harbor-ui-sprites.py"))["remove_key"]
sheet=Image.open(WORK/"extra-workshop-icons-magenta.png")
names=["BossBreaker","HealingInsert","SahurShield","BomberCharm"]
for index,name in enumerate(names):
    half=sheet.width//2;x=index%2*half;y=index//2*half
    image=remove_key(sheet.crop((x,y,x+half,y+half)))
    bounds=image.getchannel("A").getbbox();image=image.crop(bounds)
    padded=Image.new("RGBA",(image.width+16,image.height+16));padded.paste(image,(8,8));padded.save(ART/(name+".png"))
(WORK/"icon-framing-verification.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
preview=Image.new("RGB",(960,720),"#fff1d4")
draw=ImageDraw.Draw(preview)
for i,path in enumerate([OUT/"shoes_original.png",OUT/"shoes_ruby.png",OUT/"shoes_mint.png",OUT/"hat_cap.png"]+[ART/(name+".png") for name in names]):
    image=Image.open(path);image.thumbnail((205,285),Image.Resampling.LANCZOS);x=(i%4)*240;y=(i//4)*360
    preview.paste(image,(x+(240-image.width)//2,y+30),image)
    draw.text((x+10,y+325),path.stem,fill="#241409")
dest=ROOT/"tmp/image-previews/harbor-ui-live-2026-09-15/icon-review.png";dest.parent.mkdir(parents=True,exist_ok=True);preview.save(dest)
print(json.dumps({"trimmed_equipment":len(report),"new_workshop_icons":4,"preview":str(dest)}))
