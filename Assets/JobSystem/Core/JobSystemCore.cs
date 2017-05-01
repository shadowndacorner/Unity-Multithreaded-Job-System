using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JobSystem
{
    public static class JobSystemCore
    {
        static JobSystemCore()
        {
            ActiveComponents = new List<ThreadedBehaviour>();
            if (!JobManagerComponent.Instance)
                throw new System.InvalidOperationException("Unable to start up JobManager");
        }

        static List<ThreadedBehaviour> ActiveComponents;
        public static void RegisterComponent(ThreadedBehaviour component)
        {
            ActiveComponents.Add(component);
        }

        public static void UnregisterComponent(ThreadedBehaviour component)
        {
            ActiveComponents.Remove(component);
        }

        public static void RunThreadedFixedUpdate()
        {
            Time.Update();
            foreach (var v in ActiveComponents)
            {
                if (v.ShouldThreadedFixedUpdate)
                    JobUtility.EnqueueJobOnMostFree(v.ThreadedFixedUpdate());
            }

            while (JobUtility.HasJobs())
            {
                JobUtility.RunJobOnMainThread();
            }
        }

        public static void RunThreadedUpdate()
        {
            Time.Update();
            foreach (var v in ActiveComponents)
            {
                if (v.ShouldThreadedUpdate)
                    JobUtility.EnqueueJobOnMostFree(v.ThreadedUpdate());
            }
            while (JobUtility.HasJobs())
            {
                JobUtility.RunJobOnMainThread();
            }
        }
    }
}
