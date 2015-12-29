using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalRogueAI
{
    class ConditionRandom : AICondition
    {
        public ConditionRandom(AICore aicore) : base(aicore) { }

        public int Chance = 0;

        public override bool _condition()
        {
            // Skip checks during time warp
            if (TimeWarp.CurrentRateIndex > 0 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
                return false;
            // This should effectively mean that we have Chance percent chance per minute of passing
            int roll = aicore.random.Next((int)(100 * 60 / Time.fixedDeltaTime));
            return roll <= Chance;
        }
    }
}
