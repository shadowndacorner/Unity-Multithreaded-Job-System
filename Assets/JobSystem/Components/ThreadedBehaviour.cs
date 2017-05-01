using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreadedBehaviour : MonoBehaviour {
    public virtual IEnumerator ThreadedUpdate()
    {
        ShouldThreadedUpdate = false;
        // Signals a break
        yield return null;
    }

    public virtual IEnumerator ThreadedFixedUpdate()
    {
        ShouldThreadedFixedUpdate = false;
        // Signals a break
        yield return null;
    }

    void OnEnable()
    {
        JobSystem.JobSystemCore.RegisterComponent(this);
    }

    void OnDisable()
    {
        JobSystem.JobSystemCore.UnregisterComponent(this);
    }

    public bool ShouldThreadedUpdate { get; internal set; }
    public bool ShouldThreadedFixedUpdate { get; internal set; }
}
