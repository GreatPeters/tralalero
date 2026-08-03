-- Firebase / GA4 BigQuery export: exact-day D1/D7/D30 retention,
-- automatic app engagement, and client-reported active round playtime.
-- Replace YOUR_PROJECT.analytics_PROPERTY_ID with the exported dataset name.
--
-- Finalized events_YYYYMMDD tables only:
--   * cohort_reporting_* selects mature first_open cohorts;
--   * activity_reporting_* selects engagement/playtime report dates;
--   * retention_observation_days controls the return observation horizon;
--   * scan_* is the union of those needs and does not change report membership.

DECLARE cohort_reporting_start DATE DEFAULT DATE_SUB(CURRENT_DATE(), INTERVAL 90 DAY);
DECLARE cohort_reporting_end DATE DEFAULT DATE_SUB(CURRENT_DATE(), INTERVAL 31 DAY);
DECLARE activity_reporting_start DATE DEFAULT DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY);
DECLARE activity_reporting_end DATE DEFAULT DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY);
DECLARE retention_observation_days INT64 DEFAULT 30;
DECLARE retention_observation_end DATE DEFAULT DATE_ADD(
  cohort_reporting_end,
  INTERVAL retention_observation_days DAY
);
DECLARE scan_lookback_days INT64 DEFAULT 0;
DECLARE scan_start DATE DEFAULT DATE_SUB(
  LEAST(cohort_reporting_start, activity_reporting_start),
  INTERVAL scan_lookback_days DAY
);
DECLARE scan_end DATE DEFAULT GREATEST(
  retention_observation_end,
  activity_reporting_end
);

CREATE TEMP FUNCTION ParamKeyCount(params ANY TYPE, param_name STRING)
AS (
  (SELECT COUNTIF(key = param_name) FROM UNNEST(params))
);

CREATE TEMP FUNCTION ParamStringExactCount(params ANY TYPE, param_name STRING)
AS (
  (
    SELECT COUNTIF(
      key = param_name
      AND value.string_value IS NOT NULL
      AND value.int_value IS NULL
      AND value.double_value IS NULL
      AND value.float_value IS NULL
    )
    FROM UNNEST(params)
  )
);

CREATE TEMP FUNCTION ParamIntExactCount(params ANY TYPE, param_name STRING)
AS (
  (
    SELECT COUNTIF(
      key = param_name
      AND value.string_value IS NULL
      AND value.int_value IS NOT NULL
      AND value.double_value IS NULL
      AND value.float_value IS NULL
    )
    FROM UNNEST(params)
  )
);

CREATE TEMP FUNCTION ParamDoubleExactCount(params ANY TYPE, param_name STRING)
AS (
  (
    SELECT COUNTIF(
      key = param_name
      AND value.string_value IS NULL
      AND value.int_value IS NULL
      AND value.double_value IS NOT NULL
      AND value.float_value IS NULL
    )
    FROM UNNEST(params)
  )
);

CREATE TEMP FUNCTION ParamString(params ANY TYPE, param_name STRING)
AS (
  (
    SELECT IF(
      COUNTIF(key = param_name) = 1
        AND COUNTIF(
          key = param_name
          AND value.string_value IS NOT NULL
          AND value.int_value IS NULL
          AND value.double_value IS NULL
          AND value.float_value IS NULL
        ) = 1,
      MAX(IF(key = param_name, value.string_value, NULL)),
      NULL
    )
    FROM UNNEST(params)
  )
);

CREATE TEMP FUNCTION ParamInt(params ANY TYPE, param_name STRING)
AS (
  (
    SELECT IF(
      COUNTIF(key = param_name) = 1
        AND COUNTIF(
          key = param_name
          AND value.string_value IS NULL
          AND value.int_value IS NOT NULL
          AND value.double_value IS NULL
          AND value.float_value IS NULL
        ) = 1,
      MAX(IF(key = param_name, value.int_value, NULL)),
      NULL
    )
    FROM UNNEST(params)
  )
);

CREATE TEMP FUNCTION ParamDouble(params ANY TYPE, param_name STRING)
AS (
  (
    SELECT IF(
      COUNTIF(key = param_name) = 1
        AND COUNTIF(
          key = param_name
          AND value.string_value IS NULL
          AND value.int_value IS NULL
          AND value.double_value IS NOT NULL
          AND value.float_value IS NULL
        ) = 1,
      MAX(IF(key = param_name, value.double_value, NULL)),
      NULL
    )
    FROM UNNEST(params)
  )
);

