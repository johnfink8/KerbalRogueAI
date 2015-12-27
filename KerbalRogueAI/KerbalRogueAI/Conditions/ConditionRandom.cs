using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalRogueAI
{
    class ConditionRandom : AICondition
    {
        public ConditionRandom(AICore aicore) : base(aicore) { }

        public double Chance = 0;

        public override bool _condition()
        {
            if (!aicore.RandomTimeout())
                return false;
            return aicore.random.NextDouble() < Chance;
        }
    }
}
