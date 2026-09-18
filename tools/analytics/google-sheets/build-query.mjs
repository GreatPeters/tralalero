import fs from 'node:fs/promises';
const root=new URL('./',import.meta.url);
let sql=await fs.readFile(new URL('../bigquery/round_logs.sql',root),'utf8');
sql=sql.replace('DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)','@reporting_start').replace('DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY)','@reporting_end');
const first=sql.indexOf('-- Result set 1:');const second=sql.indexOf('WITH valid_round_events AS',first);
if(first<0||second<0)throw new Error('Canonical round query structure changed');
sql=sql.slice(0,first)+'CREATE TEMP TABLE sheet_rounds AS\n'+sql.slice(second);
sql+='\nSELECT start_client_event_time_ms, end_client_event_time_ms, user_pseudo_id, round_id, chapter, scene_name, outcome, play_time_ms / 1000.0 AS play_seconds, chapter_progress_pct / 100.0 AS progress, coins_earned, stage, max_stage, join_status, upgrade_levels, upgrade_flat, upgrade_pct, end_pos_x, end_pos_y, end_pos_z, ROW_NUMBER() OVER(PARTITION BY user_pseudo_id ORDER BY COALESCE(start_client_event_time_ms,end_client_event_time_ms),round_id) AS window_attempt\nFROM sheet_rounds\nORDER BY COALESCE(end_client_event_time_ms,start_client_event_time_ms) DESC, user_pseudo_id,round_id\nLIMIT @row_limit;\n';
await fs.writeFile(new URL('RoundQuery.gs',root),'// Generated from the canonical validated round query.\nconst ROUND_QUERY = '+JSON.stringify(sql)+';\n');
