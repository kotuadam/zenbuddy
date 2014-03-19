using System;
using System.Threading;
using System.Windows.Media;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;

namespace DruidBuddy
{
    public class FlightForm : PickupForm
    {
        public override bool Run(WoWPoint location, int distance)
        {
            try
            {
                var newLoc = new WoWPoint(location.X, location.Y, location.Z + 40);

                var nearestLocationToObject = WoWMathHelper.CalculatePointFrom(StyxWoW.Me.Location, newLoc, 0);
                
                    try
                    {
                        if (Helper.BlacklistHotspot(nearestLocationToObject)) return true;

                        while (nearestLocationToObject.Distance(StyxWoW.Me.Location) >= 5 && TreeRoot.IsRunning)
                        {
                            if (Helper.BlacklistHotspot(nearestLocationToObject)) return true;

                            if (Helper.GetInCombat()) break;

                            ClassBase.Avoid();

                            Flightor.MoveTo(nearestLocationToObject, true);

                            Thread.Sleep(100);
                        }
                    }
                    catch (Exception exception)
                    {
                        Logging.Write(Colors.Red, exception.Message);
                        return false;
                    }
                

                return true;
            }
            catch (Exception exception)
            {
                Logging.Write(Colors.Red, exception.Message);
            }

            return true;
        }
    }
}
