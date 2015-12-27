using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MuMech;

namespace KerbalRogueAI
{
    class ConditionDeltaV : AICondition
    {
        public ConditionDeltaV(AICore aicore) : base(aicore) { }

        public double Quantity = 0;

        public override bool _condition()
        {
            var stats = core.GetComputerModule<MechJebModuleStageStats>();
            double deltaVtotal = 0;
            stats.RequestUpdate(this);
            for (int i = stats.atmoStats.Length - 1; i >= 0; i--)
            {
                FuelFlowSimulation.Stats s;
                if (vessel.atmDensity > 0)
                    s=stats.atmoStats[i];
                else
                    s=stats.vacStats[i];
                deltaVtotal+=s.deltaV;
            }
            return deltaVtotal >= Quantity;
        }
    }
}
