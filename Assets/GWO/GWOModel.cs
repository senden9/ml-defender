using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GWOModel : MonoBehaviour, IModel
{
    /// <summary>
    ///     Maximal dimensions of the playground. In m.
    /// </summary>
    public Vector3 maxDimensions;

    /// <summary>
    ///     Maximum velocity of the agents. In m/s.
    /// </summary>
    public float maxAgentVelocity;

    /// <summary>
    ///     Number of steps for a simulation.
    /// </summary>
    public uint maxSteps;

    /// <summary>
    ///     Radius in meters where the target counts a hit, measured from it's center.
    /// </summary>
    public float targetHitRadius;

    private uint stepsDone = 0;
    private bool targetHit = false;

    private SimpleMultiAgentGroup agentGroup;

    /// <summary>
    ///     Parameter between 0 and 2 according to the GWO paper.
    /// </summary>
    [NonSerialized] public float a;

    [FormerlySerializedAs("agentPrefab")] public GameObject goodAgentPrefab;
    [FormerlySerializedAs("agentCount")] public uint goodAgentCount;

    [FormerlySerializedAs("badAgentPrefab")]
    public GameObject badManualAgentPrefab;

    [FormerlySerializedAs("badAgentCount")]
    public uint badManualAgentCount;

    public GameObject badMlAgentPrefab;
    public uint badMlAgentCount;

    /// <summary>
    ///     Maximum distance that agents can see.
    ///     Implemented via limiting the objective function.
    /// </summary>
    public float maxVisionDistance = float.PositiveInfinity;

    public Transform targetPoint;

    // List of ML-Agents on that playfield. 
    private List<MlDefender> mlAgents = new List<MlDefender>();

    public GameObject[] leadWolfs;

    /// <summary>
    ///     Contains all agents except lead wolfs.
    ///     This includes wolf-agents, manual defenders, ml defenders.
    /// </summary>
    private List<IAgent> agents;

    private IModel.SimFinished simFinishedCallback;

    void Start()
    {
        agents = new List<IAgent>();
        for (int i = 0; i < goodAgentCount; i++)
        {
            GameObject agentGo = Instantiate(goodAgentPrefab, transform);
            agentGo.name = $"Omega Agent #{i}";
            IAgent agent = agentGo.GetComponent<IAgent>();
            Assert.IsNotNull(agent);
            agent.SetSimulationModel(this);
            agent.SetObjectiveFunction(ObjectiveFunction);
            agents.Add(agent);
        }

        for (int i = 0; i < badManualAgentCount; i++)
        {
            GameObject agentGo = Instantiate(badManualAgentPrefab, transform);
            agentGo.name = $"Manual Defender #{i}";
            IAgent agent = agentGo.GetComponent<IAgent>();
            Assert.IsNotNull(agent);
            agent.SetSimulationModel(this);
            agent.SetObjectiveFunction(ObjectiveFunction);
            agents.Add(agent);
        }

        agentGroup = new SimpleMultiAgentGroup();
        for (int i = 0; i < badMlAgentCount; i++)
        {
            GameObject agentGo = Instantiate(badMlAgentPrefab, transform);
            agentGo.name = $"ML Defender #{i}";
            IAgent agent = agentGo.GetComponent<IAgent>();
            Assert.IsNotNull(agent);
            agent.SetSimulationModel(this);
            agent.SetObjectiveFunction(ObjectiveFunction);
            agents.Add(agent);

            Agent mlAgentInstance = agentGo.GetComponent<Agent>();
            Assert.IsNotNull(mlAgentInstance);
            agentGroup.RegisterAgent(mlAgentInstance);
        }

        foreach (GameObject leadWolf in leadWolfs)
        {
            leadWolf.GetComponent<LeadAgent>().objf = ObjectiveFunction;
        }

        ResetScene();
    }

    public void ResetScene()
    {
        stepsDone = 0;
        targetHit = false;

        // Mix up agent positions
        foreach (var agent in agents)
        {
            var newAgentPos = new Vector3(
                Random.Range(-maxDimensions.x / 2f, 0.5f),
                Random.Range(-maxDimensions.y / 2f, -0.5f),
                Random.Range(-maxDimensions.z / 2f, maxDimensions.z / 2f)
            );
            agent.SetCurrentPosition(newAgentPos);
        }

        // Reset all agents
        foreach (IAgent agent in agents)
        {
            agent.ResetState();
        }

        foreach (GameObject leadWolf in leadWolfs)
        {
            leadWolf.GetComponent<IAgent>().ResetState();
        }

        // Reset target position
        Vector3 maxDim = maxDimensions;
        targetPoint.localPosition = new Vector3(
            Random.Range(0, maxDim.x / 2f),
            Random.Range(0, maxDim.y / 2f),
            Random.Range(-maxDim.z / 2f, maxDim.z / 2)
        );
    }

    private void FixedUpdate()
    {
        if (stepsDone == 0)
        {
            // Skip first step for init.
            stepsDone++;
            return;
        }

        if (stepsDone >= maxSteps || targetHit)
        {
            SendStats(targetHit);
            agentGroup.GroupEpisodeInterrupted();
            ResetScene();
            simFinishedCallback?.Invoke();
            return;
        }

        UpdateA();

        // Check for next best wolf lead position
        Assert.AreEqual(leadWolfs.Length, 3);
        Assert.IsTrue(agents.Count >= 3);

        List<(float score, Vector3 localPosition)> fitness = new List<(float Score, Vector3 localPosition)>();
        foreach (IAgent agent in agents)
        {
            var fit = agent.Fitness();
            var pos = agent.GetCurrentPosition();
            fitness.Add((fit, pos));
        }

        var orderedAgents = fitness.OrderBy(agent => agent.score).Take(3).ToList();
        Assert.AreEqual(orderedAgents.Count, leadWolfs.Length);
        foreach ((GameObject lead, Vector3 newPos) in leadWolfs.Zip(orderedAgents,
                     (lead, agentTuple) => (lead, agentTuple.localPosition)))
        {
            LeadAgent leadAgent = lead.GetComponent<LeadAgent>();
            leadAgent.MoveIntoDirection(newPos);
        }

        // Let the agents do their thing.
        foreach (IAgent agent in agents)
        {
            agent.Step();
        }

        // Calculate target to hit
        Vector3 targetPos = targetPoint.localPosition;
        foreach (IAgent agent in agents)
        {
            if ((targetPos - agent.GetCurrentPosition()).magnitude <= targetHitRadius)
            {
                targetHit = true;
            }
        }

        if (targetHit)
        {
            agentGroup.AddGroupReward(-1f);
            Debug.Log($"Target got hit at round {stepsDone}");
        }

        // We could have two strategies here.
        // 1) Just give them a reward for being alive ("existential reward").
        // 2) Interpret more in the data. Like calculating average distance to target.
        //
        // 1 is harder for AI, but AI is more free what to do. With 2 we enforce behavior what makes it easier for
        // AI but reduces the possible things AI can explore.

        // 1)
        agentGroup.AddGroupReward(0.01f);

        // 2)
        // For training calculate the mean distance to the target
        if (false)
        {
            List<float> distances = new List<float>();
            foreach (IAgent agent in agents)
            {
                float distance = (agent.GetCurrentPosition() - targetPos).magnitude;
                distances.Add(distance);
            }

            float avgDistance = distances.Average();
            float maxDistance = maxDimensions.magnitude;
            float distanceFraction = avgDistance / maxDistance;
            Assert.IsTrue(distanceFraction <= 1f);
            agentGroup.AddGroupReward(distanceFraction);
        }

        stepsDone++;
    }

    /// <summary>
    ///     Decreases linearly  from 2 to 0
    ///     Start to go from 2 down at step 0.
    ///     Reached 0 and stop descending at `maxSteps`
    /// </summary>
    private void UpdateA()
    {
        float lin = 2f - 2f * ((float)stepsDone / (float)maxSteps);
        a = Mathf.Clamp(lin, 0f, 2f);
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
        return MathF.Min(delta.magnitude, maxVisionDistance);
    }

    public int NrObservations()
    {
        return 13;
    }

    public List<float> AgentObservations(Vector3 localAgentPos)
    {
        // We start with the information all agents get from the model.
        // If we look at `GWOAgent::Step()` we see that this is `a` and the
        // positions of the lead wolfs.
        List<float> ret = new List<float>(NrObservations()); // Init with capacity for performance

        ret.Add(a);

        Assert.AreEqual(leadWolfs.Length, 3);
        foreach (GameObject leadWolf in leadWolfs)
        {
            Vector3 dLead = leadWolf.transform.localPosition - localAgentPos;
            ret.Add(dLead.x);
            ret.Add(dLead.y);
            ret.Add(dLead.z);
        }

        // Extra info: Target relative to calling ML agent
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
        return maxDimensions;
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

        if (maxDimensions.y != maxDimensions.x || maxDimensions.z != maxDimensions.x)
        {
            Debug.LogError("The statistic subsystem can only be used with Playfields that are cubes.");
            return;
        }

        RoundStatisticDto obj = new RoundStatisticDto
        {
            EnvironmentType = RoundStatisticDto.EnvironmentTypeEnum.GWO,
            MaxRounds = maxSteps,
            PlayedRounds = stepsDone,
            WhoWon = haveAttackersWon
                ? RoundStatisticDto.WhoWonEnum.AttackerWon
                : RoundStatisticDto.WhoWonEnum.DefenderWon,
            LineOfSight = maxVisionDistance,
            NrAttackers = goodAgentCount,
            NrDefenders = badMlAgentCount,
            MaxSpeed = maxAgentVelocity,
            AreaSideLength = maxDimensions.x,
            TargetHitRadius = targetHitRadius
        };

        StatsEventSystem.current.OnRoundFinished(obj);
    }
}