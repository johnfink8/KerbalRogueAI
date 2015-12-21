using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using KSP.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using File = KSP.IO.File;

namespace KerbalRogueAI
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AICore : MonoBehaviour
    {
        private static Vessel vessel { get { return FlightGlobals.ActiveVessel; } }

        private static MuMech.MechJebCore core { get { return VesselExtensions.GetModules<MuMech.MechJebCore>(vessel)[0]; } }

        private static double atmHeight { get { return vessel.mainBody.atmosphereDepth; } }

        void CheckVessel()
        {
            
            if (vessel != null)
            {
                vessel.OnFlyByWire -= flyByWire; //just a safety precaution to avoid duplicates
                vessel.OnFlyByWire += flyByWire;
            }

        }
        SSTOSituation prevSituation = SSTOSituation.Preflight;
        string strSituation="";
        string strAp="";
        string strDebug = "";
        TimeWarp timeWarp;


        public enum SSTOSituation { Preflight, Takeoff, SubsonicAscent, SonicTransition, HypersonicAscent, CoastToSpace, Circularize,InOrbit }
        void Start()  //Called when vessel is placed on the launchpad
        {
            timeWarp = (TimeWarp)FindObjectOfType(typeof(TimeWarp));
            if ((windowPos.x == 0) && (windowPos.y == 0))//windowPos is used to position the GUI window, lets set it in the center of the screen
            {
                windowPos = new Rect(Screen.width - 250, Screen.height / 2, 10, 10);
            } 
            RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));//start the GUI
            //at the beginning of the flight, register your fly-by-wire control function that will be called repeatedly during flight:
            //vessel.OnFlyByWire += new FlightInputCallback(flyByWire);

            //...
        }

        private SSTOSituation getVesselSituation()
        {
            if (prevSituation == SSTOSituation.InOrbit)
                return prevSituation;
            if (Staging.CurrentStage == Staging.StageCount)
            {
                //Debug.Log("RogueAI SSTOSituation PreFlight");
                return SSTOSituation.Preflight;
            }
            if (vessel.Landed && vessel.heightFromTerrain < 5.0f)
            {
                //Debug.Log("RogueAI SSTOSituation Takeoff");
                return SSTOSituation.Takeoff;
            }
            if (vessel.altitude < 10000 && vessel.mach < 1)
            {
                //Debug.Log("RogueAI SSTOSituation SubsonicAscent height " + vessel.altitude);
                return SSTOSituation.SubsonicAscent;
            }
            if (vessel.mach >= 0.9f && vessel.mach < 2)
            {
                //Debug.Log("RogueAI SSTOSituation SonicTransition");
                return SSTOSituation.SonicTransition;
            }
            if (vessel.mach >= 1.9 && vessel.orbit.ApA <= atmHeight + 5500)
            {
                if (vessel.orbit.ApA > atmHeight+5000 && prevSituation == SSTOSituation.CoastToSpace)
                    return SSTOSituation.CoastToSpace;
                //Debug.Log("RogueAI SSTOSituation HypersonicAscent");
                return SSTOSituation.HypersonicAscent;
            }
            if (vessel.orbit.ApA > atmHeight + 5000 && vessel.altitude < atmHeight)
            {
                return SSTOSituation.CoastToSpace;
            }
            if (vessel.orbit.ApA > atmHeight && vessel.orbit.PeA < atmHeight)
                return SSTOSituation.Circularize;
            return SSTOSituation.InOrbit;

        }

        public bool OrbitApproaches(Orbit orbit, CelestialBody body)
        {
            double UT = Planetarium.GetUniversalTime();
            return !(orbit.NextClosestApproachTime(body.orbit, UT) < UT + 1 ||
                    orbit.NextClosestApproachDistance(body.orbit, UT) > body.orbit.semiMajorAxis * 0.2);
        }

        public double OrbitApproachPE(Orbit orbit, CelestialBody body)
        {
            if (orbit.nextPatch != null && orbit.nextPatch.referenceBody != null && orbit.nextPatch.referenceBody == body)
                return orbit.nextPatch.PeA;
            return orbit.NextClosestApproachDistance(body.orbit, Planetarium.GetUniversalTime());
        }

        public bool OrbitCircular(Orbit orbit)
        {
            return (orbit.ApA - orbit.PeA) / orbit.PeA < 0.2;
        }

        private void holdFlightHeading(FlightCtrlState s, float elevation, float heading)
        {

            core.attitude.attitudeTo(Quaternion.Euler(elevation,heading,0), AttitudeReference.SURFACE_NORTH, this);
        }

        private void holdPrograde()
        {
            core.attitude.attitudeTo(Quaternion.Euler(0, 0, 0), AttitudeReference.ORBIT, this);
        }

        private void SetTarget(string targetName)
        {
            foreach (var body in FlightGlobals.Bodies)
            {
                if (body.name == targetName)
                {
                    FlightGlobals.fetch.SetVesselTarget(body);
                    break;
                }
            }
        }

        private void flyByWire(FlightCtrlState s)
        {
            SSTOSituation situation = getVesselSituation();
            strSituation = situation.ToString();
            strAp = vessel.orbit.ApA.ToString();
            switch (situation)
            {
                case SSTOSituation.Preflight:
                    s.mainThrottle = 1;
                    Staging.ActivateNextStage();
                    break;
                case SSTOSituation.Takeoff:
                    s.mainThrottle = 1;
                    holdFlightHeading(s,0,90);
                    strDebug = "Zoom " + FlightCamera.fetch.zoomScaleFactor;
                    break;
                case SSTOSituation.SubsonicAscent:
                    if (vessel.geeForce < 1.2 && vessel.altitude > 500)
                        TimeWarp.SetRate(1,true);
                    else
                        TimeWarp.SetRate(0,true);
                    s.mainThrottle = 1;
                    holdFlightHeading(s, -20, 90);
                    //DeployLandingGears(false);
                    if (vessel.ActionGroups[KSPActionGroup.Gear])
                        vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
                    break;
                case SSTOSituation.SonicTransition:
                    TimeWarp.SetRate(0, true);
                    if (Staging.CurrentStage > 1)
                        Staging.ActivateNextStage();
                    s.mainThrottle = 1;
                    holdFlightHeading(s, -5, 90);
                    break;
                case SSTOSituation.HypersonicAscent:
                    TimeWarp.SetRate(0, true);
                    s.mainThrottle = 1;
                    holdFlightHeading(s, -20, 90);
                    break;
                case SSTOSituation.CoastToSpace:
                    TimeWarp.SetRate(1, true);
                    s.mainThrottle = 0;
                    holdFlightHeading(s, 0, 90);
                    break;
                case SSTOSituation.Circularize:
                    if (!vessel.patchedConicSolver.maneuverNodes.Any())
                    {
                        TimeWarp.SetRate(0, true);
                        s.mainThrottle = 0;
                        Operation circularize = new OperationCircularize();
                        var computedNode = circularize.MakeNode(vessel.orbit, Planetarium.GetUniversalTime(), core.target);
                        vessel.PlaceManeuverNode(vessel.orbit, computedNode.dV, computedNode.UT);
                        core.node.ExecuteOneNode(this);
                    }
                    break;
                case SSTOSituation.InOrbit:
                    CelestialBody minmus=null;
                    foreach (var body in FlightGlobals.Bodies)
                    {
                        if (body.name == "Minmus")
                        {
                            minmus=body;
                            break;
                        }
                    }
                    if (vessel.patchedConicSolver.maneuverNodes.Any())
                    {
                        if (core.node.burnTriggered)
                        {
                            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
                            double burnTime = node.GetBurnVector(node.patch).magnitude / core.vesselState.limitedMaxThrustAccel;
                            if (burnTime > 25)
                            {
                                if (TimeWarp.CurrentRateIndex < 2)
                                    core.warp.IncreasePhysicsWarp();
                            }
                            else if (TimeWarp.CurrentRateIndex>0)
                            {
                                TimeWarp.SetRate(0, true);
                            }
                        }
                    }
                    else
                    {
                        strDebug = "body " + vessel.mainBody.name + " approach " + OrbitApproaches(vessel.orbit, minmus);
                        if (vessel.mainBody.name == "Kerbin" && OrbitCircular(vessel.orbit))
                        {
                            strDebug = "Transfer to Minmus";
                            SetTarget("Minmus");
                            Operation transfer = new OperationGeneric();
                            var computedNode = transfer.MakeNode(vessel.orbit, Planetarium.GetUniversalTime(), core.target);
                            vessel.PlaceManeuverNode(vessel.orbit, computedNode.dV, computedNode.UT);
                            core.node.ExecuteOneNode(this);
                        }
                        else if (vessel.mainBody.name=="Kerbin" && OrbitApproaches(vessel.orbit,minmus) && OrbitApproachPE(vessel.orbit,minmus) > 18000)                         
                        {
                            strDebug = "Course adjustment for Minmus intercept, PE " + OrbitApproachPE(vessel.orbit, minmus).ToString();
                            SetTarget("Minmus");
                            OperationCourseCorrection transfer = new OperationCourseCorrection();
                            transfer.courseCorrectFinalPeA.val = 15000;
                            var computedNode = transfer.MakeNode(vessel.orbit, Planetarium.GetUniversalTime(), core.target);
                            vessel.PlaceManeuverNode(vessel.orbit, computedNode.dV, computedNode.UT);
                            core.node.ExecuteOneNode(this);
                        }
                        else if (vessel.mainBody.name=="Kerbin" && OrbitApproaches(vessel.orbit, minmus))
                        {
                            strDebug = "Warp to Minmus";
                            double UT = Planetarium.GetUniversalTime() + vessel.orbit.timeToTransition1;
                            double desiredRate = 1.0 * (UT - (core.vesselState.time + Time.fixedDeltaTime * (float)TimeWarp.CurrentRateIndex));
                            desiredRate = MuUtils.Clamp(desiredRate, 1, 100000);
                            strDebug += " rate " + desiredRate + " UT " + UT + " time "+core.vesselState.time + " dt "+Time.fixedDeltaTime;
                            core.warp.WarpRegularAtRate((float)desiredRate);
                        }
                        else if (vessel.mainBody.name=="Minmus")
                        {
                            TimeWarp.SetRate(0,true);
                            if (vessel.orbit.PeA > 18000 || vessel.orbit.PeA < 15000)
                            {
                                strDebug = "Change periapsis";
                                double UT = Planetarium.GetUniversalTime();
                                var computedNode = new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangePeriapsis(vessel.orbit, UT, 16000 + vessel.mainBody.Radius), UT);
                                vessel.PlaceManeuverNode(vessel.orbit, computedNode.dV, computedNode.UT);
                                core.node.ExecuteOneNode(this);
                            }
                            else
                            {

                                strDebug = "Circularize at Minmus";
                                Operation circularize = new OperationCircularize();
                                double UT = Planetarium.GetUniversalTime() + vessel.orbit.timeToPe;
                                var computedNode = new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToCircularize(vessel.orbit,UT), UT);
                                vessel.PlaceManeuverNode(vessel.orbit, computedNode.dV, computedNode.UT);
                                core.node.ExecuteOneNode(this);
                            }
                        }
                        else // Not circular, not going to Minmus
                        {
                            OperationCircularize circularize = new OperationCircularize();
                            double UT = Planetarium.GetUniversalTime();
                            if (vessel.orbit.PeA > vessel.mainBody.atmosphereDepth)
                                UT += vessel.orbit.timeToPe;
                            else
                                UT += vessel.orbit.timeToAp;
                            var computedNode = circularize.MakeNode(vessel.orbit.nextPatch, UT, core.target);
                            vessel.PlaceManeuverNode(vessel.orbit, computedNode.dV, computedNode.UT);
                            core.node.ExecuteOneNode(this);
                        }
                    }
                    break;
            }
            prevSituation = situation;

        }
        /*
         * Called after the scene is loaded.
         */
        void Awake()
        {
        }
        /*
 * Called every frame
 */
        void Update()
        {
            CheckVessel();
            if ((Time.time - lastUpdate) > logInterval)
            {
                lastUpdate = Time.time;
            }
        }

        /*
         * Called at a fixed time interval determined by the physics time step.
         */
        private float lastUpdate = 0.0f;
        private float lastFixedUpdate = 0.0f;
        private float logInterval = 5.0f;
        void FixedUpdate()
        {
            if (TimeWarp.CurrentRateIndex > 0 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
            {
                // The flyByWire doesn't get called during time warps
                flyByWire(vessel.ctrlState);
            }
            if ((Time.time - lastFixedUpdate) > logInterval)
            {
                lastFixedUpdate = Time.time;
            }
        }

        /*
         * Called when the game is leaving the scene (or exiting). Perform any clean up work here.
         */
        void OnDestroy()
        {
        }

        protected Rect windowPos;

        private void WindowGUI(int windowID)
        {
            GUIStyle mySty = new GUIStyle(GUI.skin.button);
            mySty.normal.textColor = mySty.focused.textColor = Color.white;
            mySty.hover.textColor = mySty.active.textColor = Color.yellow;
            mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
            mySty.padding = new RectOffset(8, 8, 8, 8);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Situation", GUILayout.ExpandWidth(true));
            GUILayout.Label(strSituation, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Apoapsis", GUILayout.ExpandWidth(true));
            GUILayout.Label(strAp, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Landed", GUILayout.ExpandWidth(true));
            GUILayout.Label(vessel.landedAt, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            var stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);
            for (int i = stats.atmoStats.Length - 1; i >= 0; i--)
            {
                var s = stats.atmoStats[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label("Stage", GUILayout.ExpandWidth(true));
                GUILayout.Label(i.ToString(), GUILayout.ExpandWidth(true));
                GUILayout.Label(s.deltaTime.ToString(), GUILayout.ExpandWidth(true));
                GUILayout.Label(s.deltaV.ToString(), GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("NextPatch", GUILayout.ExpandWidth(true));
            string strPatch;
            if (vessel.orbit.nextPatch == null)
                strPatch = "nextPatchnull";
            else if (vessel.orbit.nextPatch.referenceBody == null)
                strPatch = "referenceNull";
            else
                strPatch = vessel.orbit.nextPatch.referenceBody.name;
            GUILayout.Label(strPatch, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MainBody", GUILayout.ExpandWidth(true));
            GUILayout.Label(vessel.mainBody.name, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(strDebug, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();


            //DragWindow makes the window draggable. The Rect specifies which part of the window it can by dragged by, and is 
            //clipped to the actual boundary of the window. You can also pass no argument at all and then the window can by
            //dragged by any part of it. Make sure the DragWindow command is AFTER all your other GUI input stuff, or else
            //it may "cover up" your controls and make them stop responding to the mouse.
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

        }
        private void drawGUI()
        {
            GUI.skin = HighLogic.Skin;
            windowPos = GUILayout.Window(1, windowPos, WindowGUI, "Set Direction", GUILayout.MinWidth(200));
        }
    }
}
