using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    class AIOperationDock : AIOperation
    {
        public AIOperationDock(AICore aicore) : base(aicore) { }

        protected override bool CheckStep()
        {
            return false;
        }

        public bool FromGrabber = false;
        public bool FromDockingPort = false;
        private bool activated = false;
        public ITargetable Target = null;

        private bool GetTargetDockingPort()
        {
            Vessel targetvessel = vessel.targetObject.GetVessel();
            foreach (Part part in targetvessel.parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    if (module is ModuleDockingNode)
                    {
                        FlightGlobals.fetch.SetVesselTarget(module as ModuleDockingNode);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool SetDockControl()
        {
            foreach (Part part in vessel.parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var node = module as ModuleDockingNode;
                    if (node != null)
                    {
                        vessel.SetReferenceTransform(part);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool SetGrabControl()
        {
            foreach (Part part in vessel.parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var node = module as ModuleGrappleNode;
                    if (node != null)
                    {
                        node.enabled = true;
                        vessel.SetReferenceTransform(part);
                        return true;
                    }
                }
            }
            return false;
        }


        public override bool _operation()
        {
            if (core == null)
                return false;
            if (core.target.Distance > 100)
                throw new AbortFlightPlanException("Target too far");
            if (FromDockingPort)
            {
                if (!GetTargetDockingPort())
                    throw new AbortFlightPlanException("Target does not have docking port");
                if (!SetDockControl())
                    throw new AbortFlightPlanException("Vessel does not have docking port");
            }
            else if (FromGrabber)
            {
                if (!SetGrabControl())
                    throw new AbortFlightPlanException("Vessel does not have grabber");
            }
            var autopilot = core.GetComputerModule<MechJebModuleDockingAutopilot>();
            if (autopilot == null)
                return false;
            if (!activated)
            {
                activated = true;
                if (Target == null)
                    Target = vessel.targetObject;
                FlightGlobals.fetch.SetVesselTarget(Target);
                autopilot.users.Add(this);
            }
            core.node.autowarp = true;
            autopilot.Drive(vessel.ctrlState);
            return !autopilot.users.Any();
        }
    }
}
