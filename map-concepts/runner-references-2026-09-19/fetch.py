from pathlib import Path
import json, urllib.request, concurrent.futures

root = Path(__file__).resolve().parents[2]
out = root / "tmp/image-previews/runner-references-2026-09-19"
out.mkdir(parents=True, exist_ok=True)
def get_json(url):
    with urllib.request.urlopen(url, timeout=30) as r:
        return json.load(r)
steam = get_json("https://store.steampowered.com/api/appdetails?appids=2495980&l=english")["2495980"]["data"]
records=[]
for item in steam["screenshots"]:
    records.append({"game":"Arrow a Row","slug":"arrow-a-row","file":f'arrow-a-row-{item["id"]+1:02}.jpg',"url":item["path_full"],"source":"https://store.steampowered.com/app/2495980/Arrow_a_Row/","type":"Steam official screenshot"})
apps=[("1672925980","weapon-craft-run"),("1633171432","gun-head-run"),("1562817072","mob-control")]
def app_records(app):
    appid,slug=app
    d=get_json("https://itunes.apple.com/lookup?id="+appid+"&country=us")["results"][0]
    rows=[]
    for i,url in enumerate(d["screenshotUrls"][:4]):
        suffix=Path(url).suffix
        rows.append({"game":d["trackName"],"slug":slug,"file":f"{slug}-{i+1:02}{suffix}","url":url,"source":f"https://apps.apple.com/us/app/{slug}/id{appid}","type":"Developer App Store screenshot / promotional image"})
    return rows
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
    for rows in pool.map(app_records,apps):
        records.extend(rows)
def download(row):
    p=out / row["file"]
    if not p.exists():
        with urllib.request.urlopen(row["url"],timeout=30) as response:
            p.write_bytes(response.read())
    return row["file"]
with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:
    for name in pool.map(download,records):
        print(name)
Path(__file__).with_name("sources.json").write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding="utf-8")
print("Downloaded",len(records),"official store images")