CREATE TEMP FUNCTION ParamIssue(
  param_name STRING,
  expected_type STRING,
  key_count INT64,
  exact_type_count INT64
)
AS (
  CASE
    WHEN key_count = 0
      THEN FORMAT('missing:%s(expected=%s)', param_name, expected_type)
    WHEN key_count != 1
      THEN FORMAT(
        'duplicate_or_ambiguous:%s(key_count=%d,expected=%s,exact_type_count=%d)',
        param_name,
        key_count,
        expected_type,
        exact_type_count
      )
    WHEN exact_type_count != 1
      THEN FORMAT('wrong_type:%s(expected=%s)', param_name, expected_type)
    ELSE NULL
  END
);

-- Result set 1: first_open cohorts with exact calendar-day returns. Activity
-- is scanned through the observation horizon, but only mature cohort dates are
-- reported.
WITH exported_activity AS (
  SELECT
    PARSE_DATE('%Y%m%d', event_date) AS activity_date,
    event_name,
    user_pseudo_id
  FROM `YOUR_PROJECT.analytics_PROPERTY_ID.events_*`
  WHERE
    REGEXP_CONTAINS(_TABLE_SUFFIX, r'^\d{8}$')
    AND _TABLE_SUFFIX BETWEEN FORMAT_DATE('%Y%m%d', scan_start)
                          AND FORMAT_DATE('%Y%m%d', scan_end)
    AND user_pseudo_id IS NOT NULL
    AND event_name IN (
      'first_open',
      'session_start',
      'user_engagement',
      'game_round_start'
    )
),
cohorts AS (
  SELECT
    user_pseudo_id,
    MIN(activity_date) AS cohort_date
  FROM exported_activity
  WHERE event_name = 'first_open'
  GROUP BY user_pseudo_id
  HAVING cohort_date BETWEEN cohort_reporting_start AND cohort_reporting_end
),
return_days AS (
  SELECT DISTINCT
    user_pseudo_id,
    activity_date
  FROM exported_activity
  WHERE event_name IN ('session_start', 'user_engagement', 'game_round_start')
)
SELECT
  cohorts.cohort_date,
  COUNT(DISTINCT cohorts.user_pseudo_id) AS cohort_users,
  COUNT(DISTINCT IF(
    DATE_DIFF(return_days.activity_date, cohorts.cohort_date, DAY) = 1,
    cohorts.user_pseudo_id,
    NULL
  )) AS d1_returning_users,
  ROUND(100 * SAFE_DIVIDE(
    COUNT(DISTINCT IF(
      DATE_DIFF(return_days.activity_date, cohorts.cohort_date, DAY) = 1,
      cohorts.user_pseudo_id,
      NULL
    )),
    COUNT(DISTINCT cohorts.user_pseudo_id)
  ), 2) AS d1_retention_pct,
  COUNT(DISTINCT IF(
    DATE_DIFF(return_days.activity_date, cohorts.cohort_date, DAY) = 7,
    cohorts.user_pseudo_id,
    NULL
  )) AS d7_returning_users,
  ROUND(100 * SAFE_DIVIDE(
    COUNT(DISTINCT IF(
      DATE_DIFF(return_days.activity_date, cohorts.cohort_date, DAY) = 7,
      cohorts.user_pseudo_id,
      NULL
    )),
    COUNT(DISTINCT cohorts.user_pseudo_id)
  ), 2) AS d7_retention_pct,
  COUNT(DISTINCT IF(
    DATE_DIFF(return_days.activity_date, cohorts.cohort_date, DAY) = 30,
    cohorts.user_pseudo_id,
    NULL
  )) AS d30_returning_users,
  ROUND(100 * SAFE_DIVIDE(
    COUNT(DISTINCT IF(
      DATE_DIFF(return_days.activity_date, cohorts.cohort_date, DAY) = 30,
      cohorts.user_pseudo_id,
      NULL
    )),
    COUNT(DISTINCT cohorts.user_pseudo_id)
  ), 2) AS d30_retention_pct
FROM cohorts
LEFT JOIN return_days
  ON cohorts.user_pseudo_id = return_days.user_pseudo_id
  AND return_days.activity_date BETWEEN DATE_ADD(cohorts.cohort_date, INTERVAL 1 DAY)
                                    AND DATE_ADD(
                                      cohorts.cohort_date,
                                      INTERVAL retention_observation_days DAY
                                    )
