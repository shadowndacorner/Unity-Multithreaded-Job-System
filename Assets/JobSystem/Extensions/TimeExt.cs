using UnityEngine;
namespace JobSystem
{
    public static class Time
    {
        public static void Update()
        {
            timeSinceLevelLoad = Time.timeSinceLevelLoad;
            deltaTime = Time.deltaTime;
            fixedDeltaTime = Time.fixedDeltaTime;
            realtimeSinceStartup = Time.realtimeSinceStartup;
            unscaledTime = Time.unscaledTime;
        }

        public static float timeSinceLevelLoad { get; internal set; }
        public static float deltaTime { get; internal set; }
        public static float fixedDeltaTime { get; internal set; }
        public static float realtimeSinceStartup { get; internal set; }
        public static float unscaledTime { get; internal set; }
    }
}