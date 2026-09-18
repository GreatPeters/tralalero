from PIL import Image,ImageFont,ImageDraw
from pathlib import Path
root=Path(__file__).resolve().parents[1]
image=Image.new('RGB',(1100,430),'#fff6df');draw=ImageDraw.Draw(image)
for row,(name,path) in enumerate([('Existing KERIS',root/'Assets/JH/Font/KERISKEDU_B.ttf'),('Jua / SIL OFL',root/'tmp/font-candidates/Jua-Regular.ttf')]):
 y=row*210;draw.text((25,y+10),name,fill='#08133b');font=ImageFont.truetype(str(path),64)
 draw.text((25,y+42),'노량진 수산시장  이야기 01 / 04',font=font,fill='#08133b')
 draw.text((25,y+122),'꾸미기  강화  스토리  다음 장면',font=ImageFont.truetype(str(path),48),fill='#08133b')
image.save(root/'tmp/image-previews/harbor-faithful-art-2026-09-17/rounded-font-comparison.png')