GROUP BY cohorts.cohort_date
ORDER BY cohorts.cohort_date;

-- Result set 2: Firebase/GA4 automatic app engagement. A missing
-- engagement_time_msec key is normal; a present key must occur exactly once as
-- INT64 and must fall within the per-event safety bound. Invalid values are
-- counted but excluded from BIGNUMERIC-safe totals.
WITH engagement_profiled AS (
  SELECT
    PARSE_DATE('%Y%m%d', event_date) AS activity_date,
    user_pseudo_id,
    ParamKeyCount(event_params, 'engagement_time_msec') AS key_count,
    ParamIntExactCount(event_params, 'engagement_time_msec')
      AS exact_type_count,
    ParamInt(event_params, 'engagement_time_msec') AS engagement_time_ms
  FROM `YOUR_PROJECT.analytics_PROPERTY_ID.events_*`
  WHERE
    REGEXP_CONTAINS(_TABLE_SUFFIX, r'^\d{8}$')
    AND _TABLE_SUFFIX BETWEEN FORMAT_DATE('%Y%m%d', activity_reporting_start)
                          AND FORMAT_DATE('%Y%m%d', activity_reporting_end)
    AND user_pseudo_id IS NOT NULL
),
engagement_classified AS (
  SELECT
    *,
    key_count = 1
      AND exact_type_count = 1
      AND engagement_time_ms BETWEEN 1 AND 86400000 AS is_valid
  FROM engagement_profiled
  WHERE key_count > 0
),
engagement_daily AS (
  SELECT
    activity_date,
    COUNT(DISTINCT IF(is_valid, user_pseudo_id, NULL)) AS engaged_users,
    COUNT(*) AS events_with_engagement_param,
    COUNTIF(is_valid) AS valid_engagement_events,
    COUNTIF(key_count > 1) AS duplicate_engagement_param_events,
    COUNTIF(key_count = 1 AND exact_type_count != 1)
      AS wrong_type_engagement_param_events,
    COUNTIF(
      key_count = 1
      AND exact_type_count = 1
      AND engagement_time_ms NOT BETWEEN 1 AND 86400000
    ) AS out_of_range_engagement_events,
    SUM(CAST(IF(is_valid, engagement_time_ms, 0) AS BIGNUMERIC))
      AS total_engagement_time_ms
  FROM engagement_classified
  GROUP BY activity_date
)
SELECT
  activity_date,
  engaged_users,
  events_with_engagement_param,
  valid_engagement_events,
  events_with_engagement_param - valid_engagement_events
    AS quarantined_engagement_events,
  duplicate_engagement_param_events,
  wrong_type_engagement_param_events,
  out_of_range_engagement_events,
  ROUND(
    SAFE_DIVIDE(
      total_engagement_time_ms,
      CAST(1000 AS BIGNUMERIC)
    ),
    2
  ) AS total_app_engagement_seconds,
  ROUND(
    SAFE_DIVIDE(
      total_engagement_time_ms,
      CAST(3600000 AS BIGNUMERIC)
    ),
    2
  ) AS total_app_engagement_hours,
  ROUND(
    SAFE_DIVIDE(
      total_engagement_time_ms,
      CAST(NULLIF(engaged_users, 0) AS BIGNUMERIC)
    ) / CAST(1000 AS BIGNUMERIC),
    2
  ) AS average_engagement_seconds_per_user
FROM engagement_daily
ORDER BY activity_date;

