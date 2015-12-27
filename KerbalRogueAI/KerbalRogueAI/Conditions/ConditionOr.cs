using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalRogueAI
{
    class ConditionOr : AICondition
    {
        public List<AICondition> ConditionList = new List<AICondition>();
        public ConditionOr(AICore aicore) : base(aicore) { }
        public override bool _condition()
        {
            foreach (AICondition condition in ConditionList)
                if (condition._condition())
                    return true;
            return false;
        }
    }
}
