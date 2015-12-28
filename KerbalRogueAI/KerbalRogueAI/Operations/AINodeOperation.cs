using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MuMech;

namespace KerbalRogueAI
{
    public abstract class AINodeOperation : AIOperation
    {
        public AINodeOperation(AICore aicore) : base(aicore) { }

        public double Threshhold = 0.2; // If we get a node less than this magnitude, then we're already done

        protected bool ForceDone = false; // Some operations need to be done once and quit.

        private bool CheckNode(Vector3d node)
        {
            return node.magnitude > Threshhold;
        }

        protected Orbit orbit;
        protected Vector3d dV;
        protected double UT;

        private bool CreateNode ()
        {
            if (!CheckNode(dV))
                return false;
            if (UT < orbit.EndUT && dV.magnitude > aicore.VesselDeltaV())
                throw new AbortFlightPlanException("Node exceeds available deltaV");
            vessel.PlaceManeuverNode(orbit, dV, UT);
            core.node.ExecuteOneNode(this);
            return true;
        }

        public abstract void GenerateNode();

        public override bool _operation()
        {
            throw new NotImplementedException();
        }

        public override bool Execute()
        {
            if (ForceDone)
                return true;
            if (CheckStep())
                return false;
            GenerateNode();
            if (!CreateNode())
                return true;  // true means done
            return false;  // false means not done
        }
    }
}