CREATE TEMP TABLE round_end_profiled AS
WITH extracted AS (
  SELECT
    PARSE_DATE('%Y%m%d', event_date) AS end_event_date,
    event_timestamp,
    user_pseudo_id,
    event_params,
    ParamString(event_params, 'round_id') AS round_id,
    ParamString(event_params, 'scene_name') AS scene_name,
    ParamString(event_params, 'game_mode') AS game_mode,
    ParamInt(event_params, 'chapter') AS chapter,
    ParamInt(event_params, 'stage') AS stage,
    ParamInt(event_params, 'max_stage') AS max_stage,
    ParamInt(event_params, 'client_event_time_ms') AS client_event_time_ms,
    ParamString(event_params, 'outcome') AS outcome,
    ParamDouble(event_params, 'chapter_progress_pct') AS chapter_progress_pct,
    ParamInt(event_params, 'coins_earned') AS coins_earned,
    ParamInt(event_params, 'play_time_ms') AS play_time_ms,
    ParamDouble(event_params, 'end_pos_x') AS end_pos_x,
    ParamDouble(event_params, 'end_pos_y') AS end_pos_y,
    ParamDouble(event_params, 'end_pos_z') AS end_pos_z,
    ParamString(event_params, 'upgrade_levels') AS upgrade_levels,
    ParamString(event_params, 'upgrade_flat') AS upgrade_flat,
    ParamString(event_params, 'upgrade_pct') AS upgrade_pct
  FROM `YOUR_PROJECT.analytics_PROPERTY_ID.events_*`
  WHERE
    REGEXP_CONTAINS(_TABLE_SUFFIX, r'^\d{8}$')
    AND _TABLE_SUFFIX BETWEEN FORMAT_DATE('%Y%m%d', scan_start)
                          AND FORMAT_DATE('%Y%m%d', scan_end)
    AND event_name = 'game_round_end'
),
classified AS (
  SELECT
    * EXCEPT (event_params),
    ARRAY(
      SELECT issue
      FROM UNNEST([
        ParamIssue('round_id', 'STRING',
          ParamKeyCount(event_params, 'round_id'),
          ParamStringExactCount(event_params, 'round_id')),
        ParamIssue('scene_name', 'STRING',
          ParamKeyCount(event_params, 'scene_name'),
          ParamStringExactCount(event_params, 'scene_name')),
        ParamIssue('game_mode', 'STRING',
          ParamKeyCount(event_params, 'game_mode'),
          ParamStringExactCount(event_params, 'game_mode')),
        ParamIssue('chapter', 'INT64',
          ParamKeyCount(event_params, 'chapter'),
          ParamIntExactCount(event_params, 'chapter')),
        ParamIssue('stage', 'INT64',
          ParamKeyCount(event_params, 'stage'),
          ParamIntExactCount(event_params, 'stage')),
        ParamIssue('max_stage', 'INT64',
          ParamKeyCount(event_params, 'max_stage'),
          ParamIntExactCount(event_params, 'max_stage')),
        ParamIssue('client_event_time_ms', 'INT64',
          ParamKeyCount(event_params, 'client_event_time_ms'),
          ParamIntExactCount(event_params, 'client_event_time_ms')),
        ParamIssue('outcome', 'STRING',
          ParamKeyCount(event_params, 'outcome'),
          ParamStringExactCount(event_params, 'outcome')),
        ParamIssue('chapter_progress_pct', 'DOUBLE',
          ParamKeyCount(event_params, 'chapter_progress_pct'),
          ParamDoubleExactCount(event_params, 'chapter_progress_pct')),
        ParamIssue('coins_earned', 'INT64',
          ParamKeyCount(event_params, 'coins_earned'),
          ParamIntExactCount(event_params, 'coins_earned')),
        ParamIssue('play_time_ms', 'INT64',
          ParamKeyCount(event_params, 'play_time_ms'),
          ParamIntExactCount(event_params, 'play_time_ms')),
        ParamIssue('end_pos_x', 'DOUBLE',
          ParamKeyCount(event_params, 'end_pos_x'),
          ParamDoubleExactCount(event_params, 'end_pos_x')),
        ParamIssue('end_pos_y', 'DOUBLE',
          ParamKeyCount(event_params, 'end_pos_y'),
          ParamDoubleExactCount(event_params, 'end_pos_y')),
        ParamIssue('end_pos_z', 'DOUBLE',
          ParamKeyCount(event_params, 'end_pos_z'),
          ParamDoubleExactCount(event_params, 'end_pos_z')),
        ParamIssue('upgrade_levels', 'STRING',
          ParamKeyCount(event_params, 'upgrade_levels'),
          ParamStringExactCount(event_params, 'upgrade_levels')),
        ParamIssue('upgrade_flat', 'STRING',
          ParamKeyCount(event_params, 'upgrade_flat'),
          ParamStringExactCount(event_params, 'upgrade_flat')),
        ParamIssue('upgrade_pct', 'STRING',
          ParamKeyCount(event_params, 'upgrade_pct'),
          ParamStringExactCount(event_params, 'upgrade_pct'))
      ]) AS issue
      WHERE issue IS NOT NULL
    ) AS schema_issues,
    ARRAY(
      SELECT issue
      FROM UNNEST([
        IF(user_pseudo_id IS NULL OR user_pseudo_id = '',
          'invalid:user_pseudo_id', NULL),
        IF(round_id IS NOT NULL AND (round_id = '' OR LENGTH(round_id) > 100),
          'out_of_range:round_id', NULL),
        IF(scene_name IS NOT NULL AND (scene_name = '' OR LENGTH(scene_name) > 100),
          'out_of_range:scene_name', NULL),
        IF(game_mode IS NOT NULL AND (game_mode = '' OR LENGTH(game_mode) > 100),
          'out_of_range:game_mode', NULL),
        IF(chapter IS NOT NULL AND chapter NOT BETWEEN 0 AND 10000,
          'out_of_range:chapter', NULL),
        IF(stage IS NOT NULL AND stage NOT BETWEEN 0 AND 100000,
          'out_of_range:stage', NULL),
        IF(max_stage IS NOT NULL AND max_stage NOT BETWEEN 0 AND 100000,
          'out_of_range:max_stage', NULL),
        IF(
          stage IS NOT NULL
            AND max_stage IS NOT NULL
            AND max_stage > 0
            AND stage > max_stage,
          'inconsistent:stage_gt_max_stage',
          NULL
        ),
        IF(
          client_event_time_ms IS NOT NULL
            AND client_event_time_ms NOT BETWEEN
              UNIX_MILLIS(TIMESTAMP '2020-01-01 00:00:00+00')
              AND UNIX_MILLIS(TIMESTAMP_ADD(CURRENT_TIMESTAMP(), INTERVAL 1 DAY)),
          'out_of_range:client_event_time_ms',
          NULL
        ),
        IF(outcome IS NOT NULL AND outcome NOT IN ('win', 'death', 'abandoned'),
          'invalid_enum:outcome', NULL),
        IF(
          chapter_progress_pct IS NOT NULL
            AND (
              IS_NAN(chapter_progress_pct)
              OR IS_INF(chapter_progress_pct)
              OR chapter_progress_pct NOT BETWEEN 0.0 AND 100.0
            ),
          'out_of_range:chapter_progress_pct',
          NULL
        ),
        IF(coins_earned IS NOT NULL
            AND coins_earned NOT BETWEEN 0 AND 1000000000,
          'out_of_range:coins_earned', NULL),
        IF(play_time_ms IS NOT NULL
            AND play_time_ms NOT BETWEEN 0 AND 86400000,
          'out_of_range:play_time_ms', NULL),
        IF(end_pos_x IS NOT NULL
            AND (IS_NAN(end_pos_x) OR IS_INF(end_pos_x) OR ABS(end_pos_x) > 1000000),
          'out_of_range:end_pos_x', NULL),
        IF(end_pos_y IS NOT NULL
            AND (IS_NAN(end_pos_y) OR IS_INF(end_pos_y) OR ABS(end_pos_y) > 1000000),
          'out_of_range:end_pos_y', NULL),
        IF(end_pos_z IS NOT NULL
            AND (IS_NAN(end_pos_z) OR IS_INF(end_pos_z) OR ABS(end_pos_z) > 1000000),
          'out_of_range:end_pos_z', NULL),
        IF(upgrade_levels IS NOT NULL AND LENGTH(upgrade_levels) > 100,
          'out_of_range:upgrade_levels', NULL),
        IF(upgrade_flat IS NOT NULL AND LENGTH(upgrade_flat) > 100,
          'out_of_range:upgrade_flat', NULL),
        IF(upgrade_pct IS NOT NULL AND LENGTH(upgrade_pct) > 100,
          'out_of_range:upgrade_pct', NULL)
      ]) AS issue
      WHERE issue IS NOT NULL
    ) AS metric_issues
  FROM extracted
)
SELECT
  *,
  ARRAY_CONCAT(schema_issues, metric_issues) AS quarantine_reasons,
  ARRAY_LENGTH(schema_issues) = 0
    AND ARRAY_LENGTH(metric_issues) = 0 AS is_valid
