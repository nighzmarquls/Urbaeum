using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UrbUtility
{
    public class UrbThrottle{
        protected int SkipCount = 0;
        protected int MaxSkips;
        const float MaxDelay =  0.0005f;
        const float MinTime =   0.0035047f;
        const float MaxTime = MaxDelay + MinTime;

        public UrbThrottle(int DefaultMaxSkips = 60)
        {
            MaxSkips = DefaultMaxSkips;
        }

        public IEnumerator PerformanceThrottle()
        {
            while (Time.deltaTime > MaxTime)
            {
                if (SkipCount > MaxSkips)
                {
                    yield break;
                }

                SkipCount++;

                yield return new WaitForSeconds(MaxDelay*(MaxSkips/SkipCount));
            }
            SkipCount = 0;
        }
    }
}
