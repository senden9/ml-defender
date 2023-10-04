using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SlimeModel : MonoBehaviour, IModel
{
    public GameObject goodAgentPrefab;
    public uint goodAgentCount = 25;

    public float maxAgentVelocity = 13f;

    /// <summary>
    ///     Number of steps for a simulation.
    /// </summary>
    public uint maxSteps = 2300;

    public uint handBadSlimeCount = 0;
    public GameObject handBadSlimePrefab;

    public uint mlBadSlimeCount = 0;
    public GameObject mlBadSlimePrefab;

    /// <summary>
    ///     Maximum distance that agents can see.
    ///     Implemented via limiting the objective function.
    /// </summary>
    public float maxVisionDistance = float.PositiveInfinity;

    /// <summary>
    ///     Side length of the search area cube
    /// </summary>
    [FormerlySerializedAs("areaSideLength")]
    public uint maxDimensions = 40;

    /// <summary>
    ///     Used to mark the corner of the play field.
    ///     Just informal. Moving that objects will not influence the agents or the model.
    /// </summary>
    public GameObject cornerMarkerPrefab;

    private List<GameObject> placedCornerMarkers = new();

    private List<IAgent> agents = new();
    [NonSerialized] public List<float> currentFitnessPerAgent = new();

    [Serializable]
    public enum SpawnModeType
    {
        /// <summary>
        ///     Spawn around a specific point
        /// </summary>
        Point,

        /// <summary>
        ///     Spawn fully random in the cube
        /// </summary>
        FullArea
    }

    public SpawnModeType spawnMode = SpawnModeType.Point;
    public Vector3 pointSpawnModeStartpoint = Vector3.zero;
    public float pointSpawnModeRadius = 1f;

    public Transform targetPoint;
    public float targetRadius;

    /// <summary>
    ///     "Magic" value as defined in the paper. Must be between 0 and 1 exclusive both.
    ///     See Eq 2.7 in  https://doi.org/10.1016/j.future.2020.03.055
    ///     Can be seen as possibility for going random instead of the deterministic way.
    /// </summary>
    [Range(0.0001f, 0.9999f)] public float z = 0.03f;

    [NonSerialized] public float bestFitness;
    [NonSerialized] public Vector3 bestPosition;
    [NonSerialized] public float worstFitness;
    [NonSerialized] public float destinationFitness;

    public uint stepsDone = 0;
    private bool targetHit = false;

    private SimpleMultiAgentGroup agentGroup;

    [Header("Stats")] public int attackersWon = 0;
    public int attackersLose = 0;

    private IModel.SimFinished simFinishedCallback;

    // Start is called before the first frame update
    void Start()
    {
        attackersLose = 0;
        attackersWon = 0;
        agentGroup = new SimpleMultiAgentGroup();
        agents.Clear();
        currentFitnessPerAgent.Clear();

        uint idxStart = 0;
        for (uint idx = idxStart; idx < goodAgentCount + idxStart; idx++)
        {
            GameObject newAgent = Instantiate(goodAgentPrefab, transform);
            newAgent.name = $"Good Agent #{idx}";
            IAgent sa = newAgent.GetComponent<IAgent>();
            sa.SetIdx(idx);
            sa.SetSimulationModel(this);
            sa.SetObjectiveFunction(ObjectiveFunction);
            agents.Add(sa);
            currentFitnessPerAgent.Add(0f);
        }

        idxStart += goodAgentCount;

        for (uint idx = idxStart; idx < handBadSlimeCount + idxStart; idx++)
        {
            GameObject newAgent = Instantiate(handBadSlimePrefab, transform);
            newAgent.name = $"Hand Defender #{idx}";
            IAgent sa = newAgent.GetComponent<IAgent>();
            sa.SetIdx(idx);
            sa.SetSimulationModel(this);
            sa.SetObjectiveFunction(ObjectiveFunction);
            agents.Add(sa);
            currentFitnessPerAgent.Add(0f);
        }

        idxStart += handBadSlimeCount;

        for (uint idx = idxStart; idx < mlBadSlimeCount + idxStart; idx++)
        {
            GameObject newAgent = Instantiate(mlBadSlimePrefab, transform);
            newAgent.name = $"ML Defender #{idx}";
            IAgent sa = newAgent.GetComponent<IAgent>();
            sa.SetIdx(idx);
            sa.SetSimulationModel(this);
            sa.SetObjectiveFunction(ObjectiveFunction);
            agents.Add(sa);
            currentFitnessPerAgent.Add(0f);
            // Register agent at the ML-Framework
            Agent mlAgentInstance = newAgent.GetComponent<Agent>();
            Assert.IsNotNull(mlAgentInstance);
            agentGroup.RegisterAgent(mlAgentInstance);
        }

        idxStart += mlBadSlimeCount;

        ResetScene();
    }

    public void ResetScene()
    {
        stepsDone = 0;
        targetHit = false;
        destinationFitness = float.PositiveInfinity;

        // Mix up agent positions
        foreach (var agent in agents)
        {
            if (spawnMode == SpawnModeType.Point)
            {
                Vector3 spherePoint = Random.insideUnitSphere * pointSpawnModeRadius;
                Vector3 startPoint = spherePoint + pointSpawnModeStartpoint;
                agent.SetCurrentPosition(startPoint);
            }
            else if (spawnMode == SpawnModeType.FullArea)
            {
                float dist = maxDimensions / 2f;
                var newAgentPos = new Vector3(
                    Random.Range(-dist, dist),
                    Random.Range(-dist, dist),
                    Random.Range(-dist, dist)
                );
                agent.SetCurrentPosition(newAgentPos);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        // Reset all agents
        foreach (IAgent agent in agents)
        {
            agent.ResetState();
        }

        // Reset target position
        targetPoint.localPosition = new Vector3(
            Random.Range(-maxDimensions / 2f, maxDimensions / 2f),
            Random.Range(-maxDimensions / 2f, maxDimensions / 2f),
            Random.Range(-maxDimensions / 2f, maxDimensions / 2f)
        );

        // Mark size of gamefield
        foreach (GameObject placedCornerMarker in placedCornerMarkers)
        {
            Destroy(placedCornerMarker);
        }

        placedCornerMarkers.Clear();

        for (int x = -1; x < 2; x += 2)
        {
            for (int y = -1; y < 2; y += 2)
            {
                for (int z = -1; z < 2; z += 2)
                {
                    GameObject marker = Instantiate(cornerMarkerPrefab, transform);
                    marker.transform.localPosition = new Vector3(
                        x * maxDimensions / 2f,
                        y * maxDimensions / 2f,
                        z * maxDimensions / 2f
                    );
                    placedCornerMarkers.Add(marker);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (stepsDone >= maxSteps)
        {
            attackersLose += 1;
            Debug.Log("Attackers Lost");
            SendStats(haveAttackersWon: false);
        }

        if (targetHit)
        {
            agentGroup.GroupEpisodeInterrupted();
            attackersWon += 1;
            SendStats(haveAttackersWon: true);
        }
        else if (stepsDone >= maxSteps)
        {
            agentGroup.EndGroupEpisode();
        }

        if (stepsDone >= maxSteps || targetHit)
        {
            ResetScene();
            simFinishedCallback?.Invoke();
            return;
        }

        stepsDone++;

        // Agent Phase 1
        foreach (IAgent agent in agents)
        {
            // fetch fitness from all agents
            currentFitnessPerAgent[(int)agent.GetIdx()] = agent.Fitness();
            agent.Step();
        }

        // Model Phase 1
        bestFitness = float.PositiveInfinity;
        worstFitness = float.NegativeInfinity;

        Assert.AreEqual(currentFitnessPerAgent.Count, agents.Count);

        for (int i = 0; i < currentFitnessPerAgent.Count; i++)
        {
            float fit = currentFitnessPerAgent[i];
            if (fit < bestFitness)
            {
                bestFitness = fit;
            }

            if (fit > worstFitness)
            {
                worstFitness = fit;
            }

            if (fit < destinationFitness)
            {
                destinationFitness = fit;
                bestPosition = agents[i].GetCurrentPosition();
            }
        }

        if (bestFitness == worstFitness)
        {
            // TODO: This is normally a bug, but if we have limited vision it happens that all agents report the same.
            //Debug.Log("Strange, best and worse are the same...");
            worstFitness = worstFitness + 0.0001f;
        }

        // Agent Phase 2
        foreach (IAgent agent in agents)
        {
            agent.Advance();
        }

        // Target hit check
        Vector3 targetPos = targetPoint.localPosition;
        foreach (IAgent agent in agents)
        {
            Vector3 agentPos = agent.GetCurrentPosition();
            float dist = (agentPos - targetPos).magnitude;
            if (dist <= targetRadius)
            {
                targetHit = true;
                uint agentId = agent.GetIdx();
                Debug.Log($"Agent {agentId} hit target");
            }
        }

        if (targetHit)
        {
            agentGroup.AddGroupReward(-1f);
        }
        else
        {
            // Just give them a reward for being alive ("existential reward").
            agentGroup.AddGroupReward(0.01f);
        }
    }

    /// <summary>
    ///     Maximal velocity for an agent in m / step.
    /// </summary>
    public float MaxAgentMovementPerStep()
    {
        return maxAgentVelocity * Time.fixedDeltaTime;
    }

    public void SetSimulationFinishedEvent(IModel.SimFinished func)
    {
        this.simFinishedCallback = func;
    }

    float ObjectiveFunction(Vector3 pos)
    {
        Vector3 delta = targetPoint.localPosition - pos;
        //return delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
        //return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) + Mathf.Abs(delta.z);

        // Used in my bachelor thesis
        //delta = delta.ElementAbs();
        //return delta.x + delta.y + delta.z;

        // Same as GWO Master thesis
        return MathF.Min(delta.magnitude, maxVisionDistance);
    }

    /// <summary>
    ///     Returns the total number of active agents on the playfield
    /// </summary>
    public int TotalAgents()
    {
        return agents.Count;
    }

    public IAgent GetRandomAgent()
    {
        int max = agents.Count;
        int idx = Random.Range(0, max);
        return agents[idx];
    }

    public int NrObservations()
    {
        return 6;
    }

    public List<float> AgentObservations(Vector3 localAgentPos)
    {
        List<float> ret = new List<float>(NrObservations()); // Init with capacity for performance

        // We start with the information all agents get from the model.
        ret.Add(bestFitness);
        ret.Add(worstFitness);
        ret.Add((float)stepsDone / maxSteps);

        // Extra info: Target relative to calling agent
        Vector3 dTarget = targetPoint.localPosition - localAgentPos;
        ret.Add(dTarget.x);
        ret.Add(dTarget.y);
        ret.Add(dTarget.z);

        Assert.AreEqual(NrObservations(), ret.Count); // check if we used the right nr of slots.
        return ret;
    }

    /// <summary>
    ///     Returns the nearest `count` agents relative to `localCenterPos`.
    /// </summary>
    /// <param name="localCenterPos">Position we want to use to be relative to.</param>
    /// <param name="count">Maximum number of return values.</param>
    /// <param name="ignoreSelf">If `true` the position of `localCenterPos` will not be counted if seen first</param>
    /// <returns>A list of vectors relative to `localCenterPos`. Ordered from near to far. Has at most `count` elements.</returns>
    public List<Vector3> NearestXAgents(Vector3 localCenterPos, int count, bool ignoreSelf = true)
    {
        bool selfSeen = !ignoreSelf;
        List<Vector3> relVectors = new List<Vector3>();
        foreach (IAgent agent in agents)
        {
            Vector3 relativePos = agent.GetCurrentPosition() - localCenterPos;
            if (!selfSeen && relativePos.sqrMagnitude == 0.0)
            {
                // Skip first time we see our own position.
                selfSeen = true;
            }
            else
            {
                // Add to seen agent list.
                relVectors.Add(relativePos);
            }
        }

        return relVectors.OrderBy(p => p.sqrMagnitude).Take(count).ToList();
    }

    public Vector3 PlayfieldSize()
    {
        return new Vector3(maxDimensions, maxDimensions, maxDimensions);
    }

    /// <summary>
    ///     Sends data over to the statistics event-system.
    ///     For logging & post-processing.
    ///     Must be called at the end of each round before reset the simulation.
    /// </summary>
    /// <param name="haveAttackersWon">Sets flag if attackers or defenders have won that round</param>
    private void SendStats(bool haveAttackersWon)
    {
        if (StatsEventSystem.current is null)
        {
            Debug.LogWarning("Can not send statistics. No statistics event system found.");
            return;
        }

        RoundStatisticDto obj = new RoundStatisticDto
        {
            EnvironmentType = RoundStatisticDto.EnvironmentTypeEnum.SMA,
            MaxRounds = maxSteps,
            PlayedRounds = stepsDone,
            WhoWon = haveAttackersWon
                ? RoundStatisticDto.WhoWonEnum.AttackerWon
                : RoundStatisticDto.WhoWonEnum.DefenderWon,
            LineOfSight = maxVisionDistance,
            NrAttackers = goodAgentCount,
            NrDefenders = mlBadSlimeCount,
            MaxSpeed = maxAgentVelocity,
            AreaSideLength = maxDimensions,
            TargetHitRadius = targetRadius
        };

        StatsEventSystem.current.OnRoundFinished(obj);
    }
}