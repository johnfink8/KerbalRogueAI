using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    public abstract class AICondition
    {
        public AICore aicore = null;

        public static Vessel vessel { get { return FlightGlobals.ActiveVessel; } }

        public static MuMech.MechJebCore core { get { return VesselExtensions.GetModules<MuMech.MechJebCore>(vessel)[0]; } }

        public static double atmHeight { get { return vessel.mainBody.atmosphereDepth; } }

        public AICondition(AICore aicore)
        {
            this.aicore = aicore;
        }

        public abstract bool _condition();

        public bool Check()
        {
            return _condition();
        }
    }
}
