using UnityEngine;

public class Forcer : MonoBehaviour
{
    private Rigidbody Rigidbody;

    [Range(0f, 20f)] public float TotalThrust;
    [Range(-1f, 1f)] public float ThrustSplitH = 0;
    [Range(-1f, 1f)] public float ThrustSplitV = 0;

    public float SplitChangeRate = 0.02f;

    // Start is called before the first frame update
    void Start()
    {
        //Fetch the Rigidbody from the GameObject with this script attached
        Rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        {
            float h = Input.GetAxis("Horizontal");
            ThrustSplitH += h * SplitChangeRate;
            ThrustSplitH = Mathf.Clamp(ThrustSplitH, -1f, 1f);

            float v = Input.GetAxis("Vertical");
            ThrustSplitV += v * SplitChangeRate;
            ThrustSplitV = Mathf.Clamp(ThrustSplitV, -1f, 1f);
        }
        if (Input.GetButton("Jump"))
        {
            //Apply a force to this Rigidbody in direction of this GameObjects up axis
            float thrustPerPoint = TotalThrust / 4;
            Vector3 pos1 = Vector3.left;
            float f1 = (ThrustSplitH + 1f) * thrustPerPoint;
            Vector3 pos2 = Vector3.right;
            float f2 = -(ThrustSplitH - 1f) * thrustPerPoint;
            Vector3 pos3 = Vector3.forward;
            float f3 = (ThrustSplitV + 1f) * thrustPerPoint;
            Vector3 pos4 = Vector3.back;
            float f4 = -(ThrustSplitV - 1f) * thrustPerPoint;
            Debug.Log($"{f1} + {f2} + {f1} + {f2} = {f1 + f2 + f3 + f4}");

            Transform t = transform;
            Vector3 globalF1 = t.TransformVector(Vector3.up * f1);
            Vector3 globalF2 = t.TransformVector(Vector3.up * f2);
            Vector3 globalF3 = t.TransformVector(Vector3.up * f3);
            Vector3 globalF4 = t.TransformVector(Vector3.up * f4);
            Vector3 globalPos1 = t.TransformPoint(pos1);
            Vector3 globalPos2 = t.TransformPoint(pos2);
            Vector3 globalPos3 = t.TransformPoint(pos3);
            Vector3 globalPos4 = t.TransformPoint(pos4);

            Rigidbody.AddForceAtPosition(globalF1, globalPos1);
            Rigidbody.AddForceAtPosition(globalF2, globalPos2);
            Rigidbody.AddForceAtPosition(globalF3, globalPos3);
            Rigidbody.AddForceAtPosition(globalF4, globalPos4);
        }
    }
}