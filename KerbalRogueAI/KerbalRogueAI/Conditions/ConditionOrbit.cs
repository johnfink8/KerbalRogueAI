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
        public string Body = null;
        public override bool _condition()
        {
            if (Body != null && Body.ToLower() != vessel.mainBody.name.ToLower())
                return false;
            return aicore.OrbitCircular(vessel.orbit);
        }
    }
}
