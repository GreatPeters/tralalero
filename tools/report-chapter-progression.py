"""Summarize native progression CSVs without treating directed tests as normal clears."""
import csv
import json
import os
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
record = root / "map-concepts/chapters-polish-2026-09-12"
evidence = root / "tmp/q/tmp/image-previews/sr18-presentation-progression-2026-09-10"
cohorts = [
    ("노량진", "chapter-polish-final-nory-20260913", "save-for-best-value"),
    ("고속도로", "chapter-polish-final-highway-paced-20260913", "save-for-best-value"),
    ("휴게소", "chapter-polish-final-reststop-confirmed-20260913", "save-for-best-value"),
]
report = []
for name, label, policy in cohorts:
    source = evidence / label / "runs.csv"
    rows = []
    if source.exists():
        with source.open(encoding="utf-8-sig", newline="") as handle:
            for row in csv.DictReader(handle):
                if row.get("outcome") in {"death", "clear"} and row.get("hp_level"):
                    rows.append(row)
    clears = [row for row in rows if row["outcome"] == "clear"]
    if any(not row.get("movement_calls") or int(row["movement_calls"]) > int(row["game_frames"]) for row in rows):
        raise RuntimeError(f"Unverified movement cadence in {label}")
    report.append({"chapter": name, "cohort": label, "purchasePolicy": policy,
                   "firstClearAttempt": int(clears[0]["attempt"]) if clears else None,
                   "completedAttempts": len(rows), "lastRun": rows[-1] if rows else None,
                   "source": str(source.relative_to(root)), "runs": rows})
complete = all(chapter["firstClearAttempt"] is not None for chapter in report)
(record / "progression-result.json").write_text(json.dumps({
    "allThreeFinalCohortsCleared": complete, "powerCheat": False,
    "adsWatchedForProgression": False, "movement": "normal PlayerMove at3xsimulation speed",
    "limits": "At most one movement call per game frame; real earned carryover between scenes. One automated strategy does not guarantee identical results for human purchase/steering choices.",
    "chapters": report,
}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

os.environ.setdefault("MPLCONFIGDIR", str(root / "tmp/chapter-plot-cache"))
sys.path.insert(0, str(root / "tmp/chapter-plot-deps"))
import matplotlib
matplotlib.use("Agg")
from matplotlib import font_manager, pyplot as plt

font_manager.fontManager.addfont(str(root / "Assets/ShooterSurvival/Fonts/JigmoGameUI/JigmoGameUI.ttf"))
plt.rcParams["font.family"] = "Jigmo Game UI"
plt.rcParams["font.weight"] = 500
plt.rcParams["axes.titleweight"] = 500
plt.rcParams["axes.unicode_minus"] = False
fig, axes = plt.subplots(3, 1, figsize=(9, 11), constrained_layout=True)
for axis, chapter, color in zip(axes, report, ["#297c91", "#ba7725", "#558247"]):
    runs = chapter["runs"]
    axis.axhline(300, color="#8c9398", linestyle="--", linewidth=1)
    axis.axvspan(18, 22, color="#e8edf0", zorder=0)
    axis.plot([int(row["attempt"]) for row in runs], [float(row["seconds"]) for row in runs],
              marker="o", markersize=4, linewidth=1.8, color=color)
    status = f"{chapter['firstClearAttempt']}판째 첫 완주" if chapter["firstClearAttempt"] else "측정 진행 중"
    axis.set_title(f"{chapter['chapter']} / {status}", loc="left", fontsize=15)
    axis.set(xlim=(1, max(26, len(runs))), ylim=(0, 320), xlabel="도전 횟수", ylabel="생존 시간 (초)")
    axis.grid(axis="y", color="#e5e9ec", linewidth=.6)
    axis.spines[["top", "right"]].set_visible(False)
fig.suptitle("실제 플레이와 획득 코인으로 확인한 성장" + ("" if complete else " / 진행 중"), fontsize=19)
target = root / "tmp/image-previews/chapters-polish-2026-09-12/progression-verified.png"
version = 2
while target.exists():
    target = target.with_name(f"progression-verified-v{version}.png")
    version += 1
fig.savefig(target, dpi=140)
plt.close(fig)
print(json.dumps({"complete": complete, "firstClears": [r["firstClearAttempt"] for r in report], "preview": str(target)}, ensure_ascii=False))
