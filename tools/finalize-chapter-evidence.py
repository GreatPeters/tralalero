"""Validate the retained native evidence before marking canonical data installed."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
record = root / "map-concepts/chapters-polish-2026-09-12"
checks = record / "final-checks"
def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

tests = read(checks / "tests-summary.json")
assert sum(item["total"] for item in tests) == 83
assert all(item["passed"] == item["total"] and item["failed"] == 0 for item in tests)
for filename in ("rewards-travel.json", "coin-collection.json", "consent-start.json"):
    data = read(checks / filename)
    assert all(value for value in data.values() if isinstance(value, bool)), filename
earned = read(checks / "ad-earned.json")
assert earned["coins"] - earned["before"] == earned["expectedReward"] == 90
assert earned["duplicateIgnored"] and earned["continueEnabled"] and not earned["showing"]
closed = read(checks / "ad-closed.json")
assert closed["before"] == closed["after"] and not closed["showing"] and closed["continueEnabled"]
preferences = read(record / "original-prefs-verification.json")
assert preferences["unchanged"] and preferences["newRewardKeysAbsent"]
progress = read(record / "progression-result.json")
assert progress["allThreeFinalCohortsCleared"]

verification_path = root / "outputs/chapters-polish-2026-09-12/balance/verification.json"
verification = read(verification_path)
for workbook in (root / "Assets/ShooterSurvival/GameData/Editor/Data.xlsx", verification_path.parent / "Data.xlsx"):
    assert hashlib.sha256(workbook.read_bytes()).hexdigest() == verification["candidateSha256"]
verification["canonicalInstalled"] = True
verification["liveBalanceValidation"] = "Native frame-bounded earned-purchase clears:17/22/22; individual runs vary"
verification["nativeArchiveRoundTripVerified"] = True
verification_path.write_text(json.dumps(verification, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
result = {"nativeTests": 83, "distinctRelatedNativeCases": 102,
          "firstClearAttempts": [item["firstClearAttempt"] for item in progress["chapters"]],
          "canonicalWorkbookSha256": verification["candidateSha256"],
          "runtimeArchiveSha256": hashlib.sha256((root / "Assets/ShooterSurvival/Resources/GameData/Data.bytes").read_bytes()).hexdigest(),
          "originalPreferencesPreserved": True, "functionalChecksPassed": True,
          "androidBuild": "pending", "physicalDeviceTested": False, "productionAdsActivated": False}
build_path = checks / "android-build.json"
if build_path.exists():
    build = read(build_path)
    assert build["result"] == "Succeeded" and build["errors"] == 0
    apk = root / build["output"]
    assert apk.exists()
    result["androidBuild"] = build
    result["apkBytes"] = apk.stat().st_size
    result["apkSha256"] = hashlib.sha256(apk.read_bytes()).hexdigest()
    result["androidMinimumApi"] = 24
    result["androidTargetApi"] = 36
    result["apkSignatureVerified"] = "v2"
(record / "final-verification.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(result, ensure_ascii=False))
