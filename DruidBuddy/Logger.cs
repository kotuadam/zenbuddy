using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Styx;
using Styx.Common;
using Styx.CommonBot;

namespace DruidBuddy
{
    public static class Logger
    {
        private static string _lastMessage = "";

        public static void Info(string message, params object[] args)
        {
            try
            {
                string formatted = TreeRoot.IsRunning
                    ? string.Format("[DruidBuddy] [HP: " + StyxWoW.Me.HealthPercent.ToString("##.#") + "] [Mana: " + StyxWoW.Me.ManaPercent.ToString("##.#") + "] " + message, args)
                    : string.Format("[DruidBuddy] " + message, args);

                if (_lastMessage == formatted) return;
                _lastMessage = formatted;

                Logging.Write(Colors.White, formatted);
            }
            catch (Exception ex)
            {
                Logging.Write(Colors.Red, "Exception thrown at Log.Info(): {0}", ex);
            }

        }

        public static void Debug(string message, params object[] args)
        {
            try
            {
                Logging.Write(LogLevel.Diagnostic, Colors.Aquamarine, "[DruidBuddy - Debug] {0}", string.Format(message, args));
            }
            catch (Exception ex)
            {
                Logging.Write(Colors.Red, "Exception thrown at Log.Debug(): {0}", ex);
            }


        }

        public static void Fail(string message, params object[] args)
        {
            try
            {
                Logging.Write(LogLevel.Diagnostic, Colors.Crimson, "[DruidBuddy - Fail] {0}", string.Format(message, args));
            }
            catch (Exception ex)
            {
                Logging.Write(Colors.Red, "Exception thrown at Log.Fail(): {0}", ex);
            }

        }
    }
}
