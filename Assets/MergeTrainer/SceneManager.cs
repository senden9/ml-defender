using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class SceneManager : MonoBehaviour
{
    public GameObject GWOPlayfieldPrefab;
    public GameObject SMAPlayfieldPrefab;
    public GameObject mergeDefenderAgentPrefab;
    public uint runPerSetting = 100;

    public uint parallelRunningEnvs;

    public uint[] PossibleAreaSideLength;
    public float[] PossibleMaxAgentSpeed;
    public uint[] PossibleMaxEpisodeLength;
    public uint[] PossibleNrMlDefenders;
    public uint[] PossibleNrAttackers;
    public float[] PossibleTargetHitRadius;
    public float[] PossibleMaxVisionDistance;

    private struct SettingCombo
    {
        public RoundStatisticDto.EnvironmentTypeEnum EnvironmentType;
        public uint areaSideLength;
        public float maxAgentSpeed;
        public uint maxEpisodeLength;
        public uint nrMlDefenders;
        public uint nrAttackers;
        public float targetHitRadius;
        public float maxVisionDistance;
    }

    private List<IModel> RunningModels;
    private Queue<SettingCombo> SettingCombos;
    private bool CalculatedTotalRoundsSend;
    private int CalculatedTotalRounds;

    private void OnEnable()
    {
        SettingCombos ??= new Queue<SettingCombo>();
        RunningModels ??= new List<IModel>();

        var settingsList = new List<SettingCombo>();
        // Generate all possible settings
        RoundStatisticDto.EnvironmentTypeEnum[] allEnvironmentTypes =
            { RoundStatisticDto.EnvironmentTypeEnum.GWO, RoundStatisticDto.EnvironmentTypeEnum.SMA };
        for (uint c = 0; c < runPerSetting; c++)
        {
            foreach (uint areaSideLength in PossibleAreaSideLength)
            {
                foreach (float maxAgentSpeed in PossibleMaxAgentSpeed)
                {
                    foreach (uint maxEpisodeLength in PossibleMaxEpisodeLength)
                    {
                        foreach (uint nrMlDefenders in PossibleNrMlDefenders)
                        {
                            foreach (uint nrAttackers in PossibleNrAttackers)
                            {
                                foreach (float targetHitRadius in PossibleTargetHitRadius)
                                {
                                    foreach (float maxVisionDistance in PossibleMaxVisionDistance)
                                    {
                                        foreach (RoundStatisticDto.EnvironmentTypeEnum environmentType in
                                                 allEnvironmentTypes)
                                        {
                                            settingsList.Add(
                                                new SettingCombo()
                                                {
                                                    EnvironmentType = environmentType,
                                                    areaSideLength = areaSideLength,
                                                    maxAgentSpeed = maxAgentSpeed,
                                                    maxEpisodeLength = maxEpisodeLength,
                                                    nrMlDefenders = nrMlDefenders,
                                                    nrAttackers = nrAttackers,
                                                    targetHitRadius = targetHitRadius,
                                                    maxVisionDistance = maxVisionDistance,
                                                }
                                            );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Shuffle setting combination order
        // This only works because `OrderBy` caches the return value of the delegate.
        settingsList = settingsList.OrderBy(x => Random.value).ToList();
        SettingCombos = new Queue<SettingCombo>(settingsList);

        Assert.AreEqual(
            SettingCombos.Count,
            PossibleAreaSideLength.Length * PossibleMaxAgentSpeed.Length * PossibleMaxEpisodeLength.Length *
            PossibleNrMlDefenders.Length * PossibleNrAttackers.Length * PossibleTargetHitRadius.Length *
            PossibleMaxVisionDistance.Length * runPerSetting *
            Enum.GetValues(typeof(RoundStatisticDto.EnvironmentTypeEnum)).Length,
            "Safety check of number of setting combos failed"
        );
        CalculatedTotalRounds = SettingCombos.Count;

        if (parallelRunningEnvs == 0)
        {
            Debug.LogWarning("Will run no environments because the setting tells us we should run 0 in parallel.",
                this);
        }

        Debug.Log($"Nr of setting combos: {SettingCombos.Count}");

        float maxSideLength = PossibleAreaSideLength.Max();
        for (int i = 0; i < parallelRunningEnvs; i++)
        {
            Vector3 localEnvPos = new Vector3(
                (i % 2 == 0) ? 0 : maxSideLength,
                0,
                (int)(i / 2) * maxSideLength
            );
            SettingCombo nextSettings;
            bool gotValue = SettingCombos.TryDequeue(out nextSettings);
            if (gotValue)
            {
                SpawnEnvironment(nextSettings, localEnvPos);
            }
            else
            {
                Debug.Log("We are out of new settings for our simulation. Right at the start.");
            }
        }
    }

    /// <summary>
    /// Terminates the calling instance and spawn a new one if we have more settings in our queue.
    /// </summary>
    /// <param name="caller">Object that will get replaced</param>
    private void ReplaceItself(GameObject caller)
    {
        Vector3 envPos = caller.transform.localPosition;
        RemoveEnvironmentFromList(caller);
        Destroy(caller);
        SettingCombo nextSettings;
        bool gotValue = SettingCombos.TryDequeue(out nextSettings);
        if (gotValue)
        {
            SpawnEnvironment(nextSettings, envPos);
        }
        else
        {
            Debug.Log("We are out of new settings for our simulation. We should shortly be done!");
        }
    }

    /// <summary>
    /// Removes an IModel from our list of running models.  
    /// </summary>
    /// <param name="go">Gameobject that has the model we want to remove from our list.</param>
    private void RemoveEnvironmentFromList(GameObject go)
    {
        IModel model = go.GetComponent<IModel>();
        if (model is null)
        {
            Debug.LogWarning("Object has no model that we would remove from our list.", go);
            return;
        }

        bool removed = RunningModels.Remove(model);
        if (!removed)
        {
            Debug.LogWarning(
                "Object contained an IModel but that one was not in our list of running models. Buggy behaviour!", go);
        }

        if (RunningModels.Count == 0 && SettingCombos.Count == 0)
        {
            Debug.Log("No more running environments and nothing in the queue. Seems we are done here.");
        }
    }

    /// <summary>
    /// Spawns a new training / evaluation environment with the given settings at the given position.
    /// Also adds the model to our list of running models.
    /// </summary>
    /// <param name="settings">The settings that will be used</param>
    /// <param name="localPos">Local position to spawn at</param>
    /// <returns>Created game object ready to run.</returns>
    private GameObject SpawnEnvironment(SettingCombo settings, Vector3 localPos)
    {
        GameObject go;
        IModel iModel;
        switch (settings.EnvironmentType)
        {
            case RoundStatisticDto.EnvironmentTypeEnum.GWO:
                // Generate and position playfield
                go = Instantiate(GWOPlayfieldPrefab, transform);
                go.transform.localPosition = localPos;
                go.name = $"GWO Field";
                var gwoModelInterface = go.GetComponent<GWOModel>();
                Assert.IsNotNull(gwoModelInterface);

                // Inject settings
                gwoModelInterface.maxDimensions = new Vector3(settings.areaSideLength, settings.areaSideLength,
                    settings.areaSideLength);
                gwoModelInterface.maxAgentVelocity = settings.maxAgentSpeed;
                gwoModelInterface.maxSteps = settings.maxEpisodeLength;
                gwoModelInterface.badMlAgentCount = settings.nrMlDefenders;
                gwoModelInterface.goodAgentCount = settings.nrAttackers;
                gwoModelInterface.targetHitRadius = settings.targetHitRadius;
                gwoModelInterface.maxVisionDistance = settings.maxVisionDistance;
                gwoModelInterface.badMlAgentPrefab = mergeDefenderAgentPrefab;

                iModel = gwoModelInterface;
                break;
            case RoundStatisticDto.EnvironmentTypeEnum.SMA:
                // Generate and position playfield
                go = Instantiate(SMAPlayfieldPrefab, transform);
                go.transform.localPosition = localPos;
                go.name = $"SMA Field";
                var smaModelInterface = go.GetComponent<SlimeModel>();
                Assert.IsNotNull(smaModelInterface);

                // Inject settings
                smaModelInterface.maxDimensions = settings.areaSideLength;
                smaModelInterface.maxAgentVelocity = settings.maxAgentSpeed;
                smaModelInterface.maxSteps = settings.maxEpisodeLength;
                smaModelInterface.mlBadSlimeCount = settings.nrMlDefenders;
                smaModelInterface.goodAgentCount = settings.nrAttackers;
                smaModelInterface.targetRadius = settings.targetHitRadius;
                smaModelInterface.maxVisionDistance = settings.maxVisionDistance;
                smaModelInterface.mlBadSlimePrefab = mergeDefenderAgentPrefab;

                iModel = smaModelInterface;
                break;
            default:
                Debug.LogError("Environment not supported in Scene Manager", this);
                throw new ArgumentOutOfRangeException();
        }

        iModel.SetSimulationFinishedEvent(() => ReplaceItself(go));
        RunningModels.Add(iModel);
        return go;
    }

    private void Update()
    {
        if (CalculatedTotalRoundsSend) return;

        StatsEventSystem.current.OnCalculatedTotalRounds(CalculatedTotalRounds);
        CalculatedTotalRoundsSend = true;
    }
}