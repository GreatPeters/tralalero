"""Subset the author's CC0 Jigmo font for the Korean game UI."""
from hashlib import sha1, sha256
from pathlib import Path
from zipfile import ZipFile
import io
import json
import sys

root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root/'tmp/chapter-fonts/deps'))
from fontTools import subset
from fontTools.ttLib import TTFont

archive=root/'tmp/chapter-fonts/Jigmo-20250912.zip'
with ZipFile(archive) as package:
    license_text=package.read('LICENSE.txt').decode('utf-8')
    if 'CC0 1.0 Universal' not in license_text:raise RuntimeError('Unexpected font license')
    font=TTFont(io.BytesIO(package.read('Jigmo.ttf')))
    cmap=font.getBestCmap()
    if sum(0xAC00<=code<=0xD7A3 for code in cmap)!=11172:raise RuntimeError('Incomplete Hangul coverage')
    # Retain non-Han characters, including all Hangul, Latin, punctuation and arrows.
    keep=[code for code in cmap if not (0x3400<=code<=0x9FFF or 0xF900<=code<=0xFAFF or code>=0x20000)]
    options=subset.Options();options.name_IDs=['*'];options.name_languages=['*']
    worker=subset.Subsetter(options=options);worker.populate(unicodes=keep);worker.subset(font)
    names={1:'Jigmo Game UI',2:'Regular',4:'Jigmo Game UI Regular',6:'JigmoGameUI-Regular'}
    for record in font['name'].names:
        if record.nameID in names:record.string=names[record.nameID].encode(record.getEncoding())
    destination=root/'Assets/ShooterSurvival/Fonts/JigmoGameUI';destination.mkdir(parents=True,exist_ok=True)
    target=destination/'JigmoGameUI.ttf';font.save(target)
    (destination/'LICENSE.txt').write_text(license_text.replace('\r\r\n','\n'),encoding='utf-8')
    report={'source':'https://kamichikoichi.github.io/jigmo/','download':'https://kamichikoichi.github.io/jigmo/Jigmo-20250912.zip',
            'license':'CC0-1.0','archiveSha256':sha256(archive.read_bytes()).hexdigest(),'archiveSha1':sha1(archive.read_bytes()).hexdigest(),
            'fontSha256':sha256(target.read_bytes()).hexdigest(),'hangulSyllables':11172,'retainedCharacters':len(font.getBestCmap()),'fontBytes':target.stat().st_size,
            'modifications':'Subset removes Han ideographs; family renamed Jigmo Game UI. Original character outlines retained.'}
    (destination/'PROVENANCE.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report),flush=True)
