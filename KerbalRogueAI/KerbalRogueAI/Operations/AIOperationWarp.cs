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
