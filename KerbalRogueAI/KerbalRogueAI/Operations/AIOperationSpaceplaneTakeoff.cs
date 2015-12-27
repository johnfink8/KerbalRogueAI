using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    class AIOperationSpaceplaneTakeoff : AIOperation
    {
        public AIOperationSpaceplaneTakeoff(AICore aicore) : base (aicore){}
        SpaceplaneSituation prevSituation = SpaceplaneSituation.Preflight;
        private SpaceplaneSituation getVesselSituation()
        {
            if (prevSituation == SpaceplaneSituation.InOrbit)
                return prevSituation;
            if (Staging.CurrentStage == Staging.StageCount)
            {
                //Debug.Log("RogueAI SSTOSituation PreFlight");
                return SpaceplaneSituation.Preflight;
            }
            if (vessel.Landed && vessel.heightFromTerrain < 5.0f)
            {
                //Debug.Log("RogueAI SSTOSituation Takeoff");
                return SpaceplaneSituation.Takeoff;
            }
            if (vessel.altitude < 10000 && vessel.mach < 1)
            {
                //Debug.Log("RogueAI SSTOSituation SubsonicAscent height " + vessel.altitude);
                return SpaceplaneSituation.SubsonicAscent;
            }
            if (vessel.mach >= 0.9f && vessel.mach < 2)
            {
                //Debug.Log("RogueAI SSTOSituation SonicTransition");
                return SpaceplaneSituation.SonicTransition;
            }
            if (vessel.mach >= 1.9 && vessel.orbit.ApA <= atmHeight + 5500)
            {
                if (vessel.orbit.ApA > atmHeight + 5000 && prevSituation == SpaceplaneSituation.CoastToSpace)
                    return SpaceplaneSituation.CoastToSpace;
                //Debug.Log("RogueAI SSTOSituation HypersonicAscent");
                return SpaceplaneSituation.HypersonicAscent;
            }
            if (vessel.orbit.ApA > atmHeight + 5000 && vessel.altitude < atmHeight)
            {
                return SpaceplaneSituation.CoastToSpace;
            }
            if (vessel.orbit.ApA > atmHeight && vessel.orbit.PeA < atmHeight)
                return SpaceplaneSituation.Circularize;
            return SpaceplaneSituation.InOrbit;

        }
        public override bool _operation()
        {
            if (aicore.OrbitCircular(vessel.orbit))
                return true;
            FlightCtrlState s = vessel.ctrlState;
            SpaceplaneSituation situation = getVesselSituation();
            //strSituation = situation.ToString();
            //strAp = vessel.orbit.ApA.ToString();
            switch (situation)
            {
                case SpaceplaneSituation.Preflight:
                    s.mainThrottle = 1;
                    Staging.ActivateNextStage();
                    break;
                case SpaceplaneSituation.Takeoff:
                    s.mainThrottle = 1;
                    aicore.holdFlightHeading(s, 0, 90);
                    break;
                case SpaceplaneSituation.SubsonicAscent:
                    if (vessel.geeForce < 1.2 && vessel.altitude > 500)
                        TimeWarp.SetRate(1, true);
                    else
                        TimeWarp.SetRate(0, true);
                    s.mainThrottle = 1;
                    aicore.holdFlightHeading(s, -20, 90);
                    //DeployLandingGears(false);
                    if (vessel.ActionGroups[KSPActionGroup.Gear])
                        vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
                    break;
                case SpaceplaneSituation.SonicTransition:
                    TimeWarp.SetRate(0, true);
                    if (Staging.CurrentStage > 1)
                        Staging.ActivateNextStage();
                    s.mainThrottle = 1;
                    aicore.holdFlightHeading(s, -5, 90);
                    break;
                case SpaceplaneSituation.HypersonicAscent:
                    TimeWarp.SetRate(0, true);
                    s.mainThrottle = 1;
                    aicore.holdFlightHeading(s, -20, 90);
                    break;
                case SpaceplaneSituation.CoastToSpace:
                    TimeWarp.SetRate(1, true);
                    s.mainThrottle = 0;
                    aicore.holdFlightHeading(s, 0, 90);
                    break;
                case SpaceplaneSituation.Circularize:
                    TimeWarp.SetRate(0, true);
                    return true;
                case SpaceplaneSituation.InOrbit:
                    TimeWarp.SetRate(0, true);
                    return true;
            }
            return false;
        }
    }
}
