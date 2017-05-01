using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestThread : ThreadedBehaviour {
    public override IEnumerator ThreadedUpdate()
    {
        yield return JobYields.SwitchToMain;

        var start = transform.position;
        var dir = transform.forward;
        RaycastHit hit;
        var didhit = Physics.Raycast(start, dir, out hit);
        if (didhit)
        {
            Debug.DrawLine(start, hit.point, Color.green);
        }
        else
        {
            Debug.DrawRay(start, dir, Color.red);
        }

        yield return JobYields.SwitchToWorker;
    }

    public override IEnumerator ThreadedFixedUpdate()
    {
        yield return null;
    }
}
