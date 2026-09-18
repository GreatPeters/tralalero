from PIL import Image
import numpy as np,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
im=Image.open(root/'Assets/ShooterSurvival/UI/CoastalFaithful/NavigationStrip.png')
alpha=np.asarray(im)[:,:,3];columns=(alpha>128).sum(axis=0)>10
edges=np.flatnonzero(np.diff(np.r_[False,columns,False]));assert len(edges)==6,edges
rows=[]
for name,left,right in zip(['SkinTile','UpgradeTile','StoryTile'],edges[::2],edges[1::2]):
    ys=np.flatnonzero((alpha[:,left:right]>128).any(axis=1))
    rows.append(dict(name=name,x=int(left),y=int(ys[0]),width=int(right-left),height=int(ys[-1]+1-ys[0])))
(root/'map-concepts/harbor-faithful-art-2026-09-17/sources/navigation-slices.json').write_text(json.dumps(dict(items=rows),indent=2))
print(rows)
