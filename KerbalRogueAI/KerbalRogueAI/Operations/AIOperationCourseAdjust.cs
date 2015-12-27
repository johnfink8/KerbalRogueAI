using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    class AIOperationCourseAdjust : AINodeOperation
    {
        public AIOperationCourseAdjust(AICore aicore) : base(aicore) { }
        public string body = null;
        public double distance = 15000;
        public override void GenerateNode()
        {
            if (body != null)
                aicore.SetTarget(body);
            vessel.RemoveAllManeuverNodes();
            OperationCourseCorrection transfer = new OperationCourseCorrection();
            transfer.courseCorrectFinalPeA.val = distance;
            var computedNode = transfer.MakeNode(vessel.orbit, Planetarium.GetUniversalTime(), core.target);
            orbit = vessel.orbit;
            dV = computedNode.dV;
            UT = computedNode.UT;
            return;
        }
    }
}
