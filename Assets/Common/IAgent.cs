using System;
using UnityEngine;

/// <summary>
///     Interface for agents.
///     GWO and SlimeMould should be both implement this.
///     That way we can later mix ML defenders.
/// </summary>
public interface IAgent
{
    /// <summary>
    ///     Objective function. Can evaluate a position. Lower values are better.
    /// </summary>
    public delegate float Objf(Vector3 position);

    /// <summary>
    ///     Index of the agent. Used to identify the agent in the crowd for statistic purposes.
    ///     Optional but practical. So a no-op default impl.
    /// </summary>
    public void SetIdx(uint newIdx)
    {
    }

    /// <summary>
    ///     Returns current Index of agent.
    ///     Default throw exception. If one implements the `SetIdx` `GetIdx` should also be implemented.
    /// </summary>
    public uint GetIdx()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     Fitness at current position. Lower is better
    /// </summary>
    public float Fitness()
    {
        return Fitness(GetCurrentPosition());
    }

    /// <summary>
    ///     Fitness at given position. Lower is better
    /// </summary>
    public float Fitness(Vector3 atPosition);

    /// <summary>
    ///     Do one step of the Simulation
    /// </summary>
    public void Step();

    /// <summary>
    ///     Some models have a split Step/Advance setup.
    ///     There for each round first `Step` is called for preparation.
    ///     After that `advance` is called executing the actual movement.
    ///     Optional, so a default no-op implementation is given.
    /// </summary>
    public void Advance()
    {
    }

    public void SetSimulationModel(IModel model);
    public void SetObjectiveFunction(Objf objf);

    /// <summary>
    ///     Returns the current position local in the training area.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetCurrentPosition();

    /// <summary>
    ///     Sets the current position of the agent in the local training-area system.
    ///     No bounds check is done here!
    /// </summary>
    public void SetCurrentPosition(Vector3 pos);

    /// <summary>
    ///     Reset internal state for next simulation run.
    /// </summary>
    public void ResetState()
    {
    }
}