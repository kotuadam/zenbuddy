using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Documents;
using System.Xml.Serialization;
using Styx;
using Styx.Common;
using Styx.Helpers;

namespace DruidBuddy
{
    public class Settings
    {
        public enum Forms
        {
            TravelForm,
            FlightForm
        }

        private static Settings _instance;


        public static Settings Instance
        {
            get
            {
                return _instance ?? (_instance = new Settings());
            }

            set { _instance = value; }
        }

        [XmlElement]
        [DisplayName("Prevent Mobs")]
        [Description("Bot will go stealth if it detects any mobs in X yards. This behavior is not valid when flying.")]
        public bool PreventMobs { get; set; }

        [XmlElement]
        [DisplayName("Prevent Players")]
        [Description("Bot will go stealth if it detects any players in X yards. This behavior is not valid when flying.")]
        public bool PreventPlayers { get; set; }

        [XmlElement]
        [DisplayName("Pickup Indoors")]
        [Description("Picksup whether nodes are in caves.")]
        public bool PickupInDoor { get; set; }

        [XmlElement]
        [DisplayName("No Combat")]
        [Description("Bot never gets in combat. Instead it will continue running.")]
        public bool NoCombat { get; set; }

        [XmlArray("PickupHerbList"), XmlArrayItem("PickupHerbList", typeof(uint))]
        [DisplayName("Herb List")]
        [Description("Bot will pickup selected herbs")]
        public uint[] PickupHerbList { get; set; }

        [XmlArray("PickupOreList"), XmlArrayItem("PickupOreList", typeof(uint))]
        [DisplayName("Ore List")]
        [Description("Bot will pickup selected ores")]
        public uint[] PickupOreList { get; set; }

        [XmlElement]
        [DisplayName("Pickup Form")]
        [Description("Flight form or travel form")]
        public Forms Form { get; set; }

        public void Save()
        {
            var filePath = Path.Combine(Utilities.AssemblyDirectory,
                string.Format(@"Settings/DruidBuddy/DruidBuddy-Settings-" + StyxWoW.Me.Name + ".xml"));
            if (!File.Exists(filePath))
            {
                File.Create(filePath); 
            }

            var serializer = new XmlSerializer(typeof(Settings));
            TextWriter textWriter = new StreamWriter(filePath);
            serializer.Serialize(textWriter, Instance);
            textWriter.Close();
        }

        public void Load()
        {
            var filePath = Path.Combine(Utilities.AssemblyDirectory,
                string.Format(@"Settings/DruidBuddy/DruidBuddy-Settings-" + StyxWoW.Me.Name + ".xml"));
            if (!File.Exists(filePath)) { Save(); }
            var deserializer = new XmlSerializer(typeof(Settings));
            TextReader textReader = new StreamReader(filePath);
            var settings = (Settings)deserializer.Deserialize(textReader);
            textReader.Close();
            Instance = settings;
        }
    }
}
