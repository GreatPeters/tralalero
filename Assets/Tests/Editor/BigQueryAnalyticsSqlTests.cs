#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class BigQueryAnalyticsSqlTests
{
    [Test]
    public void RoundLogs_UseOccurrenceDatesAndEndEventContext()
    {
        string sql = NormalizeLineEndings(ReadProjectFile(
            "tools/analytics/bigquery/round_logs.sql"));

        Assert.That(sql, Does.Not.Contain("LIMIT 1"));
        Assert.That(sql, Does.Contain("ParamKeyCount"));
        Assert.That(sql, Does.Contain("start_occurrence_date"));
        Assert.That(sql, Does.Contain("end_occurrence_date"));
        Assert.That(
            sql,
            Does.Contain("COALESCE(end_chapter, start_chapter) AS chapter"));
        Assert.That(sql, Does.Contain("start_chapter"));
        Assert.That(sql, Does.Contain("end_chapter"));
        Assert.That(sql, Does.Contain("boundary_start_unobserved"));
        Assert.That(sql, Does.Contain("start_unobserved_after_lookback"));
        Assert.That(sql, Does.Contain("pending_end_observation"));
        Assert.That(sql, Does.Contain("quarantine_reasons"));
    }

    [Test]
    public void RetentionAndPlaytime_UseValidatedOverflowSafeMetrics()
    {
        string sql = NormalizeLineEndings(ReadProjectFile(
            "tools/analytics/bigquery/retention_and_playtime.sql"));

        Assert.That(sql, Does.Not.Contain("LIMIT 1"));
        Assert.That(sql, Does.Contain("retention_observation_days"));
        Assert.That(sql, Does.Contain("round_occurrence_date"));
        Assert.That(
            sql,
            Does.Contain(
                "end_event_date BETWEEN activity_reporting_start " +
                "AND activity_reporting_end"));
        Assert.That(
            sql,
            Does.Contain(
                "round_occurrence_date BETWEEN\n" +
                "      activity_reporting_start AND activity_reporting_end"));
        Assert.That(sql, Does.Contain("ParamIntExactCount"));
        Assert.That(sql, Does.Contain("quarantined_round_end_events"));
        Assert.That(sql, Does.Contain("CAST(play_time_ms AS BIGNUMERIC)"));
        Assert.That(sql, Does.Contain("out_of_range_engagement_events"));
    }

    private static string ReadProjectFile(string projectRelativePath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return File.ReadAllText(
            Path.Combine(
                projectRoot,
                projectRelativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n");
    }
}
#endif
