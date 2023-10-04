using System;
using UnityEngine;

public class LeadAgent : GwoOmegaAgent
{
    public override void Step()
    {
        throw new NotSupportedException("Lead wolfs do not make steps. They are driven from the model");
    }

    public void MoveIntoDirection(Vector3 newPos)
    {
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
}