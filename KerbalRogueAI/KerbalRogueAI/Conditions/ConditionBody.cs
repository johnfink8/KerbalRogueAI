using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalRogueAI
{
    class ConditionBody : AICondition
    {
        public ConditionBody(AICore aicore) : base(aicore) { }
        public string BodyName=null;
        public override bool _condition()
        {
            CelestialBody body = aicore.GetBody(BodyName);
            return body != null;
        }
    }
}
