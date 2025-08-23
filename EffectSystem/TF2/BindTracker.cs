using System.Text.RegularExpressions;

using TF2FrameworkInterface;

namespace EffectSystem.TF2
{
    public class BindTracker
    {
        private PollingCacheTF2Proxy tf2;
        private List<CommandBinding> binds = new List<CommandBinding>();

        public BindTracker(PollingCacheTF2Proxy pollingCacheTF2Proxy)
        {
            this.tf2 = pollingCacheTF2Proxy;
        }

        private void CacheBinds()
        {
            binds.Clear();

            //key_findbinding +forward
            string boundKeys = tf2.RunCommandRaw("key_listboundkeys");
            StringReader bindreader = new StringReader(boundKeys);
            string? bind = bindreader.ReadLine();
            while (bind != null)
            {
                // each bind is like:
                // "`" = "toggleconsole"
                Match matcher = TF2Instance.VariableMatch.Match(bind);
                if (matcher.Success)
                {
                    string key = matcher.Groups["variable"].Value;
                    string command = matcher.Groups["value"].Value;

                    binds.Add(new CommandBinding(tf2, key, command));
                }

                bind = bindreader.ReadLine();
            }
        }

        public CommandBinding? GetCommandBinding(string command)
        {
            if (binds.Count == 0)
                CacheBinds();

            return binds.FirstOrDefault(b => b.OriginalCommand == command);
        }
    }


    /*
     * 
     * update from my own stuff using https://wiki.teamfortress.com/wiki/List_of_default_keys
     * 
     * meant to be a reference for default key_listboundkeys 
     * 
"1" = "slot1"
"2" = "slot2"
"3" = "slot3"
"4" = "slot4"
"5" = "slot5"
"6" = "slot6"
"7" = "slot7"
"8" = "slot8"
"9" = "slot9"
"a" = "+moveleft"
"b" = "lastdisguise"
"c" = "voice_menu_3"
"d" = "+moveright"
"e" = "voicemenu 0 0"
"f" = "+inspect"
"g" = "+taunt"
"h" = "+use_action_slot_item"
"l" = "+drop"?"sayAndDrop"
"m" = "open_charinfo_direct"
"q" = "lastinv"
"r" = "+reload"
"s" = "+back"
"v" = "+voicerecord"
"u" = "say_team"
"w" = "+forward"
"x" = "voice_menu_2"
"y" = "say"
"z" = "voice_menu_1"
"`" = "toggleconsole"
"," = "changeclass"
"." = "changeteam"
"/" = "+movedown"
"-" = "disguiseteam"
// ? "ENTER" = "cl_decline_first_notification"
"SPACE" = "+jump"
//???"BACKSPACE" = "vr_toggle"
"TAB" = "+showscores"
//???"CAPSLOCK" = "vr_reset_home_pos"
//???"NUMLOCK" = "say_party"
"ESCAPE" = "cancelselect"
//??"PGUP" = "+lookup"
//??"PGDN" = "+lookdown"
//??"PAUSE" = "pause"
//??"ALT" = "+strafe"
"CTRL" = "+duck"
"LEFTARROW" = "+left"
"RIGHTARROW" = "+right"
"F1" = "+showroundinfo"
"F2" = "show_quest_log"
"F3" = "askconnect_accept"
"F4" = "cl_trigger_first_notification"
"F5" = "screenshot"
"F6" = "save_replay"
"F7" = "show_matchmaking"
"F9" = "abuse_report_queue"
"F10" = "quit prompt"
"F12" = "replay_togglereplaytips"
"MOUSE1" = "+attack"
"MOUSE2" = "+attack2"
"MOUSE3" = "+attack3"
//"MWHEELUP" = ?next weapon
//"MWHEELDOWN" = ?prev weapon
     */


    public class CommandBinding
    {
        private readonly TF2Proxy tf2;

        public CommandBinding(TF2Proxy tf2, string key, string originalCommand)
        {
            this.tf2 = tf2;
            Key = key;
            OriginalCommand = originalCommand;
            CurrentCommand = originalCommand;
        }

        public bool IsChanged => CurrentCommand != OriginalCommand;
        public string Key { get; }
        public string OriginalCommand { get; }

        private string CurrentCommand { get; set; }

        public void ChangeCommand(string newCommand)
        {
            if (IsChanged)
                throw new InvalidOperationException("Command Already Changed");

            EndCurrentCommand();

            Bind(Key, newCommand);
            CurrentCommand = newCommand;
        }

        private void EndCurrentCommand()
        {
            if (!CurrentCommand.StartsWith('+'))
                return;

            string endCommand = CurrentCommand.Replace('+', '-');

            _ = tf2.RunCommand(endCommand);
        }

        private void Bind(string key, string newCommand)
        {
            _ = tf2.RunCommand(string.Format(
                "bind \"{0}\" \"{1}\"",
                key,
                newCommand));
        }

        public void RestoreCommand()
        {
            EndCurrentCommand();

            Bind(Key, OriginalCommand);
            CurrentCommand = OriginalCommand;
        }
    }


}