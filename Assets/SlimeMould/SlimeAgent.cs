using System;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class SlimeAgent : MonoBehaviour, IAgent
{
    protected SlimeModel simulationModel;

    [NonSerialized] public uint idx;
    protected IAgent.Objf objf;
    private Vector3 weight;

    /// <summary>
    ///     Eq 2.5
    ///     Calculates the weight depending on the smell index
    ///     idx: Index of the agent. Zero based.
    ///     nr_agents: Number of agents in total.
    ///     bf: As in the paper, bF denotes the optimal fitness obtained in the
    ///     current iterative process.
    ///     wf: As in the paper, wF denotes the worst fitness value obtained in
    ///     the iterative process currently.
    ///     si: fitness of current agent
    /// </summary>
    protected void CalcW()
    {
        float bf = simulationModel.bestFitness;
        float wf = simulationModel.worstFitness;
        float si = Fitness();
        bool condition = (idx + 1) <= (simulationModel.TotalAgents());
        Vector3 r = new Vector3( // Values [0, 1)
            Random.value,
            Random.value,
            Random.value
        );
        if (condition)
        {
            weight = Vector3.one + r * Mathf.Log10((bf - si) / (bf - wf) + 1);
        }
        else
        {
            weight = Vector3.one - r * Mathf.Log10((bf - si) / (bf - wf) + 1);
        }
    }

    /// <summary>
    ///     Phase 1 of each round. Prepare values etc
    /// </summary>
    public void Step()
    {
        // Fitness update is now in the `FixedUpdate` of the slime model
    }

    /// <summary>
    ///     Phase 2 of each round. Use values from phase one to move forward
    /// </summary>
    public virtual void Advance()
    {
        CalcW();
        SetPosition(UpdatePosition());
    }

    protected void SetPosition(Vector3 newPosition)
    {
        Vector3 dx = newPosition - transform.localPosition;
        float ds = dx.magnitude;

        // Limit velocity
        float maxAgentMovementPerStep = simulationModel.MaxAgentMovementPerStep();
        if (ds > maxAgentMovementPerStep)
        {
            dx *= maxAgentMovementPerStep / ds;
            newPosition = transform.localPosition + dx;
        }

        // Clamp to area
        Vector3 maxDim = Vector3.one * simulationModel.maxDimensions;
        newPosition.x = Mathf.Clamp(newPosition.x, -maxDim.x / 2f, maxDim.x / 2f);
        newPosition.y = Mathf.Clamp(newPosition.y, -maxDim.y / 2f, maxDim.y / 2f);
        newPosition.z = Mathf.Clamp(newPosition.z, -maxDim.z / 2f, maxDim.z / 2f);

        if (float.IsNaN(newPosition.x) || float.IsNaN(newPosition.y) || float.IsNaN(newPosition.z))
        {
            Debug.LogError($"Detected NaN position in Agent {idx}!");
        }

        transform.localPosition = newPosition;
    }

    public virtual float Fitness()
    {
        return objf(transform.localPosition);
    }

    public virtual float Fitness(Vector3 atPosition)
    {
        return objf(atPosition);
    }

    /// <summary>
    ///     Eq 2.7
    ///     x: current position of the agent
    ///     lb: lower bound of search range
    ///     ub: upper bound of search range
    ///     z: behaviour parameter from rango [0,1]
    ///     si: fitness of current agent
    ///     df: Best fitness obtained in all iterations for one (current) agent
    ///     xa: Position of a randomly selected agent
    ///     xb: Position of a randomly selected agent
    ///     x_best: represents the individual location with the highest odour concentration
    ///     currently found
    ///     vc: Decreases linearly from one (start) to zero (at max iteration).
    ///     w: Smell-Weight
    ///     t: current time (or iteration)
    ///     t_max: highest possible time / target iteration number
    /// </summary>
    /// <returns></returns>
    protected Vector3 UpdatePosition()
    {
        if (Random.value < simulationModel.z)
        {
            // Jup, that next line creates a array that has the same numbers on
            // all places. I think that does not make sense but it is the same way
            // as in the MATLAB reference implementation:
            // `X(i,:) = (ub-lb)*rand+lb;`
            // The paper says nothing about that. Maybe the reference implementation
            // is wrong?
            // This results in a placement on a diagonal line on the play field

            // Code matching the code above and the matlab implementation
            /*
            float ub = this.simulationModel.maxDimensions / 2f;
            float lb = -this.simulationModel.maxDimensions / 2f;
            float val = Random.value * (ub - lb) + lb;
            return Vector3.one * val;
            */

            // Code that seams to match more the intend of the paper instead of the reference implementation.
            Vector3 rand = Random.insideUnitSphere * simulationModel.maxAgentVelocity;
            return transform.localPosition + rand;
        }
        else
        {
            float vc = 1f - ((float)simulationModel.stepsDone / ((float)simulationModel.maxSteps));
            return CalcNextX(vc);
        }
    }

    /// <summary>
    ///     Eq 2.1
    ///     The following rule is proposed to imitate the contraction mode.
    ///     x: current position of the agent
    ///     si: fitness of current agent
    ///     df: Best fitness obtained in all iterations for one (current) agent
    ///     xa: Position of a randomly selected agent
    ///     xb: Position of a randomly selected agent
    ///     x_best: represents the individual location with the highest odour concentration
    ///     currently found
    ///     vc: Decreases linearly from one (start) to zero (at max iteration).
    ///     w: Smell-Weight
    ///     t: current time (or iteration)
    ///     t_max: highest possible time / target iteration number
    /// </summary>
    /// <returns></returns>
    protected Vector3 CalcNextX(float vc)
    {
        Assert.IsTrue((0 <= vc) & (vc <= 1), "VC must be between zero and one.");
        float r = Random.value;
        float p = CalcP();
        float a = CalcA();
        Vector3 vb = CalcVB(a);
        Vector3 x = transform.localPosition;
        Vector3 xBest = simulationModel.bestPosition;
        IAgent agentA = simulationModel.GetRandomAgent();
        IAgent agentB = simulationModel.GetRandomAgent();
        Vector3 xa = agentA.GetCurrentPosition();
        Vector3 xb = agentB.GetCurrentPosition();
        if (r < p)
        {
            return xBest + vb.ElementProduct(weight.ElementProduct(xa) - xb);
        }
        else
        {
            Vector3 vcVec = CalcVB(vc);
            return vcVec.ElementProduct(x);
        }
    }

    /// <summary>
    ///     Eq 2.2
    ///     si: fitness of current agent
    ///     df: Best fitness obtained in all iterations
    /// </summary>
    /// <returns></returns>
    protected float CalcP()
    {
        float si = Fitness();
        float df = simulationModel.destinationFitness;
        return (float)Math.Tanh(Math.Abs(si - df));
    }

    /// <summary>
    ///     Eq 2.3
    /// </summary>
    /// <returns></returns>
    protected Vector3 CalcVB(float a)
    {
        return new Vector3(
            Random.Range(-a, a),
            Random.Range(-a, a),
            Random.Range(-a, a)
        );
    }

    /// <summary>
    ///     Eq 2.4
    ///     t: current time / iteration number
    ///     max_t: End of simulation time / iteration number
    /// </summary>
    /// <returns></returns>
    protected float CalcA()
    {
        return (float)Math.Atanh(-((double)simulationModel.stepsDone / (double)simulationModel.maxSteps) + 1);
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

    public void SetIdx(uint newIdx)
    {
        idx = newIdx;
    }

    public uint GetIdx()
    {
        return idx;
    }
}