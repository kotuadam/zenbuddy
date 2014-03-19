using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace DruidBuddy
{
    public class Druid : ClassBase
    {
        public override string StealthSpell { get { return "Prowl"; } }

        public override string MountName { get { return "Travel Form"; } }

        public override void Avoid()
        {
            ObjectManager.Update();

            if (StyxWoW.Me.IsFlying) return;

            if (StyxWoW.Me.IsStealthed && (Helper.NearByPlayer != null || Helper.NearByMob != null)) return;

            if (Helper.NearByPlayer != null && Settings.Instance.PreventPlayers)
            {
                if (!StyxWoW.Me.IsStealthed && SpellManager.CanCast(StealthSpell))
                {
                    Logging.Write(Colors.SkyBlue,
                        "Detected nearby player: {0} Level: {1} Class: {2} Distance {3}. Going stealth.",
                        Helper.NearByPlayer.Name, Helper.NearByPlayer.Level, Helper.NearByPlayer.Class,
                        StyxWoW.Me.Location.Distance(Helper.NearByPlayer.Location));

                    SpellManager.Cast(StealthSpell);
                }
            }
            else if (Helper.NearByMob != null && Settings.Instance.PreventPlayers)
            {
                if (!StyxWoW.Me.IsStealthed && SpellManager.CanCast(StealthSpell))
                {
                    Logging.Write(Colors.SkyBlue,
                        "Detected nearby mob: {0} Level: {1} Distance {2}. Going stealth.",
                        Helper.NearByMob.Name, Helper.NearByMob.Level,
                        StyxWoW.Me.Location.Distance(Helper.NearByMob.Location));

                    SpellManager.Cast(StealthSpell);
                }
            }
            //else if (StyxWoW.Me.IsSwimming)
            //{
            //    if (!StyxWoW.Me.HasAura("Aquatic Form"))
            //    {
            //        SpellManager.Cast("Aquatic Form");
            //    }
            //}
            //else
            //{
            //    if (!StyxWoW.Me.HasAura("Travel Form"))
            //    {
            //        SpellManager.CastSpellById(783);
            //    }
            //}
        }
    }
}
