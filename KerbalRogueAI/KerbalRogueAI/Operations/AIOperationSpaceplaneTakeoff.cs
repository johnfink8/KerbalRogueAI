﻿using System;
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
        float DesiredThrottle = 1;
        double prevSpeed = 0;

        protected override bool CheckStep()
        {
            return false;
        }

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
            if (vessel.altitude < 10000 && prevSituation <= SpaceplaneSituation.SubsonicAscent)
            {
                //Debug.Log("RogueAI SSTOSituation SubsonicAscent height " + vessel.altitude);
                return SpaceplaneSituation.SubsonicAscent;
            }
            if (prevSituation <= SpaceplaneSituation.SonicTransition && vessel.mach < 2.1)
            {
                //Debug.Log("RogueAI SSTOSituation SonicTransition");
                return SpaceplaneSituation.SonicTransition;
            }
            if (prevSituation <= SpaceplaneSituation.CoastToSpace && vessel.orbit.ApA <= atmHeight + 5500 && vessel.altitude < atmHeight)
            {
                if (vessel.orbit.ApA > atmHeight + 5000 && prevSituation == SpaceplaneSituation.CoastToSpace)
                    return SpaceplaneSituation.CoastToSpace;
                //Debug.Log("RogueAI SSTOSituation HypersonicAscent");
                return SpaceplaneSituation.HypersonicAscent;
            }
            if (vessel.orbit.ApA > atmHeight + 5000 && vessel.altitude < atmHeight)
            {
                //Debug.Log("RogueAI SSTOSituation CoastToSpace " + vessel.altitude);
                return SpaceplaneSituation.CoastToSpace;
            }
            if (vessel.orbit.ApA > atmHeight && vessel.orbit.PeA < atmHeight)
            {
                Debug.Log("RogueAI SSTOSituation Circularize");
                return SpaceplaneSituation.Circularize;
            }
            Debug.Log("RogueAI SSTOSituation InOrbit");
            return SpaceplaneSituation.InOrbit;

        }

        double ThrottleDelay = 0;

        private void HoldMach(double speed, FlightCtrlState s)
        {
            double upper_limit = speed + speed * 0.05;
            double UT = Planetarium.GetUniversalTime();
            if (UT > ThrottleDelay)
            {
                if (vessel.mach > speed && vessel.mach > prevSpeed)
                    DesiredThrottle -= 0.01f;
                else if (vessel.mach < upper_limit && vessel.mach < prevSpeed)
                    DesiredThrottle += 0.01f;
                if (DesiredThrottle > 1)
                    DesiredThrottle = 1;
                else if (DesiredThrottle < 0)
                    DesiredThrottle = 0;
                ThrottleDelay = UT + 0.08;
            }
            s.mainThrottle = DesiredThrottle;
            prevSpeed = vessel.mach;
        }

        public override bool _operation()
        {
            if (aicore.OrbitCircular(vessel.orbit))
                return true;
            FlightCtrlState s = vessel.ctrlState;
            SpaceplaneSituation situation = getVesselSituation();
            prevSituation = situation;
            aicore.ManeuverStatus = situation.ToString() + " " + core.vesselState.limitedMaxThrustAccel.ToString();
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
                    aicore.holdFlightHeading(s, -5, 90);
                    break;
                case SpaceplaneSituation.SubsonicAscent:
                    /*if (vessel.geeForce < 1.2 && vessel.altitude > 500)
                        TimeWarp.SetRate(1, true);
                    else
                        TimeWarp.SetRate(0, true);*/
                    aicore.holdFlightHeading(s, -20, 90);
                    HoldMach(0.9, s);
                    if (vessel.altitude > 100 && vessel.ActionGroups[KSPActionGroup.Gear])
                        vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
                    break;
                case SpaceplaneSituation.SonicTransition:
                    s.mainThrottle = 1;
                    DesiredThrottle = 1;
                    aicore.holdFlightHeading(s, -5, 90);
                    break;
                case SpaceplaneSituation.HypersonicAscent:
                    if (core.vesselState.limitedMaxThrustAccel < 0.1 && Staging.CurrentStage > 0)
                        Staging.ActivateNextStage();
                    if (vessel.altitude < 30000)
                        HoldMach(4.0, s);
                    else
                        s.mainThrottle = 1;
                    aicore.holdFlightHeading(s, -20, 90);
                    break;
                case SpaceplaneSituation.CoastToSpace:
                    //TimeWarp.SetRate(1, true);
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
