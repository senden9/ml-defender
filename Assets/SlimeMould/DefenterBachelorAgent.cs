using System;
using UnityEngine;
using UnityEngine.Assertions;

public class DefenterBachelorAgent : SlimeAgent
{
    public float badSlimeLiar = 1000;

    /// <summary>
    ///     Between 0 and 1.
    ///     0 = Do not go to the crowd
    ///     1 = Do what the crowd want. No bad movement
    /// </summary>
    [Range(0.0f, 1.0f)] public float goToCrowdFactor;

    public override float Fitness()
    {
        return objf(transform.localPosition) - badSlimeLiar;
    }

    public override float Fitness(Vector3 atPosition)
    {
        return objf(atPosition) - badSlimeLiar;
    }

    public override void Advance()
    {
        CalcW();
        Vector3 goodNewPosition = UpdatePosition();

        Assert.IsTrue(goToCrowdFactor <= 1.0f);
        Assert.IsTrue(goToCrowdFactor >= 0.0f);
        var invSqrt3 = 1.0f / MathF.Sqrt(3);

        Vector3 fakePoint =
            goodNewPosition +
            (Vector3.one * (invSqrt3 * simulationModel.maxAgentVelocity * (1f - goToCrowdFactor)));

        SetPosition(fakePoint);
    }
}