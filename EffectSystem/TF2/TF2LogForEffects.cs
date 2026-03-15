using System.Text.RegularExpressions;
using TF2FrameworkInterface;

namespace EffectSystem.TF2
{

    // change class causes class.cfg to execute.  Doesn't execute until spawn.
    // death can be detected by log, but respawn can't.
    //
    // on death: change class and set an alias to change back on spawn
    //   alias respawnclass "alias respawnclass ; joinclass {currentclass}"; joinclass {diffclass};
    // all class config files - fire alias to set proper class and unset alias.
    //   setinfo currentclass "diffclass";respawnclass;
    // HOWEVER Engineer is not an option for that hack - it would destroy his buildings.
    // THEREFORE we have to rely on backup timer of perhaps 30 seconds.


    /// <summary>
    /// Adds <see cref="OnUserChangedClass"/> 
    /// powered by <see cref="CustomClassChangeMatcher"/> 
    /// which depends on custom class cfg files.
    /// </summary>
    public class TF2LogForEffects : TF2LogOutput
    {
        public static bool IsLogReadingActive => TF2Effects.Instance?.TF2Proxy?.IsReading ?? false;

        public TF2LogForEffects(string tf2Path)
            : base(tf2Path)
        {

            LineMatchers.AddRange([
                new CustomClassChangeMatcher(this),
            ]);

        }

        /// <summary>
        /// Fires when the main player spawns as a different class than before (or their first spawn in the new game).
        /// This means the player has respawned with a class change. Respawns without a class change do not fire this.
        /// </summary>
        public event UserChangedClass OnUserChangedClass;
        internal void NotifyUserClassChanged(string playerClass)
        {
            OnUserChangedClass.Invoke(playerClass);
        }

        public override void Error(string v)
        {
            ASPEN.Aspen.Log.Error(v);
        }

        public override void ErrorException(Exception ex, string v)
        {
            ASPEN.Aspen.Log.ErrorException(ex, v);
        }
        //TODO  get ASPEN down into framework package?
        public override void Warning(string v)
        {
            ASPEN.Aspen.Log.Warning(v);
        }

        public override void WarningException(Exception ex, string v)
        {
            ASPEN.Aspen.Log.WarningException(ex, v);
        }

        public override void Info(string v)
        {
            ASPEN.Aspen.Log.Info(v);
        }
    }

    /// <summary>
    /// Fires when the player spawns as the different class.
    /// Depends on custom installations in the differnet class cfg files that output this info.
    /// </summary>
    public class CustomClassChangeMatcher : LineMatcher
    {
        //TODO tie the configure and regex strings together explicitly
        // e.g. __class-engineer__ 
        private static readonly Regex defendedRegex = new Regex(@"^\s*__class-(?<class>scout|soldier|pyro|demoman|engineer|heavyweapons|medic|sniper|spy)__\s*$");

        public CustomClassChangeMatcher(TF2LogForEffects tF2LogOutput)
            : base(tF2LogOutput, defendedRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            base.Handle(match, line);

            string playerClass = match.Groups["class"].Value;
            (TF2LogOutput as TF2LogForEffects)?.NotifyUserClassChanged(playerClass);
        }
    }
}
