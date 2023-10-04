#region

using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Assertions;

#endregion

public class MergeMlDefender : Agent, IAgent
{
    private uint _modelIdx;
    private IAgent.Objf _objf;
    private IModel _model;

    /// <summary>
    ///     Maximum number of floats per model.
    ///     Not necessary exact. Simply a upper limit. Missing values will be filled in with 0.
    /// </summary>
    private const int MaxModelObservations = 13;

    /// <summary>
    ///     Number of nearest agents we want to observe.
    /// </summary>
    private const int ObserveXNearestAgents = 3;

    // Values given from ML action, used to manipulate fitness.
    // Are between -1 and 1 because that is what Unity-ML with this trainer returns as continuous action.
    private float fitFac = 1;
    private float fitAdd = 0;

    // Next move direction from ML
    // All components are between -1 and 1.
    private Vector3 moveVector = Vector3.zero;

    public uint GetIdx()
    {
        return _modelIdx;
    }

    public void SetIdx(uint newIdx)
    {
        _modelIdx = newIdx;
    }

    public float Fitness(Vector3 atPosition)
    {
        // We need some kind of formula here.
        // I go with `y = fitFac*x + fitAdd` where
        //  y = return value
        //  x = objf(atPosition), scalar
        //  fitFac = factor (scalar) from ML
        //  fitAdd = additive value (scalar) from ML
        // Also we scale the "fit…" values because they are between -1 and 1.
        float x = _objf(atPosition);
        float y = (fitFac * 10 * x) + (fitAdd * 10);
        return y;
    }

    public void Step()
    {
        // All elements of `moveVector` are between 0 and 1. Scale them with max speed
        float maxSpeed = _model.MaxAgentMovementPerStep();
        Vector3 newPos = transform.localPosition + (moveVector * maxSpeed);

        // Limit speed & clamp position
        ClampPosition(ref newPos);
        Vector3 directionVector = newPos - transform.localPosition;
        float targetDistance = directionVector.magnitude;
        float maxAgentMovementPerStep = _model.MaxAgentMovementPerStep();
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
        Assert.IsTrue(model.NrObservations() <= MaxModelObservations,
            "Number of MaxModelObservations must be a upper limit for all models! Maybe raise the value of `MaxModelObservations`? Note that this implies a re-training this agent!");
        _model = model;
    }

    public void SetObjectiveFunction(IAgent.Objf objf)
    {
        _objf = objf;
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
    ///     Nr of observations for that sensor is 3 * `ObserveXNearestAgents` + `MaxModelObservations`
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Model observations
        List<float> modelObservations = _model.AgentObservations(GetCurrentPosition());
        Assert.IsTrue(modelObservations.Count <= MaxModelObservations,
            "Buggy model implementation! To much observations returned");

        // fill up observations with zeros for constant size
        while (modelObservations.Count < MaxModelObservations)
        {
            modelObservations.Add(0f);
        }

        sensor.AddObservation(modelObservations);

        // Neighbor observations
        var neighbors = _model.NearestXAgents(GetCurrentPosition(), ObserveXNearestAgents, ignoreSelf: true);
        foreach (Vector3 neighbor in neighbors)
        {
            sensor.AddObservation(neighbor);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Fetch next-move-vector
        moveVector.x = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        moveVector.y = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        moveVector.z = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);

        // Fetch fitness function parameters
        fitAdd = Mathf.Clamp(actions.ContinuousActions[3], -1f, 1f);
        fitFac = Mathf.Clamp(actions.ContinuousActions[4], -1f, 1f);
    }

    /// <summary>
    ///     Limit vector so it is in bounds of the playfield.
    /// </summary>
    private void ClampPosition(ref Vector3 pos)
    {
        Vector3 size = _model.PlayfieldSize();
        pos.x = Mathf.Clamp(pos.x, -size.x / 2f, size.x / 2f);
        pos.y = Mathf.Clamp(pos.y, -size.y / 2f, size.y / 2f);
        pos.z = Mathf.Clamp(pos.z, -size.z / 2f, size.z / 2f);
    }
}