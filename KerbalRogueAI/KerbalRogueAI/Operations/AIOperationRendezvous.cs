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

        public T RandomChoice<T>(IEnumerable<T> source, Func<T, bool> filter = null)
        {
            System.Random rnd = new System.Random();
            T result = default(T);
            int cnt = 0;
            foreach (T item in source)
            {
                if (item == null)
                    continue;
                if (filter != null && !filter(item))
                    continue;
                cnt++;
                if (rnd.Next(cnt) == 0)
                {
                    result = item;
                }
            }
            return result;
        }

        public bool VesselFilterPlaneDeltaV(Vessel v)
        {
            if (v == null || v.vesselType != VesselType.Ship)
                return false;
            double UT = Planetarium.GetUniversalTime();
            Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(vessel.orbit, v.orbit, UT, out UT);
            return dV.magnitude < 0.5 * aicore.VesselDeltaV();
        }

        public void GetRandomShipTarget()
        {
            Vessel newtarget = null;
            newtarget = RandomChoice(FlightGlobals.fetch.vessels, VesselFilterPlaneDeltaV);
            FlightGlobals.fetch.SetVesselTarget(newtarget);
        }

        public override bool _operation()
        {
            if (core == null)
                return false;
            var autopilot = core.GetComputerModule<MechJebModuleRendezvousAutopilot>();
            if (autopilot == null)
                return false;
            if (!aicore.CheckManeuverNodes())
                throw new AbortFlightPlanException("Insufficient deltaV");
            if (!activated)
            {
                activated = true;
                /*if (aicore.FlightPlanTarget != null)
                    Target = aicore.FlightPlanTarget;
                if (Target == null)
                    GetRandomShipTarget();
                else
                    FlightGlobals.fetch.SetVesselTarget(Target);*/
                if (core.target.Target == null)
                    throw new AbortFlightPlanException("Unable to find a target");
                aicore.FlightPlanTarget = core.target.Target;
                autopilot.desiredDistance.val = distance;
                autopilot.users.Add(this);
            }
            core.node.autowarp = true;
            autopilot.Drive(vessel.ctrlState);
            aicore.ManeuverStatus = autopilot.status;
            return !autopilot.users.Any();
        }
    }
}
