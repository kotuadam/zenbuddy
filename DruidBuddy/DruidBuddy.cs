using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Database;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using System.Windows.Media;
using Action = Styx.TreeSharp.Action;

namespace DruidBuddy
{
    public class DruidBuddy : BotBase
    {
        private Stopwatch _nodeWatcher = new Stopwatch();
        private Stopwatch _hotspotWatcher = new Stopwatch();
        private ClassBase _class;
        
        public override void Initialize()
        {
            Settings.Instance.Load();
            Mount.OnMountUp += Mount_OnMountUp;
            _class = new Druid();
        }


        public override Form ConfigurationForm
        {
            get { return new UI(); }
        }

        void Mount_OnMountUp(object sender, MountUpEventArgs e)
        {
            e.Cancel = true;
        }

        public override bool RequiresProfile
        {
            get { return true; }
        }

        #region Properties

        private static Composite _root;

        private bool _shouldHarvest;
        public DruidBuddy()
        {
            LastIndex = 0;
        }

        public List<WoWGameObject> NearByOres
        {
            get
            {
                return
                    Helper.NearByOres;
            }
        }

        public List<WoWGameObject> NearByHerbs
        {
            get
            {
                return

                    Helper.NearByHerbs;
            }
        }

        public static int NearestGBankFromDb()
        {
            var unit = NpcQueries.GetNearestNpc(StyxWoW.Me.FactionTemplate, StyxWoW.Me.MapId,
                StyxWoW.Me.Location, UnitNPCFlags.GuildBanker);


            if (unit != null) return unit.Entry;

            return 0;
        }

        public static WoWGameObject NearestGBank
        {
            get
            {

                var gbank = ObjectManager.GetObjectsOfType<WoWGameObject>().OrderBy(o => o.Distance).FirstOrDefault(o => o.SubType == WoWGameObjectType.GuildBank) ??
                            ObjectManager.GetObjectsOfType<WoWGameObject>().OrderBy(o => o.Distance).FirstOrDefault(o => o.Entry == NearestGBankFromDb());
                return gbank;
            }
        }

        public WoWPlayer NearByPlayer
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

        public bool HasResSick
        {
            get { return StyxWoW.Me.Auras.Values.Any(x => x.Name == "Resurrection Sickness"); }
        }

        public bool NeedRepairs
        {
            get { return StyxWoW.Me.DurabilityPercent < 0.2; }
        }

        #endregion

        #region Implemented Members

        public override string Name
        {
            get { return "Druid Buddy v01"; }
        }

        public override Composite Root
        {
            get { return _root ?? (_root = MainRotation()); }
        }

        public override PulseFlags PulseFlags
        {
            get { return PulseFlags.Objects; }
        }

        public int LastIndex { get; set; }

        #endregion

        #region Composites

