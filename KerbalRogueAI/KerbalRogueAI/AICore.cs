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


        public enum SSTOSituation { Preflight, Takeoff, SubsonicAscent, SonicTransition, HypersonicAscent, CoastToSpace, Circularize,InOrbit }
        void Start()  //Called when vessel is placed on the launchpad
        {
            if ((windowPos.x == 0) && (windowPos.y == 0))//windowPos is used to position the GUI window, lets set it in the center of the screen
            {
                windowPos = new Rect(Screen.width / 2, Screen.height / 2, 10, 10);
            } 
            RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));//start the GUI
            //at the beginning of the flight, register your fly-by-wire control function that will be called repeatedly during flight:
            //vessel.OnFlyByWire += new FlightInputCallback(flyByWire);

            //...
        }

        private SSTOSituation getVesselSituation()
        {
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

        private void holdFlightHeading(FlightCtrlState s, float elevation, float heading)
        {

            core.attitude.attitudeTo(Quaternion.Euler(elevation,heading,0), AttitudeReference.SURFACE_NORTH, this);
        }

        private void holdPrograde()
        {
            core.attitude.attitudeTo(Quaternion.Euler(0, 0, 0), AttitudeReference.ORBIT, this);
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
                    break;
                case SSTOSituation.SubsonicAscent:
                    s.mainThrottle = 1;
                    holdFlightHeading(s, -20, 90);
                    //DeployLandingGears(false);
                    if (vessel.ActionGroups[KSPActionGroup.Gear])
                        vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
                    break;
                case SSTOSituation.SonicTransition:
                    s.mainThrottle = 1;
                    holdFlightHeading(s, -5, 90);
                    break;
                case SSTOSituation.HypersonicAscent:
                    s.mainThrottle = 1;
                    holdFlightHeading(s, -20, 90);
                    break;
                case SSTOSituation.CoastToSpace:
                    s.mainThrottle = 0;
                    holdFlightHeading(s, 0, 90);
                    break;
                case SSTOSituation.Circularize:
                    if (!vessel.patchedConicSolver.maneuverNodes.Any())
                    {
                        s.mainThrottle = 0;
                        Operation circularize = new OperationCircularize();
                        var computedNode = circularize.MakeNode(vessel.orbit, Planetarium.GetUniversalTime(), core.target);
                        vessel.PlaceManeuverNode(vessel.orbit, computedNode.dV, computedNode.UT);
                        core.node.ExecuteOneNode(this);
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
            Debug.Log("TAC Examples-SimplePartlessPlugin [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: Awake: " + this.name);
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
                Debug.Log("TAC Examples-SimplePartlessPlugin [" + this.GetInstanceID().ToString("X")
                    + "][" + Time.time.ToString("0.0000") + "]: Update");
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
            if ((Time.time - lastFixedUpdate) > logInterval)
            {
                lastFixedUpdate = Time.time;
                Debug.Log("TAC Examples-SimplePartlessPlugin [" + this.GetInstanceID().ToString("X")
                    + "][" + Time.time.ToString("0.0000") + "]: FixedUpdate");
            }
        }

        /*
         * Called when the game is leaving the scene (or exiting). Perform any clean up work here.
         */
        void OnDestroy()
        {
            Debug.Log("TAC Examples-SimplePartlessPlugin [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: OnDestroy");
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
