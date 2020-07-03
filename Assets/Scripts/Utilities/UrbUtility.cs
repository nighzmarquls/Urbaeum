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
        const float MaxDelay =  2.0f * MilliSeconds;
        const float MinTime =   0.0035047f;
        const float MaxWaitTime = MaxDelay + MinTime;

        public UrbThrottle(int maxSkips = 60)
        {
            SkipCount = 0;
            MaxSkips = maxSkips;
        }
        
        public IEnumerator PerformanceThrottle()
        {
            if (SkipCount == 0)
            {
                ++SkipCount;
                yield return new WaitForFixedUpdate();
            }
            
            if (Time.deltaTime < MinTime || SkipCount > MaxSkips || SkipCount > HardMaxSkips)
            {
                SkipCount = 0;
                yield break;
            }
            
            if (Time.deltaTime > MaxWaitTime)
            {
                ++SkipCount;
                //We use the 
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
