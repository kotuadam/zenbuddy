using System.Linq;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace DruidBuddy
{
    public abstract class PickupForm
    {
        public ClassBase ClassBase
        {
            get
            {
                switch (StyxWoW.Me.Class)
                {
                    default:
                        return new Druid(); 
                }
            }
        }


        public abstract bool Run(WoWPoint location, int distance);
    }
}