FROM classified;

-- Result set 3: round-end ingestion quality. Missing, duplicate, wrong-type,
-- and out-of-range records remain measurable instead of disappearing.
SELECT
  end_event_date,
  COUNT(*) AS exported_round_end_events,
  COUNTIF(is_valid) AS valid_round_end_events,
  COUNTIF(NOT is_valid) AS quarantined_round_end_events,
  COUNTIF(ARRAY_LENGTH(schema_issues) > 0) AS schema_invalid_events,
  COUNTIF(ARRAY_LENGTH(metric_issues) > 0) AS metric_invalid_events,
  COUNTIF(EXISTS(
    SELECT 1 FROM UNNEST(schema_issues) AS issue
    WHERE STARTS_WITH(issue, 'missing:')
  )) AS missing_required_param_events,
  COUNTIF(EXISTS(
    SELECT 1 FROM UNNEST(schema_issues) AS issue
    WHERE STARTS_WITH(issue, 'duplicate_or_ambiguous:')
  )) AS duplicate_required_param_events,
  COUNTIF(EXISTS(
    SELECT 1 FROM UNNEST(schema_issues) AS issue
    WHERE STARTS_WITH(issue, 'wrong_type:')
  )) AS wrong_type_required_param_events
FROM round_end_profiled
WHERE end_event_date BETWEEN activity_reporting_start AND activity_reporting_end
GROUP BY end_event_date
ORDER BY end_event_date;

