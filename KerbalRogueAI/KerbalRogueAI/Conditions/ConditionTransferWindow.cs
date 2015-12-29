using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalRogueAI
{
    class ConditionTransferWindow : AICondition
    {
        public ConditionTransferWindow(AICore aicore) : base(aicore) { }
        public Orbit source = vessel.orbit;
        public Orbit destination;

        public override bool _condition()
        {
            double window = aicore.GetTransferWindow(destination);
            if (window<0)
                return false;
            return Math.Abs(window-Planetarium.GetUniversalTime()) < 86400*7; // within 1 week of transfer window
        }
    }
}
