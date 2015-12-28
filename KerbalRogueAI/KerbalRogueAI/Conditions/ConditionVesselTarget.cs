using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalRogueAI
{
    class ConditionVesselTarget : AICondition
    {
        public ConditionVesselTarget(AICore aicore) : base(aicore) { }

        public override bool _condition()
        {
            if (core == null || core.target == null || core.target.Target == null)
                return false;
            var target = core.target.Target as Vessel;
            if (target == null)
                return false;
            if (!(target.vesselType == VesselType.Ship || target.vesselType == VesselType.Lander || target.vesselType == VesselType.Station))
                return false;
            return target.situation == Vessel.Situations.ORBITING;
        }
    }
}
