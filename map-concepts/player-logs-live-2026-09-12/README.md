# Tra player-log connection — complete

The user's existing private [Tra spreadsheet](https://docs.google.com/spreadsheets/d/12L_8CQ6GlHSw7eBZUdv-Gw6L3o5T7A4pXZccIIymVZ0/edit) is connected to Firebase's exported BigQuery data. The bound [Apps Script project](https://script.google.com/home/projects/1w2wwewSmKh7ktl7gIkaLNw29hXYDlEZlm3OJDnyCiTp-VUOLyV1Zgm_8/edit) contains the maintained refresh code and canonical round query. No new sharing recipients or billing upgrade were added.

## Verified configuration

- Project: `tralaleroshooter`; dataset: `analytics_547820149`; location: `asia-northeast3`.
- Sheets: 요약, 플레이로그, 연결설정. One row per round, Korean-time date display, typed IDs, filters and frozen headers.
- Reporting window:60 days through UTC yesterday. Initial30-day query correctly returned0; extending to60 included the retained July31 round.
- One `refreshPlayerLogs` time-based trigger is registered under the owner account. The trigger UI confirms daily10:00–11:00, GMT+09:00. This verifies registration; the future scheduled invocation has not yet occurred.
- The sheet's 플레이 로그 menu exposes 지금 새로고침, 매일 자동 갱신 and 자동 갱신 끄기.
- Each query remains capped at1GiB and5000 rows; errors preserve previous data. Empty successful results have a distinct connected/no-records message.

## Actual data evidence

The dataset currently contains `events_20260731`. An aggregate query found screen_view2, user_engagement2, session_start1, first_open1, game_round_start1 and game_round_end1. The canonical validation/pairing query imported one real round:19.168 seconds,20 coins, chapter1, outcome death. No synthetic records were inserted.

A native spreadsheet export read back60-day settings, one imported row, matching summary values and zero formula errors across all three sheets. The summary was visually inspected in Chrome. Raw exported workbook evidence stays in `tmp/tra-live-20260912`; this record intentionally omits player identifiers.

## Implementation and recovery

- Maintained sources: `tools/analytics/google-sheets/Code.gs`, `RoundQuery.gs`, `appsscript.json`; the query is generated from canonical SQL by `build-query.mjs`.
- The deployed single Code.gs combines the runtime code and a whitespace-compacted canonical SQL string. Its SQL was compared by length/FNV checksum against the local derived query; the final editor contents were read back through UI copy. Temporary setup/inspection functions were removed after use.
- A single-occurrence dataset replacement was corrected: the placeholder appears in both a SQL comment and the actual FROM clause. Split/join replaces every occurrence, and connector tests now assert that no placeholder remains. All five connector scenarios pass.
- The original Tra had one empty sheet. Its native export backup is `tmp/tra-live-20260912/Tra-before.xlsx`. Chrome's file-upload permission was unavailable, so the existing sheet was configured through its authorized bound Apps Script instead. The earlier request to enable file-URL access was withdrawn.
- Google Sheets rejected freezing four columns while the title merged across all twenty columns. The title/subtitle now merge only A:D, entirely within the frozen region. The partial setup was resumed without duplicating sheets or deleting user data.
- After an advanced Sheets API write, SpreadsheetApp reads in the same invocation briefly returned cached pre-update values. Independent native export/readback verified the actual saved result; stale diagnostic logs were not treated as the final truth.

To disconnect, use 플레이 로그 → 자동 갱신 끄기. This removes this project's daily refresh trigger; Firebase export is a separate setting.
