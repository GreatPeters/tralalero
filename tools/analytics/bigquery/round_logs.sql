-- Firebase / GA4 BigQuery export: one row per game round.
-- Replace YOUR_PROJECT.analytics_PROPERTY_ID with the exported dataset name.
--
-- Defaults intentionally read finalized events_YYYYMMDD tables only. The
-- reporting window controls rows returned to analysts; the earlier scan window
-- exists only to pair starts that occurred before the reporting window.

DECLARE reporting_start DATE DEFAULT DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY);
DECLARE reporting_end DATE DEFAULT DATE_SUB(CURRENT_DATE(), INTERVAL 1 DAY);
DECLARE pairing_lookback_days INT64 DEFAULT 7;
DECLARE observation_end DATE DEFAULT reporting_end;
DECLARE pending_observation_days INT64 DEFAULT 1;
DECLARE scan_start DATE DEFAULT DATE_SUB(
  reporting_start,
  INTERVAL pairing_lookback_days DAY
);
DECLARE scan_end DATE DEFAULT observation_end;

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

CREATE TEMP TABLE round_events_profiled AS
WITH extracted AS (
  SELECT
    event_name,
    event_timestamp,
    event_server_timestamp_offset,
    PARSE_DATE('%Y%m%d', event_date) AS event_date,
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
    AND event_name IN ('game_round_start', 'game_round_end')
),
classified AS (
  SELECT
    * EXCEPT (event_params),
    ARRAY(
      SELECT issue
      FROM UNNEST([
        ParamIssue(
          'round_id',
          'STRING',
          ParamKeyCount(event_params, 'round_id'),
          ParamStringExactCount(event_params, 'round_id')
        ),
        ParamIssue(
          'scene_name',
          'STRING',
          ParamKeyCount(event_params, 'scene_name'),
          ParamStringExactCount(event_params, 'scene_name')
        ),
        ParamIssue(
          'game_mode',
          'STRING',
          ParamKeyCount(event_params, 'game_mode'),
          ParamStringExactCount(event_params, 'game_mode')
        ),
        ParamIssue(
          'chapter',
          'INT64',
          ParamKeyCount(event_params, 'chapter'),
          ParamIntExactCount(event_params, 'chapter')
        ),
        ParamIssue(
          'stage',
          'INT64',
          ParamKeyCount(event_params, 'stage'),
          ParamIntExactCount(event_params, 'stage')
        ),
        ParamIssue(
          'max_stage',
          'INT64',
          ParamKeyCount(event_params, 'max_stage'),
          ParamIntExactCount(event_params, 'max_stage')
        ),
        ParamIssue(
          'client_event_time_ms',
          'INT64',
          ParamKeyCount(event_params, 'client_event_time_ms'),
          ParamIntExactCount(event_params, 'client_event_time_ms')
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'outcome',
            'STRING',
            ParamKeyCount(event_params, 'outcome'),
            ParamStringExactCount(event_params, 'outcome')
          ),
          NULL
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'chapter_progress_pct',
            'DOUBLE',
            ParamKeyCount(event_params, 'chapter_progress_pct'),
            ParamDoubleExactCount(event_params, 'chapter_progress_pct')
          ),
          NULL
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'coins_earned',
            'INT64',
            ParamKeyCount(event_params, 'coins_earned'),
            ParamIntExactCount(event_params, 'coins_earned')
          ),
          NULL
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'play_time_ms',
            'INT64',
            ParamKeyCount(event_params, 'play_time_ms'),
            ParamIntExactCount(event_params, 'play_time_ms')
          ),
          NULL
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'end_pos_x',
            'DOUBLE',
            ParamKeyCount(event_params, 'end_pos_x'),
            ParamDoubleExactCount(event_params, 'end_pos_x')
          ),
          NULL
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'end_pos_y',
            'DOUBLE',
            ParamKeyCount(event_params, 'end_pos_y'),
            ParamDoubleExactCount(event_params, 'end_pos_y')
          ),
          NULL
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'end_pos_z',
            'DOUBLE',
            ParamKeyCount(event_params, 'end_pos_z'),
            ParamDoubleExactCount(event_params, 'end_pos_z')
          ),
          NULL
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'upgrade_levels',
            'STRING',
            ParamKeyCount(event_params, 'upgrade_levels'),
            ParamStringExactCount(event_params, 'upgrade_levels')
          ),
          NULL
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'upgrade_flat',
            'STRING',
            ParamKeyCount(event_params, 'upgrade_flat'),
            ParamStringExactCount(event_params, 'upgrade_flat')
          ),
          NULL
        ),
        IF(
          event_name = 'game_round_end',
          ParamIssue(
            'upgrade_pct',
            'STRING',
            ParamKeyCount(event_params, 'upgrade_pct'),
            ParamStringExactCount(event_params, 'upgrade_pct')
          ),
          NULL
        )
      ]) AS issue
      WHERE issue IS NOT NULL
    ) AS schema_issues,
    ARRAY(
      SELECT issue
      FROM UNNEST([
        IF(
          user_pseudo_id IS NULL OR user_pseudo_id = '',
          'invalid:user_pseudo_id',
          NULL
        ),
        IF(
          round_id IS NOT NULL
            AND (round_id = '' OR LENGTH(round_id) > 100),
          'out_of_range:round_id',
          NULL
        ),
        IF(
          scene_name IS NOT NULL
            AND (scene_name = '' OR LENGTH(scene_name) > 100),
          'out_of_range:scene_name',
          NULL
        ),
        IF(
          game_mode IS NOT NULL
            AND (game_mode = '' OR LENGTH(game_mode) > 100),
          'out_of_range:game_mode',
          NULL
        ),
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
        IF(
          event_name = 'game_round_end'
            AND outcome IS NOT NULL
            AND outcome NOT IN ('win', 'death', 'abandoned'),
          'invalid_enum:outcome',
          NULL
        ),
        IF(
          event_name = 'game_round_end'
            AND chapter_progress_pct IS NOT NULL
            AND (
              IS_NAN(chapter_progress_pct)
              OR IS_INF(chapter_progress_pct)
              OR chapter_progress_pct NOT BETWEEN 0.0 AND 100.0
            ),
          'out_of_range:chapter_progress_pct',
          NULL
        ),
        IF(
          event_name = 'game_round_end'
            AND coins_earned IS NOT NULL
            AND coins_earned NOT BETWEEN 0 AND 1000000000,
          'out_of_range:coins_earned',
          NULL
        ),
        IF(
          event_name = 'game_round_end'
            AND play_time_ms IS NOT NULL
            AND play_time_ms NOT BETWEEN 0 AND 86400000,
          'out_of_range:play_time_ms',
          NULL
        ),
        IF(
          event_name = 'game_round_end'
            AND end_pos_x IS NOT NULL
            AND (IS_NAN(end_pos_x) OR IS_INF(end_pos_x) OR ABS(end_pos_x) > 1000000),
          'out_of_range:end_pos_x',
          NULL
        ),
        IF(
          event_name = 'game_round_end'
            AND end_pos_y IS NOT NULL
            AND (IS_NAN(end_pos_y) OR IS_INF(end_pos_y) OR ABS(end_pos_y) > 1000000),
          'out_of_range:end_pos_y',
          NULL
        ),
        IF(
          event_name = 'game_round_end'
            AND end_pos_z IS NOT NULL
            AND (IS_NAN(end_pos_z) OR IS_INF(end_pos_z) OR ABS(end_pos_z) > 1000000),
          'out_of_range:end_pos_z',
          NULL
        ),
        IF(
          event_name = 'game_round_end'
            AND upgrade_levels IS NOT NULL
            AND LENGTH(upgrade_levels) > 100,
          'out_of_range:upgrade_levels',
          NULL
        ),
        IF(
          event_name = 'game_round_end'
            AND upgrade_flat IS NOT NULL
            AND LENGTH(upgrade_flat) > 100,
          'out_of_range:upgrade_flat',
          NULL
        ),
        IF(
          event_name = 'game_round_end'
            AND upgrade_pct IS NOT NULL
            AND LENGTH(upgrade_pct) > 100,
          'out_of_range:upgrade_pct',
          NULL
        )
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

-- Result set 1: quarantined raw events. These rows are never paired into the
-- reporting result. Keep this result as an ingestion-contract alert source.
SELECT
  event_date AS firebase_received_event_date,
  TIMESTAMP_MICROS(event_timestamp) AS firebase_received_time_utc,
  event_server_timestamp_offset AS server_timestamp_offset_us,
  event_name,
  user_pseudo_id,
  round_id,
  schema_issues,
  metric_issues,
  quarantine_reasons
FROM round_events_profiled
WHERE NOT is_valid
ORDER BY event_timestamp DESC;

-- Result set 2 (primary): valid, deduplicated rounds in the reporting window.
WITH valid_round_events AS (
  SELECT *
  FROM round_events_profiled
  WHERE is_valid
),
ranked_starts AS (
  SELECT
    *,
    COUNT(*) OVER (
      PARTITION BY user_pseudo_id, round_id
    ) AS start_event_count,
    ROW_NUMBER() OVER (
      PARTITION BY user_pseudo_id, round_id
      ORDER BY client_event_time_ms ASC, event_timestamp ASC
    ) AS row_number
  FROM valid_round_events
  WHERE event_name = 'game_round_start'
),
starts AS (
  SELECT * EXCEPT (row_number)
  FROM ranked_starts
  WHERE row_number = 1
),
ranked_ends AS (
  SELECT
    *,
    COUNT(*) OVER (
      PARTITION BY user_pseudo_id, round_id
    ) AS end_event_count,
    ROW_NUMBER() OVER (
      PARTITION BY user_pseudo_id, round_id
      ORDER BY client_event_time_ms DESC, event_timestamp DESC
    ) AS row_number
  FROM valid_round_events
  WHERE event_name = 'game_round_end'
),
ends AS (
  SELECT * EXCEPT (row_number)
  FROM ranked_ends
  WHERE row_number = 1
),
quarantined_pairable AS (
  SELECT
    user_pseudo_id,
    round_id,
    COUNTIF(event_name = 'game_round_start') AS quarantined_start_event_count,
    COUNTIF(event_name = 'game_round_end') AS quarantined_end_event_count
  FROM round_events_profiled
  WHERE
    NOT is_valid
    AND user_pseudo_id IS NOT NULL
    AND user_pseudo_id != ''
    AND round_id IS NOT NULL
    AND round_id != ''
  GROUP BY user_pseudo_id, round_id
),
paired AS (
  SELECT
    COALESCE(starts.user_pseudo_id, ends.user_pseudo_id) AS user_pseudo_id,
    COALESCE(starts.round_id, ends.round_id) AS round_id,
    starts.event_date AS start_event_date,
    ends.event_date AS end_event_date,
    starts.event_timestamp AS start_firebase_event_timestamp_us,
    ends.event_timestamp AS end_firebase_event_timestamp_us,
    starts.event_server_timestamp_offset AS start_server_timestamp_offset_us,
    ends.event_server_timestamp_offset AS end_server_timestamp_offset_us,
    starts.client_event_time_ms AS start_client_event_time_ms,
    ends.client_event_time_ms AS end_client_event_time_ms,
    DATE(TIMESTAMP_MILLIS(starts.client_event_time_ms))
      AS start_occurrence_date,
    DATE(TIMESTAMP_MILLIS(ends.client_event_time_ms))
      AS end_occurrence_date,
    starts.scene_name AS start_scene_name,
    ends.scene_name AS end_scene_name,
    starts.game_mode AS start_game_mode,
    ends.game_mode AS end_game_mode,
    starts.chapter AS start_chapter,
    ends.chapter AS end_chapter,
    starts.stage AS start_stage,
    ends.stage AS end_stage,
    starts.max_stage AS start_max_stage,
    ends.max_stage AS end_max_stage,
    ends.outcome,
    ends.chapter_progress_pct,
    ends.coins_earned,
    ends.play_time_ms,
    ends.end_pos_x,
    ends.end_pos_y,
    ends.end_pos_z,
    ends.upgrade_levels,
    ends.upgrade_flat,
    ends.upgrade_pct,
    starts.start_event_count,
    ends.end_event_count,
    IFNULL(quarantined_pairable.quarantined_start_event_count, 0)
      AS quarantined_start_event_count,
    IFNULL(quarantined_pairable.quarantined_end_event_count, 0)
      AS quarantined_end_event_count
  FROM starts
  FULL OUTER JOIN ends
    ON starts.user_pseudo_id = ends.user_pseudo_id
    AND starts.round_id = ends.round_id
  LEFT JOIN quarantined_pairable
    ON COALESCE(starts.user_pseudo_id, ends.user_pseudo_id)
      = quarantined_pairable.user_pseudo_id
    AND COALESCE(starts.round_id, ends.round_id)
      = quarantined_pairable.round_id
)
SELECT
  user_pseudo_id,
  round_id,
  CASE
    WHEN start_client_event_time_ms IS NULL
      AND quarantined_start_event_count > 0
      THEN 'start_quarantined'
    WHEN end_client_event_time_ms IS NULL
      AND quarantined_end_event_count > 0
      THEN 'end_quarantined'
    WHEN start_client_event_time_ms IS NULL
      AND end_occurrence_date = reporting_start
      THEN 'boundary_start_unobserved'
    WHEN start_client_event_time_ms IS NULL
      THEN 'start_unobserved_after_lookback'
    WHEN end_client_event_time_ms IS NULL
      AND start_occurrence_date > DATE_SUB(
        observation_end,
        INTERVAL pending_observation_days DAY
      )
      THEN 'pending_end_observation'
    WHEN end_client_event_time_ms IS NULL
      THEN 'end_unobserved_after_grace'
    ELSE 'complete'
  END AS join_status,
  start_client_event_time_ms,
  end_client_event_time_ms,
  start_occurrence_date,
  end_occurrence_date,
  TIMESTAMP_MILLIS(start_client_event_time_ms) AS start_occurrence_time_utc,
  TIMESTAMP_MILLIS(end_client_event_time_ms) AS end_occurrence_time_utc,
  TIMESTAMP_MICROS(start_firebase_event_timestamp_us)
    AS start_firebase_received_time_utc,
  TIMESTAMP_MICROS(end_firebase_event_timestamp_us)
    AS end_firebase_received_time_utc,
  start_server_timestamp_offset_us,
  end_server_timestamp_offset_us,
  start_event_date AS start_firebase_received_event_date,
  end_event_date AS end_firebase_received_event_date,
  -- End-event context is authoritative for completed rounds.
  COALESCE(end_scene_name, start_scene_name) AS scene_name,
  COALESCE(end_game_mode, start_game_mode) AS game_mode,
  COALESCE(end_chapter, start_chapter) AS chapter,
  COALESCE(end_stage, start_stage) AS stage,
  COALESCE(end_max_stage, start_max_stage) AS max_stage,
  start_scene_name,
  end_scene_name,
  start_game_mode,
  end_game_mode,
  start_chapter,
  end_chapter,
  start_stage,
  end_stage,
  start_max_stage,
  end_max_stage,
  outcome,
  chapter_progress_pct,
  coins_earned,
  play_time_ms,
  CASE
    WHEN start_client_event_time_ms IS NULL OR end_client_event_time_ms IS NULL
      THEN NULL
    ELSE end_client_event_time_ms - start_client_event_time_ms
  END AS observed_occurrence_elapsed_ms,
  CASE
    WHEN
      start_firebase_event_timestamp_us IS NULL
      OR end_firebase_event_timestamp_us IS NULL
      THEN NULL
    ELSE CAST(
      SAFE_DIVIDE(
        end_firebase_event_timestamp_us - start_firebase_event_timestamp_us,
        1000
      ) AS INT64
    )
  END AS observed_receive_elapsed_ms,
  end_pos_x,
  end_pos_y,
  end_pos_z,
  upgrade_levels,
  upgrade_flat,
  upgrade_pct,
  start_event_count,
  end_event_count,
  quarantined_start_event_count,
  quarantined_end_event_count
FROM paired
WHERE
  start_occurrence_date BETWEEN reporting_start AND reporting_end
  OR end_occurrence_date BETWEEN reporting_start AND reporting_end
ORDER BY COALESCE(
  end_client_event_time_ms,
  start_client_event_time_ms
) DESC;
