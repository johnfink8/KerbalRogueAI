using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    class AIOperationHohmann : AINodeOperation
    {
        public AIOperationHohmann(AICore aicore) : base(aicore) { }
        public string body = "Kerbin";
        public override void GenerateNode()
        {
            aicore.SetTarget(body);
            Operation transfer = new OperationGeneric();
            var computedNode = transfer.MakeNode(vessel.orbit, Planetarium.GetUniversalTime(), core.target);
            orbit = vessel.orbit;
            dV = computedNode.dV;
            UT = computedNode.UT;
            ForceDone = true; 
            return;
        }
    }
}
