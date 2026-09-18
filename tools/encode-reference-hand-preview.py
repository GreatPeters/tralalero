from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[1]/'tmp/image-previews/harbor-reference-fidelity-2026-09-17'
paths=sorted((root/'hand-motion-frames').glob('*.png'))
assert len(paths)==32, len(paths)
frames=[]
for path in paths:
    with Image.open(path) as image:
        frames.append(image.convert('RGB').resize((432,936),Image.Resampling.LANCZOS).quantize(colors=128))
target=root/'start-hand-motion.gif'
assert not target.exists(), 'Preserve previous animation'
frames[0].save(target,save_all=True,append_images=frames[1:],duration=100,loop=0,optimize=True)
print(target)
