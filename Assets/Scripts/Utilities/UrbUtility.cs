using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace UrbUtility
{
    public struct UrbThrottle
    {
        int SkipCount;

        readonly int MaxSkips;

        //For some reason, Structs do not call constructors if params are missing
        const int HardMaxSkips = 60;
        const float MilliSeconds = (1.0f / 1000.0f);
        //The maximum time into a frame we keep calling
        const float MaxDelay = 10.0f * MilliSeconds; 
        const float MinTime = 2.0f * MilliSeconds;
        const float MaxWaitTime = 2 * MaxDelay;

        public UrbThrottle(int maxSkips = 60)
        {
            SkipCount = 0;
            MaxSkips = maxSkips;
        }

        public IEnumerator PerformanceThrottle()
        {
            yield return new WaitForSeconds(MinTime);
            //Don't add MORE work on to the overloaded frame times
            //Also, if SkipCount == 0; we should wait at least once so we can 
            //ensure other coroutines get their chances
            if (Time.deltaTime > MaxDelay || SkipCount == 0)
            {
                ++SkipCount;
                yield return new WaitForSeconds((MaxWaitTime * (MaxSkips / SkipCount)) + MinTime);
            }
            
            if (SkipCount > MaxSkips || SkipCount > HardMaxSkips)
            {
                SkipCount = 0;
                yield break;
            }
        }
    }
}
