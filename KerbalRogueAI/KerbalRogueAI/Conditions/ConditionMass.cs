using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalRogueAI
{
    class ConditionMass : AICondition
    {
        public ConditionMass(AICore aicore) : base(aicore) { }
        public float MassGT = -1;
        public float MassLT = -1;
        public override bool _condition()
        {
            bool ret = true;
            if (MassGT >= 0)
            {
                ret = ret && vessel.totalMass > MassGT;
            }
            if (MassLT >= 0)
            {
                ret = ret && vessel.totalMass < MassLT;
            }
            return ret;

        }
    }
}
