using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalRogueAI
{
    class ConditionEncounter : AICondition
    {
        public ConditionEncounter(AICore aicore) : base(aicore) { }
        public string BodyName=null;

        public override bool _condition()
        {
            CelestialBody body = aicore.GetBody(BodyName);
            if (body == null)
                return false;
            return (vessel.orbit.nextPatch != null && vessel.orbit.nextPatch.referenceBody != null && vessel.orbit.nextPatch.referenceBody == body);
        }
    }
}
