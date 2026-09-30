---
title: "Unity 프로젝트 이전 후 C 경로 의존성과 원본 폴더 정리"
date: "2026-09-28"
category: workflow-issues
module: "Tralalero Shooter 프로젝트 이전"
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - "Unity 프로젝트를 복사한 뒤 원본을 삭제하면서 개발 도구와 작업 복구 기록을 유지할 때"
tags: [unity, drive-migration, git, codex, windows, junctions]
---

# Unity 프로젝트 이전 및 C 원본 삭제 완료

## Context

사용자는 D 작업본을 유지하고 C의 `tralalero Shooter` 프로젝트를 삭제하도록 명시적으로 요청했다. C에 더 최신인 `NoryangjinMapToolTestSpeed.cs` 수정은 필요 없다고 확인했으므로 D에 적용하거나 별도 코드 백업을 만들지 않았다. Codex 체크포인트와 Git 객체는 복구 기록으로 보존했다.

- 삭제된 폴더: `C:\Users\ljh\tralalero Shooter`
- 유지하는 작업 경로: `D:\Tralalero Shooter\Tralalero Shooter D`
- 삭제 종료 시각: `2026-09-28T22:22:46.4409415+09:00`
- 삭제 전후 C 여유 공간 순증가: **277.1 GB (258.1 GiB)**. 동시에 실행 중인 다른 프로그램의 디스크 사용 변화도 포함된 수치다.

## Guidance

1. 전체 목록을 다시 대조했다. 소스 파일 549,224개, 약307.15GB였다. D에 없는 항목은 재생성 가능한 Library/Logs 및 보존할 Git 기록뿐이었다. C 쪽 수정 시각이 더 최신인 HTML은 내용이 D와 같았다. 의도적으로 버릴 코드 외에 새로 보존해야 할 작업 파일은 발견되지 않았다.
2. C에만 있는 체크포인트 ref 1개와 Git 객체 10개를 D의 `.git`에 추가했다. 기존 D 파일을 덮어쓰지 않았고, 파일 해시 및 Git 조회로 확인했다. 체크포인트에서 도달 가능한 객체도 모두 존재했다.
3. `tools` 스크립트 18개의 입력/출력 경로를 C에서 D로 변경했다. 변경 전후의 차이가 경로 치환뿐인지 확인하고 Python 파일은 구문 분석했다.
4. 전역 Codex Unity MCP 설정은 작업 시작 시 이미 D로 변경되어 있었다. 이 실제 설정으로 MCP 서버를 새로 연결해 도구149개를 조회하고 `editor_status`·`list_open_scenes` 호출이 D에서 성공하는지 확인했다. C를 대상으로 남아 있던 MCP 프로세스2개와 실행 중이지 않은 C Editor의 오래된 발견 파일을 정리했다.
5. 로컬 미리보기 서버4개(8773·8774·8775·8776)를 동일 포트, D의 대응 폴더로 재시작했다. 모두 HTTP200 응답을 확인했다. Unity Hub 목록에서는 C 항목을 제거했다.
6. C 프로젝트의 외부 연결 폴더14개는 재귀 삭제 전에 연결만 제거했다. 대상 공용 Node 라이브러리의 존속을 전후 확인했다. 정확한 절대 경로를 검사한 뒤 PowerShell의 `Remove-Item -LiteralPath`로 C 프로젝트를 삭제했다.

## Why This Matters

복사 이후에도 기존 프로세스는 이전 경로를 계속 쓸 수 있다. 설정 파일만 바꾸거나 Unity가 D에서 열렸다는 사실만 확인하면, 오래된 미리보기 서버와 도구 출력 경로가 C 폴더를 다시 만들거나 실패할 수 있다. Git HEAD가 같아도 Codex 체크포인트의 객체는 한쪽에만 남을 수 있다.

Windows 긴 경로는 `\\?\` 접두사로 검사해야 실제 누락과 구별할 수 있다. Git 검증 출력의 한글 파일명은 Python의 기본 CP949 디코딩으로 실패할 수 있으므로 UTF-8을 명시하거나 `--no-object-names`로 객체 ID만 조회한다. 이번 검증의 인코딩 실패는 이 방법으로 해결했다.

## When to Apply

- Unity 프로젝트를 다른 드라이브로 이전한 뒤 원본을 삭제할 때.
- 복사 뒤 두 경로의 파일이 달라졌거나, MCP·로컬 웹 서버·에셋 제작 도구가 이전 경로를 사용하는 경우.
- 재귀 삭제 대상 아래에 외부 디렉터리를 가리키는 junction이 있는 경우.

## Examples

현재 사용 경로를 직접 지정한 읽기 전용 검증:

```powershell
unity command --project-path 'D:\Tralalero Shooter\Tralalero Shooter D' editor_status --format json
unity command --project-path 'D:\Tralalero Shooter\Tralalero Shooter D' list_open_scenes --format json
```

보존한 체크포인트 객체: `9174cd0ace9d397ca58c8e0d3c9774dc79a5755a`.

실제 작업 변경은 경로 치환과 Git 기록 보존, C 등록/프로세스 정리 및 요청된 폴더 삭제다. 게임 로직, 씬, 에셋을 변경하거나 새 빌드·전체 플레이 테스트를 실행하지 않았다. 오래된 보고서와 모델 출처 메타데이터 속 과거 C 경로는 이력으로 남겨두었다.

## Related

- 프로젝트 `AGENTS.md`의 공식 Unity CLI/Pipeline 연결 지침.
- 최초 삭제 가능 여부 점검에서 발견한 C 전용 수정 및 체크포인트 차이를 사용자의 선택에 따라 처리한 후속 작업이다.
