using System;
using Common;
using UnityEngine;

public class StatisticsRuntimeEstimator : MonoBehaviour
{
    private int? totalRounds;
    private int runnedSims;
    private float startTime;

    // Start is called before the first frame update
    private void Start()
    {
        StatsEventSystem.current.CalculatedTotalRounds += OnCalculatedTotalRounds;
        StatsEventSystem.current.RoundFinished += OnRoundFinished;
        startTime = Time.realtimeSinceStartup;
    }

    private void OnCalculatedTotalRounds(int newTotalRounds)
    {
        totalRounds = newTotalRounds;
    }

    private void OnRoundFinished(RoundStatisticDto obj)
    {
        runnedSims += 1;
        var now = Time.realtimeSinceStartup;
        var dt = now - startTime;
        if (dt < float.Epsilon * 10)
        {
            Debug.LogWarning($"StatisticsRuntimeEstimator: `dt` is zero or negative. Value: {dt}");
            return;
        }

        var logMsg = string.Empty;
        var meanTimePerSim = runnedSims / dt;
        Debug.Log($"Mean simulation time: {meanTimePerSim:F2}sec.");

        if (totalRounds != null && totalRounds > 0)
        {
            var totalRounds = (int)this.totalRounds;
            var doneFrac = (float)runnedSims / totalRounds;
            var remainingSims = totalRounds - runnedSims;
            var secsRemaining = meanTimePerSim * remainingSims;
            var timeRemaining = TimeSpan.FromSeconds(secsRemaining);
            logMsg += $" {doneFrac * 100:F2}% done. {timeRemaining:c} till finished.";
            var estimatedFinishDateTime = DateTime.Now.AddSeconds(secsRemaining);
            logMsg += $" Estimated finished at: {estimatedFinishDateTime}.";
        }
        else
        {
            Debug.LogWarning("StatisticsRuntimeEstimator: Total rounds unknown or less equal zero");
        }

        Debug.Log(logMsg);
    }

    private void OnDestroy()
    {
        StatsEventSystem.current.RoundFinished -= OnRoundFinished;
        StatsEventSystem.current.CalculatedTotalRounds -= OnCalculatedTotalRounds;
    }
}