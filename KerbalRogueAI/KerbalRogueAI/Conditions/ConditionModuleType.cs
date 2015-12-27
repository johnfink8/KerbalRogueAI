using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalRogueAI
{
    class ConditionModuleType : AICondition
    {
        public ConditionModuleType(AICore aicore) : base(aicore) { }
        public string ModuleType = null;
        public override bool _condition()
        {
            foreach (Part part in vessel.Parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    if (module.GetType().Name == ModuleType)
                        return true;
                }
            }
            return false;

        }
    }
}
