using UnityEngine;
using UnityEngine.Assertions;

public class GwoOmegaAgent : MonoBehaviour, IAgent
{
    public IAgent.Objf objf;
    public GWOModel simulationModel;

    public float Fitness()
    {
        return Fitness(transform.localPosition);
    }

    public virtual float Fitness(Vector3 atPosition)
    {
        return objf(atPosition);
    }

    protected void ClampPosition(ref Vector3 pos)
    {
        Vector3 maxDim = simulationModel.maxDimensions;
        pos.x = Mathf.Clamp(pos.x, -maxDim.x / 2f, maxDim.x / 2f);
        pos.y = Mathf.Clamp(pos.y, -maxDim.y / 2f, maxDim.y / 2f);
        pos.z = Mathf.Clamp(pos.z, -maxDim.z / 2f, maxDim.z / 2f);
    }

    public virtual void Step()
    {
        Vector3[] leadX = new Vector3[simulationModel.leadWolfs.Length];
        for (var idx = 0; idx < simulationModel.leadWolfs.Length; idx++)
        {
            var lead = simulationModel.leadWolfs[idx];
            Vector3 r1 = new Vector3(Random.value, Random.value, Random.value);
            Vector3 r2 = new Vector3(Random.value, Random.value, Random.value);

            Vector3 aVec = Vector3.one * simulationModel.a;
            Vector3 a1 = 2 * aVec.ElementProduct(r1) - aVec;
            Vector3 c1 = 2 * r2;

            Vector3 leadPos = lead.transform.localPosition;
            Vector3 dLead = (c1.ElementProduct(leadPos) - transform.localPosition).ElementAbs();
            leadX[idx] = leadPos - a1.ElementProduct(dLead);
        }

        Vector3 newPos = leadX.ElementAverage();
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
        Assert.AreEqual(typeof(GWOModel), model.GetType(), "GWO Agent works only for GWO models!");
        simulationModel = model as GWOModel;
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
}