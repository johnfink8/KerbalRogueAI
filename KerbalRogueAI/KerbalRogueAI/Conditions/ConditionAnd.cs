using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalRogueAI
{
    class ConditionAnd : AICondition
    {
        public List<AICondition> ConditionList = new List<AICondition>();
        public ConditionAnd(AICore aicore) : base(aicore) { }
        public override bool _condition()
        {
            foreach (AICondition condition in ConditionList)
                if (!condition._condition())
                {
                    return false;
                }
            return true;
        }
    }
}
