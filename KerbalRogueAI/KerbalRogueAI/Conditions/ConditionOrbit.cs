using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalRogueAI
{
    class ConditionOrbit : AICondition
    {
        public ConditionOrbit(AICore aicore) : base(aicore) { }
        public override bool _condition()
        {
            return aicore.OrbitCircular(vessel.orbit);
        }
    }
}
