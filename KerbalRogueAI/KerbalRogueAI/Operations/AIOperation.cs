using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;


namespace KerbalRogueAI
{
    public abstract class AIOperation
    {
        public AICore aicore = null;

        public static Vessel vessel { get { return FlightGlobals.ActiveVessel; } }

        public static MuMech.MechJebCore core { get { return VesselExtensions.GetModules<MuMech.MechJebCore>(vessel)[0]; } }

        public static double atmHeight { get { return vessel.mainBody.atmosphereDepth; } }

        public AIOperation(AICore aicore)
        {
            this.aicore = aicore;
        }

        protected virtual bool CheckStep()
        {
            if (!vessel.patchedConicSolver.maneuverNodes.Any())
                return false;
            if (!core.node.users.Any())
                core.node.ExecuteOneNode(this);
            if (core.node.burnTriggered)
            {
                ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
                double burnTime = node.GetBurnVector(node.patch).magnitude / core.vesselState.limitedMaxThrustAccel;
                if (burnTime > 25)
                {
                    if (TimeWarp.CurrentRateIndex < 2)
                        core.warp.IncreasePhysicsWarp();
                }
                else if (burnTime > 10)
                {
                    if (TimeWarp.CurrentRateIndex < 1)
                        core.warp.IncreasePhysicsWarp();
                    if (TimeWarp.CurrentRateIndex > 1)
                        core.warp.DecreasePhysicsWarp();
                }
                else if (TimeWarp.CurrentRateIndex > 0)
                {
                    TimeWarp.SetRate(0, true);
                }
            }
            return true;
        }

        private bool CheckVessel()
        {
            if (aicore.VesselDeltaV() < 1)
            {
                Debug.LogError("DeltaV Not Available");
                return false;
            }
            return true;
        }

        public abstract bool _operation();

        public virtual bool Execute()
        {
            if (!CheckVessel())
                throw new AbortFlightPlanException("Vessel state bad");
            if (CheckStep())
                return false;
            return _operation();
        }
    }
}
