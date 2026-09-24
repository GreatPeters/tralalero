"""Read-only evidence of the installed rembg preprocessing on one task input."""
import argparse,json
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from rembg import remove,new_session

parser=argparse.ArgumentParser();parser.add_argument('--id',required=True);args=parser.parse_args()
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/reststop-production-2026-09-24'
assert (Path.home()/'.u2net/u2net.onnx').is_file(),'Do not download a diagnostic model'
source=OUT/'inputs'/(args.id+'.png');dest=OUT/'reviews'/(args.id+'-preprocess')
assert not dest.exists(),'Preserve prior diagnostic evidence'
dest.mkdir();original=Image.open(source).convert('RGBA')
session=new_session('u2net',providers=['CPUExecutionProvider']);processed=remove(original,session=session)
processed.save(dest/'rembg.png')
cells=[]
for name,picture in [('Original alpha',original),('After existing rembg',processed)]:
    for color in ('white','black'):
        tile=Image.new('RGBA',picture.size,color);tile.alpha_composite(picture)
        tile=tile.convert('RGB');tile.thumbnail((600,600),Image.Resampling.LANCZOS);cells.append((name+' on '+color,tile))
sheet=Image.new('RGB',(1200,1260),(232,235,229));draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
for i,(name,tile) in enumerate(cells):
    x=(i//2)*600;y=(i%2)*630;draw.rectangle((x,y,x+600,y+30),fill=(36,57,44));draw.text((x+10,y+4),name,fill='white',font=font);sheet.paste(tile,(x+(600-tile.width)//2,y+30))
sheet.save(dest/'comparison.png')
scale=min(1,1024/max(processed.size));image=processed.resize((int(processed.width*scale),int(processed.height*scale)),Image.Resampling.LANCZOS)
array=np.asarray(image);where=np.argwhere(array[:,:,3]>.8*255);lo=where.min(axis=0);hi=where.max(axis=0);cx=(lo[1]+hi[1])/2;cy=(lo[0]+hi[0])/2;size=int(max(hi-lo))
image=image.crop((cx-size//2,cy-size//2,cx+size//2,cy+size//2));array=np.asarray(image).astype(np.float32)/255
rgb=Image.fromarray((array[:,:,:3]*array[:,:,3:4]*255).astype(np.uint8));padded=Image.new('RGB',(rgb.width+20,rgb.height+20),'black');padded.paste(rgb,(10,10));padded.save(dest/'actual-node-equivalent.png')
(dest/'receipt.json').write_text(json.dumps({'input':str(source),'diagnostic_only':True,'input_unchanged':True,'session':'cached u2net CPU','configured_remove_background':True,'max_size':1024,'padding':10},indent=2),encoding='utf8')
print(json.dumps({'folder':str(dest),'sheet':str(dest/'comparison.png'),'actual_preprocessed':str(dest/'actual-node-equivalent.png')}),flush=True)
