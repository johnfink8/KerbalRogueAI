using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MuMech;
using UnityEngine;

namespace KerbalRogueAI
{
    class ConditionLanded : AICondition
    {
        public ConditionLanded(AICore aicore) : base(aicore) { }

        public string LandedAt = null;
        public override bool _condition()
        {
            if (LandedAt == null)
                return vessel.Landed;
            else
                return vessel.landedAt.ToLower() == LandedAt.ToLower();
        }
    }
}
