using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Database;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Profiles;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace DruidBuddy
{
    public static class Helper
    {
        private static Stopwatch _hotspotWatcher = new Stopwatch();
        private static Stopwatch _nodeWatcher = new Stopwatch();
        private static bool _pickupIndoor;
        private static List<WoWPoint> _tempWoWPoints = new List<WoWPoint>();


        public static void MountUp(string mountName)
        {
            if (!StyxWoW.Me.HasAura(mountName) && Mount.CanMount())
            {
                SpellManager.Cast(mountName);
            }
        }

        
        public static bool GetCorpse()
        {
            var result = Helper.Goto(StyxWoW.Me.CorpsePoint, 10);
            if (result)
            {
                Thread.Sleep(TimeSpan.FromSeconds(3));
                Lua.DoString("RetrieveCorpse()");
            }
            else
            {
                SpiritRez();
            }

            return result;
        }
        private static void SpiritRez()
        {
            // spirit rez
            Lua.DoString("PortGraveyard()");
            Thread.Sleep(TimeSpan.FromSeconds(5));
            ObjectManager.Update();
            Thread.Sleep(TimeSpan.FromSeconds(5));
            var spiritHealer =
                ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(obj => obj.IsSpiritHealer);
            if (spiritHealer != null)
            {
                Goto(spiritHealer.Location, 3);
                spiritHealer.Interact();

                Lua.DoString("RunMacroText(\"/click StaticPopup1Button1\")");
                Thread.Sleep(TimeSpan.FromSeconds(2));
                Lua.DoString("RunMacroText(\"/click StaticPopup1Button1\")");
            }
        }
        public static void Repair()
        {
            if (!StyxWoW.Me.Combat)
            {
                //short way
                UseHeartStone();
            }

            WoWPoint location = WoWPoint.Zero;
            var repairNpc = NpcQueries.GetNearestNpc(StyxWoW.Me.FactionTemplate, StyxWoW.Me.MapId, StyxWoW.Me.Location,
                UnitNPCFlags.Repair);

            if (repairNpc == null)
            {
                var vendor = ProfileManager.CurrentProfile.VendorManager.Vendors.FirstOrDefault().FirstOrDefault();

                location = vendor != null ? vendor.Location : new WoWPoint(3015.596, -541.4718, 248.5892);
            }
            else
            {
                location = repairNpc.Location;
            }

            var result = Goto(location, 3);
            if (result)
            {
                var npc = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(x => repairNpc != null && x.Entry == repairNpc.Entry);
                if (npc != null) npc.Interact();

                if (GossipFrame.Instance != null && GossipFrame.Instance.IsVisible &&
                        GossipFrame.Instance.GossipOptionEntries != null)
                {
                    foreach (GossipEntry ge in GossipFrame.Instance.GossipOptionEntries)
                    {
                        if (ge.Type == GossipEntry.GossipEntryType.Vendor)
                        {
                            GossipFrame.Instance.SelectGossipOption(ge.Index);
                            break;
                        }
                    }
                }

                Lua.DoString("RepairAllItems(1)");
            }

        }
        public static bool Goto(WoWPoint location, int distance)
        {
            PickupForm f;
            switch (Settings.Instance.Form)
            {
                case Settings.Forms.TravelForm:
                    f = new TravelForm();
                    if (!f.Run(location, distance)) return false;
                    break;
                case Settings.Forms.FlightForm:
                    f = new FlightForm();
                    if (!f.Run(location, distance)) return false;
                    break;
                default:
                    Navigator.MoveTo(location);
                    break;
            }
            return true;
        }
        internal static bool BlacklistHotspot(WoWPoint localDest)
        {
            if (!_tempWoWPoints.Contains(localDest))
            {
                _tempWoWPoints.Add(localDest);

                if (!_hotspotWatcher.IsRunning)
                {
                    _hotspotWatcher = new Stopwatch();
                    _hotspotWatcher.Start();
                }
            }
            else if (_tempWoWPoints.Contains(localDest) && (_hotspotWatcher.Elapsed > TimeSpan.FromMinutes(1)))
            {
                BlackspotManager.AddBlackspot(localDest, 10, 10);
                _tempWoWPoints.Remove(localDest);
                _hotspotWatcher.Stop();
                _tempWoWPoints.Clear();
                return true;
            }
            else if (!_tempWoWPoints.Contains(localDest) && _hotspotWatcher.Elapsed > TimeSpan.FromMinutes(1))
            {
                _tempWoWPoints.Clear();
                _hotspotWatcher.Stop();
            }
            return false;
        }

        static List<WoWGameObject> nodes = new List<WoWGameObject>();

        internal static bool BlackListObject(WoWGameObject source)
        {
            if (!nodes.Contains(source))
            {
                nodes.Add(source);

                if (!_hotspotWatcher.IsRunning)
                {
                    _nodeWatcher = new Stopwatch();
                    _nodeWatcher.Start();
                }
            }
            else if (nodes.Contains(source) && (_hotspotWatcher.Elapsed > TimeSpan.FromMinutes(1)))
            {
                Blacklist.Add(source.Guid, BlacklistFlags.Node, TimeSpan.FromMinutes(2));
                nodes.Remove(source);
                _nodeWatcher.Stop();
                _tempWoWPoints.Clear();
                return true;
            }
            else if (!nodes.Contains(source) && _hotspotWatcher.Elapsed > TimeSpan.FromMinutes(1))
            {
                nodes.Clear();
                _nodeWatcher.Stop();
            }
            return false;
        }

        internal static void UseHeartStone()
        {
            Lua.DoString("RunMacroText(\"/use Hearthstone\")");
            Thread.Sleep(TimeSpan.FromSeconds(20));
        }

        internal static bool GetInCombat()
        {
            if (Settings.Instance.NoCombat && !ShouldHarvest) return false;

            if (StyxWoW.Me.Combat)
            {
                var nearbyMobs =
                    ObjectManager.GetObjectsOfType<WoWUnit>()
                        .Where(obj => obj.Aggro && obj.Attackable && !obj.Elite && obj.IsHostile).ToList();

                SpellManager.Cast(nearbyMobs.Count >= 3 ? "Bear Form" : "Cat Form");

                return true;
            }
            return false;
        }

        public static bool ShouldHarvest
        {
            get { return NearByOres.Any() || NearByHerbs.Any(); }
        }

        public static WoWPlayer NearByPlayer
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWPlayer>()
                        .Where(x => x.Location.Distance(StyxWoW.Me.Location) < 99)
                        .OrderBy(x => StyxWoW.Me.Location.Distance(x.Location))
                        .FirstOrDefault();
            }
        }

        public static WoWUnit NearByMob
        {
            get
            {
                return (from mob in ObjectManager.GetObjectsOfType<WoWUnit>()
                        where mob.IsHostile && !mob.IsDead && !mob.IsFriendly && !mob.IsNeutral && mob.Distance < mob.GetAggroRange(StyxWoW.Me) + 4
                        select mob).FirstOrDefault();
            }
        }

        public static List<WoWGameObject> NearByHerbs
        {
            get
            {
                return

                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                        .Where(obj =>
                        {
                            _pickupIndoor = (Settings.Instance.PickupInDoor
                                ? obj.IsIndoors || !obj.IsIndoors
                                : !obj.IsIndoors);
                            return obj.SubType == WoWGameObjectType.Chest &&
                                   Settings.Instance.PickupHerbList.Contains(obj.Entry) &&
                                   !Blacklist.Contains(obj.Guid, BlacklistFlags.Node) && _pickupIndoor;
                        })
                        .OrderBy(obj => obj.Location.Distance(StyxWoW.Me.Location))
                        .ToList();
            }
        }

        public static List<WoWGameObject> NearByOres
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                        .Where(obj =>
                        {
                            _pickupIndoor = (Settings.Instance.PickupInDoor
                                ? obj.IsIndoors || !obj.IsIndoors
                                : !obj.IsIndoors);
                            return obj.SubType == WoWGameObjectType.Chest &&
                                   Settings.Instance.PickupOreList.Contains(obj.Entry) &&
                                   !Blacklist.Contains(obj.Guid, BlacklistFlags.Node) && _pickupIndoor;
                        })
                        .OrderBy(obj => obj.Location.Distance(StyxWoW.Me.Location))
                        .ToList();
            }
        }
    }
}
