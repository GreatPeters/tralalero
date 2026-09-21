from pathlib import Path
import json, urllib.request, concurrent.futures
root=Path(__file__).resolve().parents[2]
out=root/"tmp/image-previews/runner-references-2026-09-19"
apps=[("6450103712","gun-clone","gb"),("6448931411","reload-rush","us"),("6447461923","weapon-master-gun-shooter-run","us"),("1615819431","merge-grabber","us"),("6448786147","last-war-survival","us"),("1151220243","into-the-dead-2-zombie-killer","us")]
def get(app):
    appid,slug,country=app
    with urllib.request.urlopen(f"https://itunes.apple.com/lookup?id={appid}&country={country}",timeout=30) as r:d=json.load(r)["results"][0]
    urls=d.get("screenshotUrls") or d.get("ipadScreenshotUrls",[])
    records=[]
    for i,url in enumerate(urls[:3]):
        file=f"{slug}-{i+1:02}{Path(url).suffix}"
        path=out/file
        if not path.exists():
            with urllib.request.urlopen(url,timeout=30) as r:path.write_bytes(r.read())
        records.append({"game":d["trackName"],"slug":slug,"file":file,"url":url,"source":f"https://apps.apple.com/{country}/app/{slug}/id{appid}","type":"Developer App Store screenshot / promotional image"})
    return records
records=[]
with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:
    for rows in pool.map(get,apps):records.extend(rows)
Path(__file__).with_name("sources-more.json").write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding="utf-8")
print("\n".join(x["file"] for x in records))
print("New reference images:",len(records))

