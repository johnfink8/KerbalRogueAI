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
        AllGraphTransferCalculator worker = null;
        string Target;
        bool Initialized = false;

        public AIOperationPlanetTransfer(AICore aicore, string Target)
            : base(aicore)
        {
            this.Target = Target;
            this.aicore = aicore;
        }

        void initialize()
        {
            UT = Planetarium.GetUniversalTime();
            CelestialBody targetBody = aicore.GetBody(Target);
            if (targetBody == null)
                throw new AbortFlightPlanException("Target body not found");
            FlightGlobals.fetch.SetVesselTarget(targetBody);
            orbit = vessel.orbit;
            Orbit destination = targetBody.orbit;
            double synodic_period = orbit.referenceBody.orbit.SynodicPeriod(destination);
            double minDepartureTime = UT;
            double maxDepartureTime = minDepartureTime + synodic_period * 1.5;
            double minTransferTime = 3600;
            double hohmann_transfer_time = OrbitUtil.GetTransferTime(orbit.referenceBody.orbit, destination);
            double maxTransferTime = hohmann_transfer_time * 1.5;
            worker = new AllGraphTransferCalculator(orbit, destination, minDepartureTime, maxDepartureTime, minTransferTime, maxTransferTime, 150, 200);
        }


        protected override bool CheckStep()
        {
            if (!Initialized)
            {
                Initialized = true;
                Debug.Log("Starting worker");
                initialize();
                return true;
            }
            if (worker != null)
            {
                if (!worker.Finished)
                {
                    aicore.ManeuverStatus = "Worker running " + worker.Progress.ToString();
                    return true;
                }
                else
                {
                    aicore.ManeuverStatus = "Worker finished";
                    worker.stop = true;
                }
            }
            return base.CheckStep();

        }

        public override void GenerateNode()
        {
            ManeuverParameters computedNode = TransferCalculator.OptimizeEjection(
                                worker.computed[worker.bestDate, worker.bestDuration],
                                orbit, worker.destinationOrbit,
                                worker.DateFromIndex(worker.bestDate) + worker.DurationFromIndex(worker.bestDuration),
                                Planetarium.GetUniversalTime());
            worker.stop = true;
            worker = null;
            Debug.Log("worker computed " + computedNode.dV.ToString() + " " + aicore.TimeTranslate(computedNode.UT - Planetarium.GetUniversalTime()));
            dV = computedNode.dV;
            this.UT = computedNode.UT;
            Threshhold = 5.0;
            ForceDone = true;
        }
    }
}