        private Composite MainRotation()
        {
            return new PrioritySelector(
                new Decorator(ret => StyxWoW.Me.IsDead, new Action(delegate
                {
                    Lua.DoString("RepopMe()");
                })),
                new Decorator(ret => StyxWoW.Me.IsGhost, new Action(
                    delegate
                    {
                        Logger.Info("Going to get corpse");
                        Helper.GetCorpse();
                    })),
                new Decorator(ret => HasResSick, new ActionAlwaysSucceed()),
                new Decorator(ret => NeedRepairs, new Action(delegate
                {
                    Logger.Info("Going to repair.");
                    Helper.Repair();
                })),

                new PrioritySelector(
                    new Decorator(ret => StyxWoW.Me.FreeNormalBagSlots > 10 && StyxWoW.Me.MapId == 1,
                        new Action(delegate
                        {
                            Logger.Info("Going to prtal to Jade Forest");
                            var portaltoJadeForestLocation = new WoWPoint(2018.074, -4698.36, 28.54008);
                            if (Goto(portaltoJadeForestLocation))
                            {
                                var portalToJAdeForest =
                                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                                        .FirstOrDefault(x => x.Entry == 215424);

                                if (portalToJAdeForest != null)
                                {
                                    Goto(portalToJAdeForest.Location);
                                    Thread.Sleep(TimeSpan.FromSeconds(10));
                                    portalToJAdeForest.Interact();
                                }
                            }
                        })),
                    new Decorator(ret => StyxWoW.Me.FreeNormalBagSlots == 0 && StyxWoW.Me.MapId == 1,
                        new Action(delegate
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(30));
                            // Sell junk
                            Logger.Info("Going to sell junk");
                            var vendor =
                                ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .FirstOrDefault(x => x.Entry == NpcQueries.GetNpcById(69333).Entry) ??
                                ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .FirstOrDefault(
                                        x =>
                                            x.Entry ==
                                            NpcQueries.GetNearestNpc(StyxWoW.Me.MapId, StyxWoW.Me.Location,
                                                UnitNPCFlags.AnyVendor).Entry);

                            if (vendor != null && Goto(vendor.Location))
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(10));

                                vendor.Interact();

                                Thread.Sleep(TimeSpan.FromSeconds(3));

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

                                foreach (
                                    var item in
                                        StyxWoW.Me.BagItems.Where(x => x.ItemInfo.Id == 89641 || x.ItemInfo.Id == 89640)
                                    )
                                {
                                    item.UseContainerItem();
                                    Thread.Sleep(TimeSpan.FromSeconds(1));
                                }
                            }
                            Logger.Info("Going to dump bags to guild bank");
                            //<Hotspot X="1924.377" Y="-4684.323" Z="35.01741" />
                            var bankLocation = new WoWPoint(1924.377, -4684.323, 35.01741);
                            if (Goto(bankLocation))
                            {
                                if (Goto(NearestGBank))
                                {
                                    NearestGBank.Interact();
                                    Thread.Sleep(TimeSpan.FromSeconds(5));
                                    const string putInGBankLuaFormatv2 = @"       

        for tab = 1, GetNumGuildBankTabs() do
          QueryGuildBankTab(tab)
          SetCurrentGuildBankTab(tab)
          local freeSlot = 98
          for slot = 1, 98 do
             itemLink = GetGuildBankItemLink(tab, slot)
             if itemLink then
               local m = select(1, GetItemInfo(itemLink))
               if m then
			        freeSlot = freeSlot - 1
                end
             end
          end
          if freeSlot > 0 then

            for b = 0,4 do 
	           for s = 1, GetContainerNumSlots(b) do 
	              local n = GetContainerItemLink(b,s)
	              if n then
	                 local m = select(7, GetItemInfo(n))
	                 if m and (strfind(m,'Metal') or strfind(m,'Herb')) then  
	                    UseContainerItem(b,s)
	                 end
	              end 
	           end 
	        end  		        
          end
        end
";
                                    Lua.DoString(putInGBankLuaFormatv2);
                                    Thread.Sleep(TimeSpan.FromSeconds(15));
                                    Lua.DoString(putInGBankLuaFormatv2);
                                }
                            }


                            Thread.Sleep(TimeSpan.FromMinutes(2));

                        })),
                    new Decorator(ret => StyxWoW.Me.FreeNormalBagSlots == 0 && StyxWoW.Me.MapId == 870,
                        new Action(delegate
                        {
                            Logger.Info("Bags full, going to portal to Orgrimmar");
                            var portalLocation = new WoWPoint(3004.827, -546.91, 248.1157);
                            var result = Goto(portalLocation);

                            //<Hotspot X="3004.827" Y="-546.919" Z="248.1157" />

                            if (result)
                            {
                                var portal =
                                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                                        .FirstOrDefault(x => x.Entry == 210804);
                                if (portal != null) portal.Interact();
                                while (!StyxWoW.IsInGame)
                                {
                                    Thread.Sleep(TimeSpan.FromSeconds(5));
                                }
                            }

                        })),
                    