-- Result set 4: one latest valid end event per pseudonymous user + round,
-- aggregated by client occurrence date and chapter. Firebase receive time
-- remains visible for delayed-upload diagnostics, but never chooses the metric
-- date.
WITH deduplicated_round_ends AS (
  SELECT
    * EXCEPT (row_number),
    DATE(TIMESTAMP_MILLIS(client_event_time_ms)) AS round_occurrence_date
  FROM (
    SELECT
      *,
      ROW_NUMBER() OVER (
        PARTITION BY user_pseudo_id, round_id
        ORDER BY client_event_time_ms DESC, event_timestamp DESC
      ) AS row_number
    FROM round_end_profiled
    WHERE is_valid
  )
  WHERE row_number = 1
),
daily_chapter AS (
  SELECT
    round_occurrence_date,
    chapter,
    COUNT(*) AS ended_rounds,
    COUNT(DISTINCT user_pseudo_id) AS active_players,
    COUNTIF(outcome = 'win') AS won_rounds,
    COUNTIF(outcome = 'death') AS death_rounds,
    COUNTIF(outcome = 'abandoned') AS abandoned_rounds,
    MIN(TIMESTAMP_MILLIS(client_event_time_ms))
      AS first_occurrence_time_utc,
    MAX(TIMESTAMP_MILLIS(client_event_time_ms))
      AS last_occurrence_time_utc,
    MIN(TIMESTAMP_MICROS(event_timestamp))
      AS first_firebase_received_time_utc,
    MAX(TIMESTAMP_MICROS(event_timestamp))
      AS last_firebase_received_time_utc,
    SUM(CAST(play_time_ms AS BIGNUMERIC)) AS total_active_play_time_ms,
    APPROX_QUANTILES(play_time_ms, 100)[OFFSET(50)] AS median_play_time_ms,
    APPROX_QUANTILES(play_time_ms, 100)[OFFSET(90)] AS p90_play_time_ms
  FROM deduplicated_round_ends
  WHERE
    round_occurrence_date BETWEEN
      activity_reporting_start AND activity_reporting_end
  GROUP BY round_occurrence_date, chapter
)
SELECT
  daily_chapter.round_occurrence_date,
  daily_chapter.chapter,
  daily_chapter.ended_rounds,
  daily_chapter.active_players,
  daily_chapter.won_rounds,
  daily_chapter.death_rounds,
  daily_chapter.abandoned_rounds,
  daily_chapter.first_occurrence_time_utc,
  daily_chapter.last_occurrence_time_utc,
  daily_chapter.first_firebase_received_time_utc,
  daily_chapter.last_firebase_received_time_utc,
  ROUND(
    SAFE_DIVIDE(
      daily_chapter.total_active_play_time_ms,
      CAST(1000 AS BIGNUMERIC)
    ),
    2
  ) AS total_active_play_seconds,
  ROUND(
    SAFE_DIVIDE(
      daily_chapter.total_active_play_time_ms,
      CAST(3600000 AS BIGNUMERIC)
    ),
    2
  ) AS total_active_play_hours,
  ROUND(
    SAFE_DIVIDE(
      daily_chapter.total_active_play_time_ms,
      CAST(daily_chapter.ended_rounds AS BIGNUMERIC)
    ) / CAST(1000 AS BIGNUMERIC),
    2
  ) AS average_round_seconds,
  ROUND(daily_chapter.median_play_time_ms / 1000.0, 2)
    AS median_round_seconds,
  ROUND(daily_chapter.p90_play_time_ms / 1000.0, 2)
    AS p90_round_seconds
FROM daily_chapter
ORDER BY daily_chapter.round_occurrence_date, daily_chapter.chapter;
