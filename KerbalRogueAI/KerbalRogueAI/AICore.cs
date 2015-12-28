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
#if DEBUG
    public class Debug
    {
        public static void Log(string s) { Console.WriteLine(s); }
        public static void LogWarning(string s) { Console.WriteLine(s); }
        public static void LogError(string s) { Console.WriteLine(s); }
    }

    public class MonoBehaviour
    {
        public static void print(string s) { Console.WriteLine(s); }
    }
#endif
    public enum SpaceplaneSituation { Preflight, Takeoff, SubsonicAscent, SonicTransition, HypersonicAscent, CoastToSpace, Circularize, InOrbit }
    

    public class AbortFlightPlanException : Exception
    {
        public AbortFlightPlanException()
        {
        }


        public AbortFlightPlanException(string message)
            : base(message)
        {
        }

        public AbortFlightPlanException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AICore : MonoBehaviour
    {
        private static Vessel vessel { get { return FlightGlobals.ActiveVessel; } }

        private static MuMech.MechJebCore core { get { return VesselExtensions.GetModules<MuMech.MechJebCore>(vessel)[0]; } }

        private ApplicationLauncherButton button = null;

        List<AIOperation> FlightPlan = null;
        [Persistent(pass=1)]
        public int FlightPlanStep = 0;
        [Persistent(pass=1)]
        public string FlightPlanFilename = null;
        private double RandomTimer = 0;
        private double RandomDuration = 60;
        public bool GlobalEnabled = false;
        protected Rect windowPos;
        Texture2D logo = null;
        public string OutputMessage = "AI Initiated";
        public string ManeuverStatus = "";


        //Part PartPlugin = new Part();

        public bool RandomTimeout()
        {
            if (TimeWarp.CurrentRateIndex > 0 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
                // Don't apply random chance during time warps.  That's no fun.
                return false;
            double UT = Planetarium.GetUniversalTime();
            if (UT > RandomTimer)
            {
                RandomTimer = UT + RandomDuration;
                return true;
            }
            return false;
        }

        private void CreateButtonIcon()
        {
            button = ApplicationLauncher.Instance.AddModApplication(
                null,//() => BuildAdvanced.Instance.Visible = true,
                null,//() => BuildAdvanced.Instance.Visible = false,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.FLIGHT,
                GameDatabase.Instance.GetTexture("RogueAI/Textures/icon", false)
                );
        }

        void CheckVessel()
        {

            if (vessel != null)
            {
                vessel.OnFlyByWire -= flyByWire; //just a safety precaution to avoid duplicates
                vessel.OnFlyByWire += flyByWire;
            }

        }



        void Start()  //Called when vessel is placed on the launchpad
        {
            CreateButtonIcon();
            if ((windowPos.x == 0) && (windowPos.y == 0))//windowPos is used to position the GUI window, lets set it in the center of the screen
            {
                windowPos = new Rect(Screen.width/2 - 250, Screen.height / 2-150, 10, 10);
            }
            RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));//start the GUI
            core.part.stackIcon.SetIcon(core.part, "GameData\\RogueAI\\icons.png", 0, 0);

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
            return orbit.PeA > orbit.referenceBody.atmosphereDepth && (orbit.ApA - orbit.PeA) / orbit.PeA < 0.2;
        }
        private float FlightHeadingElevation = 0f;
        private float FlightHeadingHeading = 0f;
        public System.Random random = new System.Random();
        public void holdFlightHeading(FlightCtrlState s, float elevation, float heading)
        {
            if (!core.attitude.users.Any() || elevation!=FlightHeadingElevation || heading!=FlightHeadingHeading)
                core.attitude.attitudeTo(Quaternion.Euler(elevation, heading, 0), AttitudeReference.SURFACE_NORTH, this);
            FlightHeadingHeading = heading;
            FlightHeadingElevation = elevation;
        }

        public void holdPrograde()
        {
            core.attitude.attitudeTo(Quaternion.Euler(0, 0, 0), AttitudeReference.ORBIT, this);
        }

        public void SetTarget(string targetName)
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



        public CelestialBody GetBody(string name)
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.name == name)
                    return body;
            }
            return null;
        }

        private bool CheckConditions(List<AICondition> list)
        {
            if (list == null)
                return false;
            foreach (AICondition condition in list)
            {
                if (!condition.Check())
                    return false;
                //Debug.Log("Condition passed " + condition.GetType().ToString());
            }
            return true;
        }

        public double VesselDeltaV()
        {
            var stats = core.GetComputerModule<MechJebModuleStageStats>();
            double deltaVtotal = 0;
            stats.RequestUpdate(this);
            for (int i = stats.atmoStats.Length - 1; i >= 0; i--)
            {
                FuelFlowSimulation.Stats s;
                if (vessel.atmDensity > 0)
                    s = stats.atmoStats[i];
                else
                    s = stats.vacStats[i];
                deltaVtotal += s.deltaV;
            }
            return deltaVtotal;
        }


        private List<AIOperation> GetFlightPlan()
        {
            AIXmlParser parser = new AIXmlParser(this);
            foreach (string filename in Directory.GetFiles("GameData\\RogueAI\\FlightPlans"))
            {
                if (filename.ToLower().EndsWith(".xml"))
                {
                    if (CheckConditions(parser.GetConditions(filename)))
                    {
                        vessel.RemoveAllManeuverNodes();
                        FlightPlanFilename = filename;
                        OutputMessage = "Executing flight plan " + Path.GetFileNameWithoutExtension(filename);
                        return parser.GetManeuvers(filename);
                    }
                }
            }
            return null;
        }

        private List<AIOperation> GetFlightPlan(string filename)
        {
            vessel.RemoveAllManeuverNodes();
            AIXmlParser parser = new AIXmlParser(this);
            return parser.GetManeuvers(filename);
        }

        public T RandomChoice<T>(IEnumerable<T> source,Func<T,bool> filter=null)
        {
            System.Random rnd = new System.Random();
            T result = default(T);
            int cnt = 0;
            foreach (T item in source)
            {
                if (item == null)
                    continue;
                if (filter!=null && !filter(item))
                    continue;
                cnt++;
                if (rnd.Next(cnt) == 0)
                {
                    result = item;
                }
            }
            return result;
        }

        public bool VesselFilter(Vessel v)
        {
            return v != null && v.vesselType == VesselType.Ship;
        }

        public string TimeTranslate(double dseconds)
        {
            uint seconds = (uint)dseconds;
            return string.Format("{0:0} d, {1:00} h, {2:00}m, {3:00}s", seconds / 86400, (seconds / 3600) % 24, (seconds / 60) % 60, seconds % 60);
        }

        public bool VesselFilterPlaneDeltaV(Vessel v)
        {
            if (v == null || v.vesselType != VesselType.Ship)
                return false;
            double UT = Planetarium.GetUniversalTime();
            Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(vessel.orbit, v.orbit, UT, out UT);
            return dV.magnitude<0.5*VesselDeltaV();
        }

        public ITargetable FlightPlanTarget = null;

        public void GetRandomShipTarget( Func<Vessel,bool> filterfunction)
        {
            Vessel newtarget=null;
            newtarget = RandomChoice(FlightGlobals.fetch.vessels, filterfunction);
            FlightGlobals.fetch.SetVesselTarget(newtarget);
        }


        public bool CheckManeuverNodes()
        {
            foreach (ManeuverNode node in vessel.patchedConicSolver.maneuverNodes)
            {
                if (node.DeltaV.magnitude > VesselDeltaV())
                    return false;
            }
            return true;
        }

        private void flyByWire(FlightCtrlState s)
        {
            if (core == null)
                return;
            if (!vessel.loaded)
                return;
            if (FlightPlan == null)
            {
                GlobalEnabled = false;
                if (FlightPlanFilename == null)
                    FlightPlan = GetFlightPlan();
                else
                    FlightPlan = GetFlightPlan(FlightPlanFilename);
            }
            else
            {
                GlobalEnabled = true;
                try
                {
                    if (FlightPlan[FlightPlanStep].Execute())
                    {
                        Debug.Log("FlightPlan " + FlightPlanFilename + " step " + FlightPlanStep+" completed");
                        FlightPlanStep += 1;
                        if (FlightPlanStep >= FlightPlan.Count)
                        {
                            FlightPlanStep = 0;
                            FlightPlan = null;
                            FlightPlanFilename = null;
                            FlightPlanTarget = null;
                        }
                    }
                }
                catch (AbortFlightPlanException ex)
                {
                    Debug.LogError("Flight plan aborted.  " + ex.Message);
                    FlightPlanStep = 0;
                    FlightPlan = null;
                    FlightPlanFilename = null;
                }
            }

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
        void disabledUpdate()
        {
            //CheckVessel();
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
            try
            {
                flyByWire(vessel.ctrlState);
            }
            catch (Exception ex)
            {
                Debug.LogError("Message ---"+ ex.Message);
                Debug.LogError("HelpLink ---"+ ex.HelpLink);
                Debug.LogError("Source ---"+ ex.Source);
                Debug.LogError("StackTrace ---"+ ex.StackTrace);
                Debug.LogError("TargetSite ---"+ ex.TargetSite);
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
            ApplicationLauncher.Instance.RemoveModApplication(button);
        }


        private void WindowGUI(int windowID)
        {
            if (logo == null)
            {
                logo = GameDatabase.Instance.GetTexture("RogueAI/Textures/krai_logo", false);
            }
            GUIStyle mySty = new GUIStyle(GUI.skin.button);
            mySty.normal.textColor = mySty.focused.textColor = Color.white;
            mySty.hover.textColor = mySty.active.textColor = Color.yellow;
            mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
            mySty.padding = new RectOffset(8, 8, 8, 8);

            GUILayout.BeginVertical();
            GUILayout.Box(logo);

            GUILayout.Box(OutputMessage);
            GUILayout.Box(ManeuverStatus);
            GUILayout.EndVertical();

            //DragWindow makes the window draggable. The Rect specifies which part of the window it can by dragged by, and is 
            //clipped to the actual boundary of the window. You can also pass no argument at all and then the window can by
            //dragged by any part of it. Make sure the DragWindow command is AFTER all your other GUI input stuff, or else
            //it may "cover up" your controls and make them stop responding to the mouse.
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

        }
        private void drawGUI()
        {
            if (GlobalEnabled)
            {
                GUI.skin = HighLogic.Skin;
                windowPos = GUILayout.Window(1, windowPos, WindowGUI, "", GUILayout.MinWidth(600));
            }
        }
    }
}
