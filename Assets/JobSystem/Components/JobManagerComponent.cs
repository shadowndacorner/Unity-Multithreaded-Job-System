using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobManagerComponent : MonoBehaviour
{
    public static JobManagerComponent _instance;
    public static JobManagerComponent Instance
    {
        get
        {
            if (!_instance)
            {
                var go  = new GameObject("JobManager");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<JobManagerComponent>();
            }
            return _instance;
        }
    }

    void OnEnable()
    {
        JobUtility.InitializeWorkers();
    }

    void OnDisable()
    {
        JobUtility.Shutdown();
    }

    void Update()
    {
        JobSystem.JobSystemCore.RunThreadedUpdate();
    }

    void FixedUpdate()
    {
        JobSystem.JobSystemCore.RunThreadedFixedUpdate();
    }
}
