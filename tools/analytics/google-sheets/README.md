# Player logs in Google Sheets

Live destination: [Tra](https://docs.google.com/spreadsheets/d/12L_8CQ6GlHSw7eBZUdv-Gw6L3o5T7A4pXZccIIymVZ0/edit). Connected on2026-09-12 to `tralaleroshooter.analytics_547820149`, location `asia-northeast3`, with a60-day window and one daily10:00–11:00 KST trigger. One real exported round was imported and the native workbook has no formula errors. [Connection evidence](../../../map-concepts/player-logs-live-2026-09-12/README.md).

Reusable template: `outputs/player-logs-2026-09-12/PlayerLogs.xlsx`. The template contains no fabricated player records; the live Tra sheet is the current configured destination.

The workbook has a compact summary, one row per game round, and connection settings. Times display in Korea time. The reporting window uses UTC occurrence dates and finalized daily Firebase export tables; it is not a live dashboard. `조회 기간 순번` is a player's order within the selected window, not lifetime attempt count. Missing starts/ends retain the canonical query's explicit pairing statuses.

## Connect the prepared workbook

1. Import the verified workbook as a native Google Sheet through the Google Drive connector. Keep sharing restricted to the intended account/team.
2. In Firebase project `tralaleroshooter`, verify Analytics → BigQuery integration and obtain the actual `analytics_...` dataset and its location. If no export exists yet, enable it and wait for the first daily table. This repository does not contain proof that export was already enabled.
3. Enter dataset and location in `연결설정!B6:B7`. The viewing account needs BigQuery data access and permission to create query jobs in the selected project.
4. In the sheet's Apps Script project, add `Code.gs`, `RoundQuery.gs` and the supplied manifest. Enable the BigQuery and Sheets advanced services and their corresponding Google Cloud APIs when using a standard Cloud project.
5. Run `refreshPlayerLogs` once and approve the account's Google consent screen. Check the recorded refresh date/count and compare one round with BigQuery. Run `installDailyRefresh` to enable daily refresh around10:00 Asia/Seoul; remove it with `removeDailyRefresh`.

No service-account key or admin credentials belong in Unity Assets. The connector uses the executing Google account. Queries have a configurable bytes-billed cap, bounded date window and maximum5000 rows. An over-limit, timeout or failed query leaves the previous log intact. A single Sheets batch update writes typed values and clears stale rows atomically. IDs and potentially formula-like strings are written as literal strings.

## Maintenance and verification

- `node tools/analytics/google-sheets/build-query.mjs` derives `RoundQuery.gs` from `round_logs.sql`; do not maintain a second event-validation implementation.
- `node tools/analytics/google-sheets/Code.test.mjs` verifies pagination, null/numeric/string preservation, formula-looking IDs, row limit, failure and empty refresh behavior with service mocks. These are local tests, not a Cloud integration test.
- Android device events are needed for actual Firebase verification. Editor analytics uses a stub.
- The first refresh may exceed a low query cap as the dataset grows. Adjust the visible cap deliberately or shorten the window; the script never removes the cap automatically.

Sources: [Firebase export](https://firebase.google.com/docs/analytics/bigquery-export), [Apps Script BigQuery](https://developers.google.com/apps-script/advanced/bigquery), [BigQuery query API](https://docs.cloud.google.com/bigquery/docs/reference/rest/v2/jobs/query), [Sheets batch updates](https://developers.google.com/workspace/sheets/api/guides/batchupdate).
