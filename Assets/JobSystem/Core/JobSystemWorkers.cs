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
using System.Threading;
using System.Linq;
using UnityEngine;

using Time = JobSystem.Time;
public static class JobYields
{
    public static object SwitchToWorker = new object();
    public static object SwitchToMain = new object();
    public static object Yield = new object();

    public class BaseJobYield : System.IComparable
    {
        public int CompareTo(object yield)
        {
            BaseJobYield rhs = (BaseJobYield)yield;
            bool m_yield = ShouldYield();
            bool r_yield = rhs.ShouldYield();

            if (m_yield && r_yield)
                return 0;

            if (m_yield)
                return -1;

            return 1;
        }

        public virtual bool ShouldYield()
        {
            return false;
        }
    }

    public class WaitForSeconds : BaseJobYield
    {
        public WaitForSeconds(float seconds)
        {
            endTime = Time.timeSinceLevelLoad + seconds;
        }

        public override bool ShouldYield()
        {
            return Time.timeSinceLevelLoad < endTime;
        }

        private float endTime;
    }
}

public static class JobUtility
{
    public class SynchronizedList<T>
    {
        public SynchronizedList()
        {
            m_List = new List<T>();
            m_lock = new object();
        }

        public SynchronizedList(int size)
        {
            m_List = new List<T>(size);
            m_lock = new object();
        }

        public SynchronizedList(SynchronizedList<T> queue)
        {
            m_List = new List<T>(queue.m_List);
            m_lock = new object();
        }

        public void Add(T type)
        {
            lock (m_lock)
                m_List.Add(type);
        }
        
        public int Count
        {
            get
            {
                return m_List.Count;
            }
        }

        public T this[int i]
        {
            get
            {
                return m_List[i];
            }
            set
            {
                lock(m_lock)
                    m_List[i] = value;
            }
        }

        private List<T> m_List;
        private object m_lock;
    }

    public class SynchronizedQueue<T>
    {
        public SynchronizedQueue()
        {
            m_Queue = new Queue<T>();
            m_lock = new object();
        }

        public SynchronizedQueue(SynchronizedQueue<T> queue)
        {
            m_Queue = new Queue<T>(queue.m_Queue);
            m_lock = new object();
        }

        public void Enqueue(T type)
        {
            lock (m_lock)
            {
                m_Queue.Enqueue(type);
                m_cvar.Set();
            }
        }

        public bool TryDequeue(out T m_out)
        {
            lock (m_lock)
            {
                if (Count > 0)
                {
                    m_out = m_Queue.Dequeue();
                    return true;
                }
                m_out = default(T);
                return false;
            }
        }

        public T Wait()
        {
            lock(m_lock)
            {
                while (m_Queue.Count == 0)
                    m_cvar.WaitOne(100);

                return m_Queue.Dequeue();
            }
        }
        
        public int Count
        {
            get
            {
                return m_Queue.Count;
            }
        }

        public void Sort<TKey>(System.Func<T, TKey> func)
        {
            lock(m_lock)
                m_Queue.OrderByDescending<T, TKey>(func);
        }

        private Queue<T> m_Queue;
        private object m_lock;
        private EventWaitHandle m_cvar = new EventWaitHandle(false, EventResetMode.AutoReset);
    }

    static AutoResetEvent work_cvar = new AutoResetEvent(false);
    static SynchronizedQueue<IEnumerator> _MainThreadQueue = new SynchronizedQueue<IEnumerator>();
    static SynchronizedList<SynchronizedQueue<IEnumerator>> _ReadyLists = new SynchronizedList<SynchronizedQueue<IEnumerator>>();

    struct SleepingJob
    {
        public IEnumerator job;
        public JobYields.BaseJobYield yieldFunc;
    }

    static SynchronizedQueue<SleepingJob> _SleepingJobs = new SynchronizedQueue<SleepingJob>();
    static List<Thread> m_Workers;

    static int active_workers = 0;
    static bool running = true;

