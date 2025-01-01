using ConnectorLib.SimpleTCP;

using CrowdControl.Common;

//using JetBrains.Annotations;

using System;

namespace CrowdControl.Games.Packs;

//[UsedImplicitly]
public class TF2SpectatorGamePack : SimpleTCPPack<SimpleWebsocketServerConnector>
{
    public TF2SpectatorGamePack(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler)
        : base(player, responseHandler, statusUpdateHandler) { }

    public override EffectList Effects
        => new Effect[]{
                new("Black & White", "blackandwhite"){
                    Description = "TF2 in the 50s",
                    Duration = TimeSpan.FromSeconds(120),
                },
            //new("Give Lives", "lives") { Quantity = 9 },
            //new("Moonwalk", "moonwalk") { Duration = TimeSpan.FromSeconds(30) },
        };

    public override Game Game { get; } = new(name: "Team Fortress 2", id: "tf2spectator", path: "path?", connector: ConnectorType.SimpleTCPServerConnector);

    protected override bool IsReady(EffectRequest? request)
        //TODO
        => true;

}