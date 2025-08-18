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
            CurrentCommand = newCommand;
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
            CurrentCommand = OriginalCommand;
        }
    }


}