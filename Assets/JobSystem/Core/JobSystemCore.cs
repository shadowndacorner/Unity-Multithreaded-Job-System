/*
    Copyright (c) 2017 Ian Diaz

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE. 
*/

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
