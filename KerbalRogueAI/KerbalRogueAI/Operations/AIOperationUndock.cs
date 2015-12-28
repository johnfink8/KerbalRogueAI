using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalRogueAI
{
    class AIOperationUndock : AIOperation
    {
        public AIOperationUndock(AICore aicore) : base(aicore) { }

        public override bool _operation()
        {
            foreach (Part part in vessel.parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var grapple = module as ModuleGrappleNode;
                    if (grapple != null && grapple.dockedPartUId > 0)
                    {
                        grapple.Release();
                        return true;
                    }
                    var dock = module as ModuleDockingNode;
                    if (dock != null && dock.dockedPartUId > 0)
                    {
                        dock.Undock();
                        return true;
                    }
                }
            }
            return true;
        }
    }
}
