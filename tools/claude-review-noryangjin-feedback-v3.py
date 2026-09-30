"""Run the user's requested actual Claude Code review, with read-only tools and saved receipts."""
import json
import subprocess
import time
import sys
from pathlib import Path

repo = Path(__file__).resolve().parents[1]
output = repo / "outputs/noryangjin-feedback-v3-2026-09-28"
binary = Path("C:/Users/ljh/.vscode/extensions/anthropic.claude-code-2.1.283-win32-x64/resources/native-binary/claude.exe")
assert binary.is_file()
followup = "--followup" in sys.argv
prefix = "claude-followup" if followup else "claude"
prompt = (output / ("claude-followup-prompt.txt" if followup else "claude-review-prompt.txt")).read_text(encoding="utf-8")
command = [str(binary), "-p", "--restricted", "--strict-mcp-config", "--mcp-config", str(output / "claude-empty-mcp.json"), "--tools", "Read,Grep,Glob", "--allowedTools", "Read,Grep,Glob", "--permission-mode", "dontAsk", "--output-format", "json"]
if followup:
    command.extend(["--resume", json.loads((output / "claude-stdout.json").read_text(encoding="utf-8"))["session_id"]])
started = time.time()
receipt = {"started_unix": started, "binary": str(binary), "cwd": str(repo), "tools": ["Read", "Grep", "Glob"], "restricted": True, "strict_mcp_config": True}
(output / (prefix + "-launch.json")).write_text(json.dumps(receipt, indent=2), encoding="utf-8")
result = subprocess.run(command, input=prompt, cwd=repo, capture_output=True, encoding="utf-8", errors="replace", timeout=900)
(output / (prefix + "-stdout.json")).write_text(result.stdout, encoding="utf-8")
(output / (prefix + "-stderr.txt")).write_text(result.stderr, encoding="utf-8")
receipt.update(returncode=result.returncode, elapsed_seconds=round(time.time()-started, 2))
(output / (prefix + "-launch.json")).write_text(json.dumps(receipt, indent=2), encoding="utf-8")
try:
    response = json.loads(result.stdout)
except json.JSONDecodeError:
    print(json.dumps({"returncode": result.returncode, "valid_json": False}))
    raise SystemExit(1)
(output / ("CLAUDE-FOLLOWUP.md" if followup else "CLAUDE-REVIEW.md")).write_text(str(response.get("result", "No review text returned")), encoding="utf-8")
print(json.dumps({"returncode": result.returncode, "is_error": response.get("is_error"), "duration_ms": response.get("duration_ms"), "modelUsage": response.get("modelUsage"), "num_turns": response.get("num_turns")}, ensure_ascii=False))
raise SystemExit(0 if result.returncode == 0 and not response.get("is_error") else 1)
