using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalRogueAI
{
    class ConditionEngine : AICondition
    {
        public ConditionEngine(AICore aicore) : base(aicore) { }

        public string EngineType = null;

        public override bool _condition()
        {
            ModuleEnginesFX moduleengine;
            foreach (Part part in vessel.parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    moduleengine = module as ModuleEnginesFX;
                    if (moduleengine != null)
                    {
                        if (EngineType==null)
                            return true;
                        if (moduleengine.engineType.ToString().ToLower() == EngineType.ToLower())
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
