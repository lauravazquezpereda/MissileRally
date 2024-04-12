using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiRollBar : MonoBehaviour
{

    public WheelCollider wheelLeft, wheelRight;
    private Rigidbody carRigidbody;

    public float AntiRoll = 5000.0f;

    // Start is called before the first frame update
    void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        WheelHit hit = new WheelHit();
        float travelL = 1.0f;
        float travelR = 1.0f;

        bool groundedL = wheelLeft.GetGroundHit(out hit);

        if (groundedL)
        {
            travelL = (-wheelLeft.transform.InverseTransformPoint(hit.point).y - wheelLeft.radius) / wheelLeft.suspensionDistance;
        }

        bool groundedR = wheelRight.GetGroundHit(out hit);

        if (groundedR)
        {
            travelR = (-wheelRight.transform.InverseTransformPoint(hit.point).y - wheelRight.radius) / wheelRight.suspensionDistance;
        }

        float antiRollForce = (travelL - travelR) * AntiRoll;

        if (groundedL) carRigidbody.AddForceAtPosition(wheelLeft.transform.up * -antiRollForce, wheelLeft.transform.position);
        if (groundedR) carRigidbody.AddForceAtPosition(wheelRight.transform.up * antiRollForce, wheelRight.transform.position);
    }
}
