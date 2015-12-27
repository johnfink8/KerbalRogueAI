using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    class AIOperationRendezvous : AIOperation
    {
        public AIOperationRendezvous(AICore aicore) : base(aicore) { }

        public ITargetable Target = null;

        private bool activated = false;

        //private ComputerModule autopilot = null;

        protected override bool CheckStep()
        {
            return false;
        }

        public double distance = 50;

        public override bool _operation()
        {
            if (core == null)
                return false;
            var autopilot = core.GetComputerModule<MechJebModuleRendezvousAutopilot>();
            if (autopilot == null)
                return false;
            if (!activated)
            {
                activated = true;
                if (Target == null)
                    aicore.GetRandomShipTarget();
                else
                    FlightGlobals.fetch.SetVesselTarget(Target);
                autopilot.desiredDistance.val = distance;
                autopilot.users.Add(this);
            }
            core.node.autowarp = true;
            autopilot.Drive(vessel.ctrlState);
            return !autopilot.users.Any();
        }
    }
}
