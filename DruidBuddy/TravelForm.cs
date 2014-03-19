using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;

namespace DruidBuddy
{
    public class TravelForm : PickupForm
    {
        private int _recursiveHandlerCount;
        private bool ShouldStayStealth { get { return Helper.NearByMob != null || Helper.NearByPlayer != null; } }
        public override bool Run(WoWPoint location, int distance)
        {
            
            var nearestLocationToObject = WoWMathHelper.CalculatePointFrom(StyxWoW.Me.Location, location, distance);
            var pathToDest =
                Navigator.GeneratePath(StyxWoW.Me.Location, nearestLocationToObject);

            if (_recursiveHandlerCount >= 5)
            {
                Helper.UseHeartStone();
                _recursiveHandlerCount = 0;
            }
            else
            {
                if (pathToDest.Count() == 0)
                {
                    _recursiveHandlerCount++;
                    Run(ProfileManager.CurrentProfile.HotspotManager.GetNextHotspot(), 10);

                    return false;
                }
            }


            foreach (var localDest in pathToDest)
            {
                if (!StyxWoW.Me.IsFlying && !Navigator.CanNavigateFully(StyxWoW.Me.Location, localDest, 8192))
                {
                    Logging.Write(Colors.Red, "Cant navigate to the destination.");
                    return false;
                }

                try
                {
                    if (Helper.BlacklistHotspot(localDest)) return true;

                    while (localDest.Distance(StyxWoW.Me.Location) >= 5 && TreeRoot.IsRunning)
                    {
                        if (Helper.BlacklistHotspot(localDest)) return true;

                        if (Helper.GetInCombat()) break;
                        
                        ClassBase.Avoid();
                        
                        if(!ShouldStayStealth) Helper.MountUp(ClassBase.MountName);

                        Navigator.MoveTo(localDest);

                        Thread.Sleep(100);
                    }
                    if (Helper.GetInCombat()) break;
                }
                catch (Exception exception)
                {
                    Logging.Write(Colors.Red, exception.Message);
                }
            }
            return true;
        }
    }
}
