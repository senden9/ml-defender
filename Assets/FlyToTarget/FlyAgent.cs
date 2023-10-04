using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class FlyAgent : Agent
{
    Rigidbody rBody;
    public Transform Target;
    public float RoundDuration;

    public float ElapsedTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        ElapsedTime = 0;
        // Zero agent's momentum
        rBody.angularVelocity = Vector3.zero;
        rBody.velocity = Vector3.zero;
        transform.localPosition = new Vector3(0, 0.5f, 0);
        transform.localRotation = Quaternion.identity;

        // Move the target to a new spot
        Target.localPosition = new Vector3(Random.value * 30 - 15,
            Random.value * 10 - 4,
            Random.value * 30 - 15);
        Target.localRotation = Quaternion.identity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var targetInformation = TargetInformation();
        // Target information
        sensor.AddObservation(targetInformation.direction);
        sensor.AddObservation(Mathf.Clamp(targetInformation.distance, 0f, 8f) / 8f);

        // Down direction, for orientation
        sensor.AddObservation(transform.TransformDirection(Vector3.down));

        // Agent velocities
        sensor.AddObservation(rBody.velocity);
        sensor.AddObservation(rBody.angularVelocity);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        ElapsedTime += Time.fixedDeltaTime;
        Vector4 controlSignal = Vector4.zero;
        controlSignal.w = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        controlSignal.x = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        controlSignal.y = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        controlSignal.z = Mathf.Clamp(actions.ContinuousActions[3], -1f, 1f);
        ApplyForce(controlSignal);

        float distanceToTarget = TargetInformation().distance;

        // Give out points for narrow to target.
        float narrowReward = (1 - Mathf.Clamp(distanceToTarget, 0f, 8f) / 8f);
        AddReward(narrowReward);

        // Too far down
        if (transform.localPosition.y < -80f)
        {
            SetReward(-1f);
            EndEpisode();
        }

        // End of simulation
        if (ElapsedTime >= RoundDuration)
        {
            EndEpisode();
        }
    }

    public float MaxForce = 30;

    private void ApplyForce(Vector4 forces)
    {
        //Apply a force to this Rigidbody in direction of this GameObjects up axis
        float thrustPerPoint = MaxForce / 4;
        Vector3 pos1 = Vector3.left;
        float f1 = forces.w * thrustPerPoint;
        Vector3 pos2 = Vector3.right;
        float f2 = forces.x * thrustPerPoint;
        Vector3 pos3 = Vector3.forward;
        float f3 = forces.y * thrustPerPoint;
        Vector3 pos4 = Vector3.back;
        float f4 = forces.z * thrustPerPoint;
        //Debug.Log($"{f1} + {f2} + {f1} + {f2} = {f1+f2+f3+f4}");

        Transform t = transform;
        Vector3 globalF1 = t.TransformVector(Vector3.up * f1);
        Vector3 globalF2 = t.TransformVector(Vector3.up * f2);
        Vector3 globalF3 = t.TransformVector(Vector3.up * f3);
        Vector3 globalF4 = t.TransformVector(Vector3.up * f4);
        Vector3 globalPos1 = t.TransformPoint(pos1);
        Vector3 globalPos2 = t.TransformPoint(pos2);
        Vector3 globalPos3 = t.TransformPoint(pos3);
        Vector3 globalPos4 = t.TransformPoint(pos4);

        rBody.AddForceAtPosition(globalF1, globalPos1);
        rBody.AddForceAtPosition(globalF2, globalPos2);
        rBody.AddForceAtPosition(globalF3, globalPos3);
        rBody.AddForceAtPosition(globalF4, globalPos4);
    }

    private (Vector3 direction, float distance) TargetInformation()
    {
        // Gets a vector that points from the agent position to the target.
        Vector3 heading = Target.localPosition - transform.localPosition;
        var distance = heading.magnitude;
        heading.Normalize();
        return (heading, distance);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetAxis("Horizontal");
        continuousActionsOut[3] = Input.GetAxis("Vertical");
    }
}