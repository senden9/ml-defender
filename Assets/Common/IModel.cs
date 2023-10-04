using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     This interface should be implemented by all models.
///     That way we can create generic implementations ML-Defender agents that will implement `IAgent`.
/// </summary>
public interface IModel
{
    public delegate void SimFinished();
    
    /// <summary>
    ///     Number of observations for the `AgentObservations` method.
    ///     Agents assume that this number positive arbitrary but fixed during runtime.
    /// </summary>
    public int NrObservations();

    /// <summary>
    ///     Return some observations for ML Agents.
    ///     The returned list need to be exactly `Observations` long.
    ///     The order of the observations must be fixed. Example: "pos.x, pos.y, pos.z, paramA".
    /// </summary>
    /// <param name="localAgentPos">Local position of the calling agent</param>
    public List<float> AgentObservations(Vector3 localAgentPos);

    /// <summary>
    ///     Returns the nearest `count` agents relative to `localCenterPos`.
    /// </summary>
    /// <param name="localCenterPos">Position we want to use to be relative to.</param>
    /// <param name="count">Maximum number of return values.</param>
    /// <param name="ignoreSelf">If `true` the position of `localCenterPos` will not be counted if seen first</param>
    /// <returns>A list of vectors relative to `localCenterPos`. Ordered from near to far. Has at most `count` elements.</returns>
    public List<Vector3> NearestXAgents(Vector3 localCenterPos, int count, bool ignoreSelf = true);

    /// <summary>
    ///     Returns the boundaries of the playfield.
    ///     Each component of the vector will be interpreted like the side lenght of a cuboid.
    ///     It will be assumed that the center of the cuboid is at position (0,0,0).
    ///     So valid coordinates are between (-x/2, -y/2, -z/2) and (x/2, y/2, z/2).
    /// </summary>
    /// <returns></returns>
    public Vector3 PlayfieldSize();

    /// <summary>
    ///     Returns the maximal velocity for an agent in m / step.
    /// </summary>
    public float MaxAgentMovementPerStep();

    /// <summary>
    /// Function must be called after each simulation.
    /// After the agents reported back to the ML-Framework and possible statistics are written.
    /// The caller will *maybe* destroyed after calling of the delegate.
    /// </summary>
    /// <param name="func"></param>
    public void SetSimulationFinishedEvent(SimFinished func);
}