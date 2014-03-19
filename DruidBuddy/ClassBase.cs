using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace DruidBuddy
{
    public abstract class ClassBase
    {
        public abstract string StealthSpell { get; }

        public abstract string MountName { get; }
        
        public abstract void Avoid();
    }
}
