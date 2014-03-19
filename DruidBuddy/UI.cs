using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Styx.WoWInternals;

namespace DruidBuddy
{
    public partial class UI : Form
    {
        public UI()
        {
            InitializeComponent();
            Settings.Instance.Load();
            propertyGrid1.SelectedObject = Settings.Instance;
        }

        

        private void button1_Click(object sender, EventArgs e)
        {
            Settings.Instance.Save();
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
           
            var putInGBankLuaFormatv2 = @"       

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
        }
        
    }
}
