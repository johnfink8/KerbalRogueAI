using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;

namespace KerbalRogueAI
{
    class AIOperationAP : AINodeOperation
    {
        public AIOperationAP(AICore aicore) : base(aicore) { }
        public double distance;

        public override void GenerateNode()
        {
            double UT = Planetarium.GetUniversalTime();
            orbit = vessel.orbit;
            if (distance < orbit.PeA)
                throw new AbortFlightPlanException("Can't make AP less than PE");
            if (!aicore.OrbitCircular(orbit))
                UT += orbit.timeToPe;
            var computedNode = new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, UT, distance + vessel.mainBody.Radius), UT);
            dV = computedNode.dV;
            this.UT = computedNode.UT;
            return;
        }

    }
}