    static void RunJob(IEnumerator job, SynchronizedQueue<IEnumerator> queue)
    {
        try
        {
            while (job.MoveNext())
            {
                if (job.Current == null)
                {
                    break;
                }
                else if (job.Current == JobYields.SwitchToMain && queue != _MainThreadQueue)
                {
                    EnqueueJob(job, _MainThreadQueue);
                    break;
                }
                else if (job.Current == JobYields.SwitchToWorker && queue == _MainThreadQueue)
                {
                    EnqueueJobOnMostFree(job);
                    break;
                }
                else if (job.Current == JobYields.Yield)
                {
                    EnqueueJob(job, queue);
                    break;
                }
                else if (job.Current is JobYields.BaseJobYield)
                {
                    var yield = job.Current as JobYields.BaseJobYield;
                    if (yield.ShouldYield())
                    {
                        var obj = new SleepingJob();
                        obj.yieldFunc = yield;
                        obj.job = job;
                        _SleepingJobs.Enqueue(obj);
                        break;
                    }
                }
            }
        }
        catch(System.Exception ex)
        {
            Debug.LogError(ex);
            Debug.Break();
        }
    }

    public static void RunJobOnMainThread()
    {
        if (_SleepingJobs.Count > 0)
        {
            _SleepingJobs.Sort((a) => !a.yieldFunc.ShouldYield());
            
            SleepingJob sleep;
            while(_SleepingJobs.TryDequeue(out sleep) && !sleep.yieldFunc.ShouldYield())
                EnqueueJobOnMostFree(sleep.job);

            if (sleep.yieldFunc.ShouldYield())
                _SleepingJobs.Enqueue(sleep);
        }

        int jobs = 0;
        jobs += _MainThreadQueue.Count;
        for (int i = 0; i < _ReadyLists.Count; ++i)
            jobs += _ReadyLists[i].Count;

        IEnumerator job;
        if (_MainThreadQueue.TryDequeue(out job))
        {
            RunJob(job, _MainThreadQueue);
            return;
        }

        if ((job = FindWork(0)) != null)
        {
            RunJob(job, _MainThreadQueue);
        }
    }

    public static bool HasJobs()
    {
        if (active_workers > 0)
        {
            return true;
        }

        if (_MainThreadQueue.Count > 0)
        {
            return true;
        }

        for (int i = 0; i < _ReadyLists.Count; ++i)
        {
            if (_ReadyLists[i].Count > 0)
            {
                work_cvar.Set();
                return true;
            }
        }
        
        return false;
    }

    static IEnumerator FindWork(int index)
    {
        IEnumerator ret;
        if (_ReadyLists[index].TryDequeue(out ret))
            return ret;

        for (int i = 0; i < _ReadyLists.Count; ++i)
        {
            if (i == index)
                continue;

            if (_ReadyLists[index].TryDequeue(out ret))
                return ret;
        }
        return null;
    }

    static void WorkerFunc(object index)
    {
        int threadindex = (int)index;
        while (running)
        {
            IEnumerator job = null;
            while ((job = FindWork(threadindex)) == null && running)
            {
                work_cvar.WaitOne(100);
            }
            if (job != null)
            {
                Interlocked.Increment(ref active_workers);
                RunJob(job, _ReadyLists[threadindex]);
                Interlocked.Decrement(ref active_workers);
            }
        }
    }

    public static void InitializeWorkers()
    {
        running = true;
        m_Workers = new List<Thread>();
        for (int i = 0; i < System.Environment.ProcessorCount - 1; ++i)
        {
            _ReadyLists.Add(new SynchronizedQueue<IEnumerator>());

            var thr = new Thread(WorkerFunc);
            thr.Start(i);
            m_Workers.Add(thr);
        }
    }

    public static void Shutdown()
    {
        for (int i = 0; i < m_Workers.Count; ++i)
        {
            work_cvar.Set();
        }
        running = false;
        for (int i = 0; i < m_Workers.Count; ++i)
        {
            if (!m_Workers[i].Join(1000))
                m_Workers[i].Abort();
        }
    }

    public static void EnqueueJob(IEnumerator coroutine, SynchronizedQueue<IEnumerator> list)
    {
        list.Enqueue(coroutine);
        work_cvar.Set();
    }

    public static void EnqueueJobOnMostFree(IEnumerator coroutine)
    {
        SynchronizedQueue<IEnumerator> least;
        if (_ReadyLists.Count > 0)
            least = _ReadyLists[0];
        else
            least = _MainThreadQueue;

        for (int i = 0; i < _ReadyLists.Count; ++i)
        {
            if (_ReadyLists[i].Count == 0)
            {
                least = _ReadyLists[i];
                break;
            }

            if (_ReadyLists[i].Count < least.Count)
            {
                least = _ReadyLists[i];
            }
        }
        EnqueueJob(coroutine, least);
    }
}
