from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'tmp/image-previews/harbor-opening-refinement-2026-09-16';p.mkdir(parents=True,exist_ok=True)
fonts=[('KERIS',root/'Assets/JH/Font/KERISKEDU_B.ttf'),('Gmarket Bold',next((root/'Assets').rglob('GmarketSansTTFBold.ttf'))),('Jigmo CC0',root/'Assets/ShooterSurvival/Fonts/JigmoGameUI/JigmoGameUI.ttf')]
im=Image.new('RGB',(1300,690),'#fff6df');d=ImageDraw.Draw(im)
for i,(name,file) in enumerate(fonts):
 y=i*230;d.text((25,y+10),name,fill='#102548');d.text((25,y+45),'노량진 수산시장  설정  체력 500',font=ImageFont.truetype(str(file),62),fill='#0d2545');d.text((25,y+135),'사운드  좌우 조작 감도  공격력 +5%',font=ImageFont.truetype(str(file),38),fill='#0d2545')
im.save(p/'font-comparison.png')
