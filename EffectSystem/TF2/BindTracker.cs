using System.Text.RegularExpressions;
using TF2FrameworkInterface;

namespace EffectSystem.TF2
{
    /// <summary>
    /// Bind-handling component of PollingCacheTF2Proxy
    /// </summary>
    internal class BindTracker
    {
        private PollingCacheTF2Proxy tf2Proxy;
        private List<CommandBinding> binds = new List<CommandBinding>();

        public BindTracker(PollingCacheTF2Proxy pollingCacheTF2Proxy)
        {
            this.tf2Proxy = pollingCacheTF2Proxy;
        }

        private void CacheBinds()
        {
            List<(string key, string command)> boundKeysList = GetBoundKeys();

            binds.Clear();
            binds.AddRange(boundKeysList.Select(
                (b) => new CommandBinding(tf2Proxy, b.key, b.command)));
        }

        private List<(string key, string command)> GetBoundKeys()
        {
            List<(string key, string command)> boundKeysList = new List<(string, string)>();
            //key_findbinding +forward
            string boundKeys = tf2Proxy.RunCommandRaw("key_listboundkeys");

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
                    boundKeysList.Add((key, command));
                }

                bind = bindreader.ReadLine();
            }

            return boundKeysList;
        }

        public CommandBinding? GetCommandBinding(string command)
        {
            if (binds.Count == 0)
                CacheBinds();

            return binds.FirstOrDefault(b => b.OriginalCommand == command);
        }
    }


    //* update from my own stuff using https://wiki.teamfortress.com/wiki/List_of_default_keys
    //* meant to be a reference for default key_listboundkeys 
    //"1" = "slot1"
    //"2" = "slot2"
    //"3" = "slot3"
    //"4" = "slot4"
    //"5" = "slot5"
    //"6" = "slot6"
    //"7" = "slot7"
    //"8" = "slot8"
    //"9" = "slot9"
    //"0" = "slot10"
    //"a" = "+moveleft"
    //"b" = "lastdisguise"
    //"c" = "voice_menu_3"
    //"d" = "+moveright"
    //"e" = "voicemenu 0 0"
    //    = "+helpme" according to wiki, but not default_config
    // = "dropitem" default_config
    //"f" = "+inspect"
    //"g" = "+taunt"
    //"h" = "+use_action_slot_item"
    //"i" = "showmapinfo"
    //"j" = "cl_trigger_first_notification"
    //"k" = "cl_decline_first_notification"
    //"l" = "dropitem"
    //"m" = "open_charinfo_direct"
    //"n" = "open_charinfo_backpack"
    //    "o"
    //"p" = "say_party" default_config
    //"q" = "lastinv"
    //"r" = "+reload"
    //"s" = "+back"
    //"t" = "impulse 201" // spray
    //"u" = "say_team"
    //"v" = "+voicerecord"
    //"w" = "+forward"
    //"x" = "voice_menu_2"
    //"y" = "say"
    //"z" = "voice_menu_1" // = "saveme" default_config
    //"`" = "toggleconsole"
    //"," = "changeclass"
    //"." = "changeteam"
    //"'" = "+moveup"
    //"/" = "+movedown"
    //"-" = "disguiseteam"
    //"SPACE" = "+jump"
    //"TAB" = "+showscores"
    //"ESCAPE" = "cancelselect"
    // = "escape" default_config
    //"ALT" = "+strafe" default_config
    //"INS" = "+klook" default_config
    // //"SEMICOLON" = "+mlook" default_config
    //"PAUSE" = "pause" default_config
    //"PGUP" = "+lookup" default_config
    //"PGDN" = "+lookdown" default_config
    //"END" = "centerview" default_config
    //"CTRL" = "+duck"
    //"LEFTARROW" = "+left"
    //"RIGHTARROW" = "+right"
    //"F1" = "+showroundinfo" ("does not exist currently")
    //"F2" = "show_quest_log"
    //"F3" = "show_matchmaking" ("does not exist currently")
    //"F4" = "player_ready_toggle"
    //"F5" = "screenshot"
    //"F6" = "save_replay"
    //"F7" = "abuse_report_queue"
    //"F10" = "quit prompt"
    //"F12" = "replay_togglereplaytips"
    //"MOUSE1" = "+attack"
    //"MOUSE2" = "+attack2"
    //"MOUSE3" = "+attack3"
    //"MWHEELUP" = "invprev"
    //"MWHEELDOWN" = "invnext"


    ////"" = "vr_toggle"
    //// "askconnect_accept"
    ////"" = "vr_reset_home_pos"
    ////"" = "say_party"
    ////"ALT" = "+strafe"

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

            Bind(Key, newCommand);

            EndCurrentCommand();
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
            Bind(Key, OriginalCommand);

            EndCurrentCommand();
            CurrentCommand = OriginalCommand;
        }
    }


}