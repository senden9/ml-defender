using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FpsLogger : MonoBehaviour
{
    // For the "normal" update
    private float[] deltaTimes = new float [6000];
    private int deltaTimeIndex = 0;
    
    // For the fixed update
    private double[] fixedDeltaTimes = new double[6000];
    private int fixedDeltaTimeIndex = 0;
    private double latestFixedUpdate = 0;

    private void OnEnable()
    {
        latestFixedUpdate = Time.realtimeSinceStartupAsDouble;
    }

    // Update is called once per frame
    void Update()
    {
        deltaTimes[deltaTimeIndex] = Time.deltaTime;
        deltaTimeIndex = (deltaTimeIndex + 1) % deltaTimes.Length;

        // Log out data
        if (deltaTimeIndex == 0)
        {
            int framesMeasured = deltaTimes.Length;
            float meanDeltaTime = deltaTimes.Sum() / framesMeasured;
            float meanFps = 1f / meanDeltaTime;
            Debug.Log( $"Measured {framesMeasured} frames, average fps: {meanFps:0.00}, average frame time {(meanDeltaTime*1000f):0.00}ms");
        }
    }
    
    void FixedUpdate()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        fixedDeltaTimes[fixedDeltaTimeIndex] = now - latestFixedUpdate;
        latestFixedUpdate = now;
        fixedDeltaTimeIndex = (fixedDeltaTimeIndex + 1) % fixedDeltaTimes.Length;

        // Log out data
        if (fixedDeltaTimeIndex == 0)
        {
            int framesMeasured = fixedDeltaTimes.Length;
            double meanDeltaTime = fixedDeltaTimes.Sum() / framesMeasured;
            double meanPhysicsFps = 1f / meanDeltaTime;
            Debug.Log( $"Measured {framesMeasured} PHYSICS UPDATES, average fps: {meanPhysicsFps:0.00}, average frame time {(meanDeltaTime*1000f):0.00}ms");

        }
    }
}
