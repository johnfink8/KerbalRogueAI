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
            var computedNode = new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangePeriapsis(vessel.orbit, UT, 16000 + vessel.mainBody.Radius), UT);
            orbit = vessel.orbit;
            dV = computedNode.dV;
            this.UT = computedNode.UT;
            return;
        }
    }
}
