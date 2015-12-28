using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    class AIOperationPlanetTransfer : AINodeOperation
    {
        Orbit o;
        AllGraphTransferCalculator worker = null;
        string Target;

        public AIOperationPlanetTransfer(AICore aicore, string Target)
            : base(aicore)
        {
            this.Target = Target;
        }

        void initialize()
        {
            CelestialBody targetBody = aicore.GetBody(Target);
            if (targetBody == null)
                throw new AbortFlightPlanException("Target body not found");
            FlightGlobals.fetch.SetVesselTarget(targetBody);
            o = vessel.orbit;
            Orbit destination = core.target.TargetOrbit;
            double synodic_period = o.referenceBody.orbit.SynodicPeriod(destination);
            double minDepartureTime = Planetarium.GetUniversalTime();
            double maxDepartureTime = minDepartureTime + synodic_period * 1.5;
            double minTransferTime = 3600;
            double hohmann_transfer_time = OrbitUtil.GetTransferTime(o.referenceBody.orbit, destination);
            double maxTransferTime = hohmann_transfer_time * 1.5;
            worker = new AllGraphTransferCalculator(o, destination, minDepartureTime, maxDepartureTime, minTransferTime, maxTransferTime, 300, 600);
        }


        protected override bool CheckStep()
        {
            if (worker == null)
            {
                Debug.Log("Starting worker");
                initialize();
                return true;
            }
            if (!worker.Finished)
            {
                aicore.ManeuverStatus = "Worker running " + worker.Progress.ToString();
                return true;
            }
            return base.CheckStep();

        }

        public override void GenerateNode()
        {
            ManeuverParameters computedNode = TransferCalculator.OptimizeEjection(
                                worker.computed[worker.bestDate, worker.bestDuration],
                                o, worker.destinationOrbit,
                                worker.DateFromIndex(worker.bestDate) + worker.DurationFromIndex(worker.bestDuration),
                                UT);
            dV = computedNode.dV;
            this.UT = computedNode.UT;
            Threshhold = 5.0;
        }
    }
}