                    new Decorator(ret => StyxWoW.Me.Combat,
                        new PrioritySelector(

                            #region Heal

                            new PrioritySelector(
                                // Use the Behavior
                                new Decorator(ctx => RoutineManager.Current.HealBehavior != null,
                                    new Sequence(
                                        RoutineManager.Current.HealBehavior,
                                        new Action(delegate { return RunStatus.Success; })
                                        )),

                                // Don't use the Behavior
                                new Decorator(ctx => RoutineManager.Current.NeedHeal,
                                    new Sequence(
                                        new Action(ret => TreeRoot.StatusText = "Healing"),
                                        new Action(ret => RoutineManager.Current.Heal())
                                        ))),

                            #endregion

                            #region Combat Buffs

                            new PrioritySelector(
                                // Use the Behavior
                                new Decorator(ctx => RoutineManager.Current.CombatBuffBehavior != null,
                                    new Sequence(
                                        RoutineManager.Current.CombatBuffBehavior,
                                        new Action(delegate { return RunStatus.Success; })
                                        )
                                    ),

                                // Don't use the Behavior
                                new Decorator(ctx => RoutineManager.Current.NeedCombatBuffs,
                                    new Sequence(
                                        new Action(ret => TreeRoot.StatusText = "Applying Combat Buffs"),
                                        new Action(ret => RoutineManager.Current.CombatBuff())
                                        ))),

                            #endregion

                            #region Combat

                            new PrioritySelector(
                                // Use the Behavior
                                new Decorator(ctx => RoutineManager.Current.CombatBehavior != null,
                                    new PrioritySelector(
                                        RoutineManager.Current.CombatBehavior,
                                        new Action(delegate { return RunStatus.Success; })
                                        )),

                                // Don't use the Behavior
                                new Sequence(
                                    new Action(ret => TreeRoot.StatusText = "Combat"),
                                    new Action(ret => RoutineManager.Current.Combat())))

                            #endregion

                            )),
                    //new Decorator(ret => StyxWoW.Me.Mounted && Settings.Instance.Form == Settings.Forms.TravelForm,
                    //    new Action(delegate
                    //    {
                    //        Mount.Dismount();
                    //    })),
                    new Decorator(ret => _shouldHarvest, new Sequence(
                        new Action(delegate
                        {
                            Logging.Write(Colors.MediumAquamarine, "Found a node.");

                            if (NearByOres.Any())
                            {
                                foreach (var source in NearByOres)
                                {
                                    if (Helper.GetInCombat()) break;
                                    if (!Goto(source))
                                    {
                                        Blacklist.Add(source, BlacklistFlags.Node, TimeSpan.FromMinutes(20));
                                        return;
                                    }
                                    Thread.Sleep(TimeSpan.FromSeconds(1));
                                    Logging.Write(Colors.MediumAquamarine, "Picking up: {0}", source.Name);

                                    if (Helper.BlackListObject(source)) return;
                                    Mount.Dismount();
                                    source.Interact();
                                    Thread.Sleep(TimeSpan.FromSeconds(5));
                                }
                                ObjectManager.Update();
                            }

                            if (NearByHerbs.Any())
                            {
                                foreach (var source in NearByHerbs)
                                {
                                    if (Helper.GetInCombat()) break;
                                    if (!Goto(source)) return;
                                    Thread.Sleep(TimeSpan.FromSeconds(1));
                                    Logging.Write(Colors.MediumAquamarine, "Picking up: {0}", source.Name);

                                    if (Helper.BlackListObject(source)) return;
                                    Mount.Dismount();
                                    source.Interact();
                                    Thread.Sleep(TimeSpan.FromSeconds(2));
                                }
                                ObjectManager.Update();
                            }

                            _shouldHarvest = false;
                        })
                        )),
                    new Decorator(ret => StyxWoW.Me.IsValid, new Action(delegate
                    {
                        var closestHotspot =
                            ProfileManager.CurrentProfile.HotspotManager.Hotspots.OrderBy(
                                x => StyxWoW.Me.Location.Distance(x)).FirstOrDefault();

                        for (var i = 0; i < ProfileManager.CurrentProfile.HotspotManager.Hotspots.Count; i++)
                        {
                            if (ProfileManager.CurrentProfile.HotspotManager.Hotspots[i] == closestHotspot)
                            {
                                LastIndex = i + 1;
                                break;
                            }
                        }

                        if (LastIndex == ProfileManager.CurrentProfile.HotspotManager.Hotspots.Count) LastIndex = 0;

                        for (var i = LastIndex; i < ProfileManager.CurrentProfile.HotspotManager.Hotspots.Count; i++)
                        {
                            ObjectManager.Update();

                            if (Helper.GetInCombat()) break;

                            if (NearByOres.Any() || NearByHerbs.Any())
                            {
                                _shouldHarvest = true;
                                LastIndex = i;
                                break;
                            }

                            Goto(ProfileManager.CurrentProfile.HotspotManager.Hotspots[i]);
                        }
                    }))));

        }



        #endregion

        #region Helper Methods
        
        public bool Goto(WoWGameObject obj)
        {
            return Goto(obj.Location);
        }

        public bool Goto(WoWPoint location)
        {
            //if (BlackspotManager.IsBlackspotted(location, 20))
            //    return Goto(ProfileManager.CurrentProfile.HotspotManager.GetNextHotspot());
            var rnd = new Random();
            var randomDistance = rnd.Next(1, 3);
            return Helper.Goto(location, randomDistance);
        }
        #endregion
    }
}
