using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Assertions;

public class DefenderMLAgent : Agent, IAgent
{
    private IAgent.Objf objf;
    private SlimeModel simulationModel;
    private uint idx;

    // Values given from ML action, used to manipulate fitness.
    // Are between -1 and 1 because that is what Unity-ML with this trainer returns as continuous action.
    private float fitFac;
    private float fitAdd;

    // Next move direction from ML
    // All components are between -1 and 1.
    private Vector3 moveVector = Vector3.zero;

    public float Fitness(Vector3 atPosition)
    {
        // We need some kind of formula here.
        // I go with `y = fitFac*x + fitAdd` where
        //  y = return value
        //  x = objf(atPosition), scalar
        //  fitFac = factor (scalar) from ML
        //  fitAdd = additive value (scalar) from ML
        // Also we scale the "fit…" values because they are between -1 and 1.
        float x = objf(atPosition);
        float y = (fitFac * 10 * x) + (fitAdd * 10);
        return y;
    }

    public void SetIdx(uint newIdx)
    {
        idx = newIdx;
    }

    public uint GetIdx()
    {
        return idx;
    }

    public void Step()
    {
    }

    public void Advance()
    {
        // All elements of `moveVector` are between 0 and 1. Scale them with max speed
        float maxSpeed = simulationModel.MaxAgentMovementPerStep();
        Vector3 newPos = transform.localPosition + (moveVector * maxSpeed);

        // Limit speed & clamp position
        ClampPosition(ref newPos);
        Vector3 directionVector = newPos - transform.localPosition;
        float targetDistance = directionVector.magnitude;
        float maxAgentMovementPerStep = simulationModel.MaxAgentMovementPerStep();
        if (targetDistance <= maxAgentMovementPerStep)
        {
            transform.localPosition = newPos;
        }
        else
        {
            directionVector.Normalize();
            directionVector *= maxAgentMovementPerStep;
            newPos = transform.localPosition + directionVector;
            transform.localPosition = newPos;
        }
    }

    public void SetSimulationModel(IModel model)
    {
        Assert.AreEqual(typeof(SlimeModel), model.GetType(), "Slime Agent works only for slime models!");
        simulationModel = model as SlimeModel;
    }

    public void SetObjectiveFunction(IAgent.Objf objf)
    {
        this.objf = objf;
    }

    public Vector3 GetCurrentPosition()
    {
        return transform.localPosition;
    }

    public void SetCurrentPosition(Vector3 pos)
    {
        transform.localPosition = pos;
    }

    /// <summary>
    ///     Limit vector so it is in bounds of the playfield.
    /// </summary>
    private void ClampPosition(ref Vector3 pos)
    {
        float maxDim = simulationModel.maxDimensions;
        pos.x = Mathf.Clamp(pos.x, -maxDim / 2f, maxDim / 2f);
        pos.y = Mathf.Clamp(pos.y, -maxDim / 2f, maxDim / 2f);
        pos.z = Mathf.Clamp(pos.z, -maxDim / 2f, maxDim / 2f);
    }

    // Unity-ML-Agent functions

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Fetch next-move-vector
        moveVector.x = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        moveVector.y = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
        moveVector.z = Mathf.Clamp(actionBuffers.ContinuousActions[2], -1f, 1f);

        // Fetch fitness function parameters
        fitAdd = Mathf.Clamp(actionBuffers.ContinuousActions[3], -1f, 1f);
        fitFac = Mathf.Clamp(actionBuffers.ContinuousActions[4], -1f, 1f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 ownPosition = transform.localPosition;

        // We start with the information all agents get from the model.
        sensor.AddObservation(simulationModel.bestFitness);
        sensor.AddObservation(simulationModel.worstFitness);
        // Percent done - to estimate the stage of behaviour we are at
        sensor.AddObservation((float)simulationModel.stepsDone / simulationModel.maxSteps);

        // Add nearest X agent position relative to our self.
        List<Vector3> neighbours = simulationModel.NearestXAgents(GetCurrentPosition(), 3);
        foreach (Vector3 neighbour in neighbours)
        {
            sensor.AddObservation(neighbour);
        }

        // Extra info: Target relative to us
        Vector3 dTarget = simulationModel.targetPoint.localPosition - ownPosition;
        sensor.AddObservation(dTarget);
    }
}