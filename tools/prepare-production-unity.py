"""Stage the accepted FBXs and exact GLB material maps for native Unity import."""
import io
import json
from pathlib import Path
import shutil
import struct
from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
DELIVERY = ROOT / "outputs/reststop-production-2026-09-24/delivery-r1"
STAGE = ROOT / "outputs/reststop-scene-integration-2026-09-25/staged"


def stage():
    entries = json.loads((DELIVERY / "manifest.json").read_text(encoding="utf-8"))
    for entry in entries:
        aid = entry["id"]
        folder = DELIVERY / entry["folder"]
        rig = aid.startswith("H")
        glb = next((folder / "rigged").glob("*.glb")) if rig else folder / f"{aid}.glb"
        fbx = glb.with_suffix(".fbx")
        raw = glb.read_bytes()
        size = struct.unpack_from("<I", raw, 12)[0]
        doc = json.loads(raw[20:20 + size])
        binary = raw[28 + size:]
        dest = STAGE / aid
        dest.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(fbx, dest / f"{aid}.fbx")

        def texture(info, kind):
            if info is None:
                return None
            index = info["index"]
            image = doc["images"][doc["textures"][index]["source"]]
            view = doc["bufferViews"][image["bufferView"]]
            start = view.get("byteOffset", 0)
            data = binary[start:start + view["byteLength"]]
            name = f"{kind}_{index}.png"
            im = Image.open(io.BytesIO(data))
            if kind == "mask":
                # glTF metal=B, rough=G; Unity metal=R, smooth=A.
                r, g, b = im.convert("RGB").split()
                black = Image.new("L", im.size, 0)
                Image.merge("RGBA", (b, black, black, ImageChops.invert(g))).save(dest / name)
            else:
                im.save(dest / name)
            return name

        materials = []
        for m in doc.get("materials", []):
            pbr = m.get("pbrMetallicRoughness", {})
            materials.append({
                "name": m["name"], "color": pbr.get("baseColorFactor", [1, 1, 1, 1]),
                "metallic": pbr.get("metallicFactor", 1), "roughness": pbr.get("roughnessFactor", 1),
                "alpha": m.get("alphaMode", "OPAQUE"), "cutoff": m.get("alphaCutoff", .5),
                "doubleSided": m.get("doubleSided", False),
                "base": texture(pbr.get("baseColorTexture"), "base"),
                "normal": texture(m.get("normalTexture"), "normal"),
                "normalScale": m.get("normalTexture", {}).get("scale", 1),
                "mask": texture(pbr.get("metallicRoughnessTexture"), "mask"),
            })
        entry.update({"rig": rig, "fbxSource": str(fbx.relative_to(ROOT)), "materials": materials,
                      "clips": [a["name"] for a in doc.get("animations", [])]})
        (dest / "source.json").write_text(json.dumps(entry, indent=2, ensure_ascii=False), encoding="utf-8")
        print(aid, len(materials), "materials", flush=True)
    (STAGE / "catalog.json").write_text(json.dumps(entries, indent=2, ensure_ascii=False), encoding="utf-8")


if __name__ == "__main__":
    stage()
