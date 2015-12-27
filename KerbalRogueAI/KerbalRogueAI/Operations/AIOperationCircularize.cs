using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    class AIOperationCircularize : AINodeOperation
    {
        public AIOperationCircularize(AICore aicore) : base(aicore) { }
        public string destination = "AP";
        public override void GenerateNode()
        {
            Operation circularize = new OperationCircularize();
            double UT = Planetarium.GetUniversalTime();
            if (destination == "PE")
                UT += vessel.orbit.timeToPe;
            else if (destination == "AP")
                UT += vessel.orbit.timeToAp;
            var computedNode = new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToCircularize(vessel.orbit, UT), UT);
            orbit = vessel.orbit;
            dV = computedNode.dV;
            this.UT = computedNode.UT;
            Threshhold = 5.0;
            return;
        }
    }
}
