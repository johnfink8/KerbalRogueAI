using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    class AIOperationPE : AINodeOperation
    {
        public AIOperationPE(AICore aicore) : base(aicore) { }
        public double distance;
        public override void GenerateNode()
        {
            double UT = Planetarium.GetUniversalTime();
            orbit = vessel.orbit;
            if (orbit.ApA > 0 && distance > orbit.ApA)
                throw new AbortFlightPlanException("Can't make PE more than AP");
            if (orbit.patchEndTransition == Orbit.PatchTransitionType.FINAL && aicore.OrbitCircular(orbit))
                UT += orbit.timeToAp;
            var computedNode = new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangePeriapsis(vessel.orbit, UT, distance + vessel.mainBody.Radius), UT);
            orbit = vessel.orbit;
            dV = computedNode.dV;
            this.UT = computedNode.UT;
            return;
        }
    }
}
