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

public class TestThread : ThreadedBehaviour {
    // Standard coroutine-styled job
    IEnumerator ComplicatedJob()
    {
        var start = System.DateTime.Now;
        // NOTE: This is DEFINITELY not the most efficient way to do this, but
        // it serves as an example for bouncing back and forth between the foreground
        // and the background
        List<Bounds> b = new List<Bounds>();
        
        // First we're going to sample all of the bounding
        // boxes in the scene, and for that we need to be on the main thread...
        yield return JobYields.SwitchToMain;
        foreach(var v in FindObjectsOfType<BoxCollider>())
        {
            b.Add(v.bounds);
        }

        // Now we'll switch to a worker to compute the centerpoint
        yield return JobYields.SwitchToWorker;
        Vector3 pos = Vector3.zero;
        for(int i = 0; i < b.Count; ++i)
        {
            pos += b[i].center;
        }
        pos /= (float)b.Count;

        double iterations = 0;
        double total = 0;

        // Now let's test the amount of time it takes to make an uncontested context switch
        for (int i = 0; i < 10000; ++i)
        {
            yield return JobYields.SwitchToMain;
            ++iterations;

            var startTime = System.DateTime.Now;
            yield return JobYields.SwitchToWorker;
            total += (System.DateTime.Now - startTime).TotalMilliseconds;
        }
        Debug.Log("Avg context switch time: " + (total / iterations) + " ms");

        // Switch back to the main thread to set our position after heavy work
        yield return JobYields.SwitchToMain;
        transform.position = pos;
    }

    IEnumerator JobInsideUpdate()
    {
        for (int i = 0; i < 10; ++i)
            yield return i;
    }

    void Start()
    {
        JobUtility.EnqueueJobOnMostFree(ComplicatedJob());
    }

    // If overridden from the baseclass, runs automatically every Update
    public override IEnumerator ThreadedUpdate()
    {
        // Run a job within a job
        var m_job = JobInsideUpdate();
        while (m_job.MoveNext())
        {
            int i = (int)m_job.Current;
            //Debug.Log(i); // prints 1-9
        }

        // Do some complex computation
        System.Threading.Thread.Sleep(3);

        for (int i = 0; i < 6; ++i)
        {
            // Run raycast
            yield return JobYields.SwitchToMain;
            var start = transform.position;
            var dir = transform.forward;
            RaycastHit hit;
            var didhit = Physics.Raycast(start, dir, out hit);

            yield return JobYields.SwitchToWorker;
            
            // Complicated math to process the raycast
            //System.Threading.Thread.Sleep(1);
        }
    }

    // If overridden from the baseclass, runs automatically every FixedUpdate
    public override IEnumerator ThreadedFixedUpdate()
    {
        // Currently does nothing other than demonstrating the fact that
        // FixedUpdate is threadable
        yield return null;
    }
}
