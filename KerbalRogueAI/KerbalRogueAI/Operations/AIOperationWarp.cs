using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    public class AIOperationWarp : AIOperation
    {
        public AIOperationWarp(AICore aicore) : base(aicore) { }
        public double WarpDestination = 0;
        public string strTo = "Transition";
        protected override bool CheckStep()
        {
            return false;  // This is a special case, we always need to call the operation code
        }

        public string TimeTranslate(double dseconds)
        {
            uint seconds = (uint)dseconds;
            return string.Format("{0:0} d, {1:00} h, {2:00}m, {3:00}s",seconds/86400,(seconds/3600)%24,(seconds/60)%60,seconds%60);
        }

        public override bool _operation()
        {
            double UT = Planetarium.GetUniversalTime();
            if (WarpDestination != 0 && UT >= WarpDestination)
            {
                WarpDestination = 0;
                return true;
            }
            if (WarpDestination == 0)
            {
                if (strTo == "Transition")
                {
                    if (vessel.orbit.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER)
                        WarpDestination = vessel.orbit.EndUT;
                    else
                        throw new AbortFlightPlanException("Encounter not found");
                }
            }
            if (WarpDestination == 0)
                return true;
            double desiredRate = 1.0 * (WarpDestination - (core.vesselState.time + Time.fixedDeltaTime * (float)TimeWarp.CurrentRateIndex));
            desiredRate = MuUtils.Clamp(desiredRate, 1, 100000);
            core.warp.WarpRegularAtRate((float)desiredRate);
            return false;
        }

    }
}
