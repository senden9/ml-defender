using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
///     A GWO omega agent that has the goal to defend the target.
///     So manipulating the swarm.
///     Similar implementation than in my bachelors thesis, but in 3D, C#, Unity.
/// </summary>
public class FakeAgentBachelor : GwoOmegaAgent
{
    /// <summary>
    ///     Between 0 and 1.
    ///     0.0 = Do not go to the crowd
    ///     1.0 = Do what the crowd want. No bad movement
    /// </summary>
    [Range(0f, 1f)] public float goToCrowdFactor = 0.2f;

    public float fitnessLiar = 2000;

    public override void Step()
    {
        Assert.IsTrue(goToCrowdFactor <= 1f && goToCrowdFactor >= 0f);

        float sqrt3 = Mathf.Sqrt(3);
        float maxVelocity = simulationModel.MaxAgentMovementPerStep();
        Vector3 fakePoint = transform.localPosition +
                            new Vector3(sqrt3, sqrt3, sqrt3) * maxVelocity * (1 - goToCrowdFactor);
        ClampPosition(ref fakePoint);
        transform.localPosition = fakePoint;
        base.Step();
    }

    public override float Fitness(Vector3 atPosition)
    {
        // Yup, we send fake data here. So we are a bad Wolf
        return base.Fitness(atPosition) - fitnessLiar;
    }
}