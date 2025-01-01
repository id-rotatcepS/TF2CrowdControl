using CrowdControl.Common;

using JetBrains.Annotations;

using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs;

[UsedImplicitly]
public class MegaMan2 : NESEffectPack
{
    public MegaMan2(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler) { }

    private const ushort ADDR_AREA = 0x002A;
    private const ushort ADDR_MOVEMENT = 0x002C; //what movement MegaMan is doing 03 standing still
    private const ushort ADDR_PHYSICS = 0x003D;
    private const ushort ADDR_IFRAMES = 0x004B;
    private const ushort ADDR_WEAPONS = 0x009A;
    private const ushort ADDR_ITEMS = 0x009B;
    private const ushort ADDR_ENERGY_HEAT = 0x009C;
    private const ushort ADDR_ENERGY_AIR = 0x009D;
    private const ushort ADDR_ENERGY_WOOD = 0x009E;
    private const ushort ADDR_ENERGY_BUBBLE = 0x009F;
    private const ushort ADDR_ENERGY_QUICK = 0x00A0;
    private const ushort ADDR_ENERGY_FLASH = 0x00A1;
    private const ushort ADDR_ENERGY_METAL = 0x00A2;
    private const ushort ADDR_ENERGY_CRASH = 0x00A3;
    private const ushort ADDR_ENERGY_ITEM1 = 0x00A4;
    private const ushort ADDR_ENERGY_ITEM2 = 0x00A5;
    private const ushort ADDR_ENERGY_ITEM3 = 0x00A6;
    private const ushort ADDR_ETANKS = 0x00A7;
    private const ushort ADDR_LIVES = 0x00A8;
    private const ushort ADDR_POWER = 0x00A9; //0E is funny
    private const ushort ADDR_FREEZE = 0x00AA; //freeze time
    private const ushort ADDR_COLORS = 0x00F8; //default 1E, Gameboy 1F
    private const ushort ADDR_HP = 0x06C0;
    private const ushort ADDR_BOSS_HP = 0x06C1;
    private const ushort ADDR_ENEMY_HP = 0xD78E;
    private const ushort ADDR_SFX = 0x0580;
    private const ushort ADDR_SFX_ENABLE = 0x0066;
    private const ushort ADDR_FLIP_PLAYER = 0x8904;
    private const ushort ADDR_JUMP_HEIGHT = 0x003C;
    private const ushort ADDR_PALETTE1 = 0x0356;
    private const ushort ADDR_PALETTE2 = 0x0357;
    private const ushort ADDR_PALETTE3 = 0x0358;
    private const ushort ADDR_HERO_COLOR_OUTLINE = 0x0367;
    private const ushort ADDR_HERO_COLOR_LIGHT = 0x0368;
    private const ushort ADDR_HERO_COLOR_DARK = 0x0369;

    private const ushort ADDR_GAMEPLAY_MODE = 0x1FE;

    private const ushort ADDR_UNKNOWN1 = 0x0069;//should be 0x0E

    private static readonly Dictionary<string, (string weapon, string bossName, byte value, BossDefeated bossFlag, SuitColor light, SuitColor dark, ushort address, byte limit)> _wType = new(StringComparer.InvariantCultureIgnoreCase)
    {
        {"buster", ("Mega Buster", "Mega Man", 0, 0, SuitColor.DefaultLight, SuitColor.DefaultDark, 0, 0)},
        {"fire", ("Atomic Fire", "Heat Man", 1, BossDefeated.HeatMan, SuitColor.HLight, SuitColor.HDark, ADDR_ENERGY_HEAT, 14)},
        {"air", ("Air Shooter", "Air Man", 2, BossDefeated.AirMan, SuitColor.ALight, SuitColor.ADark, ADDR_ENERGY_AIR, 14)},
        {"leaf", ("Leaf Shield", "Wood Man", 3, BossDefeated.WoodMan, SuitColor.WLight, SuitColor.WDark, ADDR_ENERGY_WOOD, 14)},
        {"bubble", ("Bubble Lead", "Bubble Man", 4, BossDefeated.BubbleMan, SuitColor.BLight, SuitColor.BDark, ADDR_ENERGY_BUBBLE, 14)},
        {"quick", ("Quick Boomerang", "Quick Man", 5, BossDefeated.QuickMan, SuitColor.QLight, SuitColor.QDark, ADDR_ENERGY_QUICK, 14)},
        {"time", ("Time Stopper", "Flash Man", 6, BossDefeated.FlashMan, SuitColor.FLight, SuitColor.FDark, ADDR_ENERGY_FLASH, 14)},
        {"metal", ("Metal Blade", "Metal Man", 7, BossDefeated.MetalMan, SuitColor.MLight, SuitColor.MDark, ADDR_ENERGY_METAL, 14)},
        {"crash", ("Crash Bomber", "Crash Man", 8, BossDefeated.CrashMan, SuitColor.CLight, SuitColor.CDark, ADDR_ENERGY_CRASH, 14)},
        {"item1", ("Item 1", "Heat Man", 9,BossDefeated.HeatMan, SuitColor.ItemLight, SuitColor.ItemDark, ADDR_ENERGY_ITEM1, 14)},
        {"item2", ("Item 2", "Air Man", 10,BossDefeated.AirMan, SuitColor.ItemLight, SuitColor.ItemDark, ADDR_ENERGY_ITEM2, 14)},
        {"item3", ("Item 3", "Flash Man", 11,BossDefeated.FlashMan, SuitColor.ItemLight, SuitColor.ItemDark, ADDR_ENERGY_ITEM3, 14)}
    };

    private static readonly Dictionary<byte, string> _aInfo = new()
    {
        {0x00, "Heat Man"},
        {0x01, "Air Man"},
        {0x02, "Wood Man"},
        {0x03, "Bubble Man"},
        {0x04, "Quick Man"},
        {0x05, "Flash Man"},
        {0x06, "Metal Man"},
        {0x07, "Crash Man"},
        {0x08, "the boss"},
        {0x09, "the boss"},
        {0x0A, "the boss"}
    };

    private enum SuitColor : byte
    {
        Black = 0x0F,
        DefaultLight = 0x2C,
        DefaultDark = 0x11,
        HLight = 0x28,
        HDark = 0x15,
        ALight = 0x30,
        ADark = 0x11,
        WLight = 0x30,
        WDark = 0x19,
        BLight = 0x30,
        BDark = 0x00,
        QLight = 0x34,
        QDark = 0x25,
        FLight = 0x34,
        FDark = 0x14,
        MLight = 0x37,
        MDark = 0x18,
        CLight = 0x30,
        CDark = 0x26,
        ItemLight = 0x30,
        ItemDark = 0x16,
    }

    [Flags]
    private enum BossDefeated : byte
    {
        HeatMan = 0x01,
        AirMan = 0x02,
        WoodMan = 0x04,
        BubbleMan = 0x08,
        QuickMan = 0x10,
        FlashMan = 0x20,
        MetalMan = 0x40,
        CrashMan = 0x80,
        All = 0xFF
    }

    private enum ItemType : byte
    {
        Item1 = 0x01,
        Item2 = 0x02,
        Item3 = 0x04
    }

    public override EffectList Effects
    {
        get
        {
            List<Effect> effects =
            [
                new("Give Lives", "lives") { Quantity = 9 },
                new("Give E-Tanks", "etank"),
                new("Boss E-Tank", "bosshpfull"),
                new("Refill Health", "hpfull"),

                new("Grant Invulnerability", "iframes") { Duration = TimeSpan.FromSeconds(15) },
                new("Freeze Time", "timefreeze")
                {
                    Description = "Freezes the game like Quick Man does but you control it!",
                    Duration = TimeSpan.FromSeconds(15)
                },
                new("Can't stop Moving Man", "moveman")
                {
                    Description = "Causes Mega Man to uncontrollably move in whatever direction he is looking.",
                    Duration = TimeSpan.FromSeconds(15)
                },
                new("Game Boy Mode", "gameboy")
                {
                    Description = "Cause the game to look like it's on a Game Boy!", Duration = TimeSpan.FromSeconds(15)
                },
                new("Moonwalk", "moonwalk") { Duration = TimeSpan.FromSeconds(30) },
                new("Magnet Floors", "magfloors") { Duration = TimeSpan.FromSeconds(30) },
                new("One-Hit KO", "ohko") { Duration = TimeSpan.FromSeconds(15) }
            ];

            return effects;
        }
    }

    public override ROMTable ROMTable
    {
        get
        {
            return new[]
            {
                new ROMInfo("Mega Man 2", null, Patching.Ignore, ROMStatus.ValidPatched, s => Patching.MD5(s, "caaeb9ee3b52839de261fd16f93103e6")),
                new ROMInfo("Mega Man 2", null, Patching.Ignore, ROMStatus.ValidPatched, s => Patching.MD5(s, "8e4bc5b03ffbd4ef91400e92e50dd294")),
                new ROMInfo("Rockman 2 - Dr. Wily no Nazo", null, Patching.Ignore, ROMStatus.ValidPatched, s => Patching.MD5(s, "055fb8dc626fb1fbadc0a193010a3e3f")),
                new ROMInfo("Mega Man 2 Randomizer", null, Patching.Ignore, ROMStatus.ValidPatched, s => s.Length == 262160)
            };
        }
    }

    private enum SFXType : byte
    {
        TimeStop = 0x21,
        Explosion = 0x22,
        MetalBlade = 0x23,
        BusterShot = 0x24,
        EnemyShot = 0x25,
        Damage = 0x26,
        QuickmanBeam = 0x27,
        HPIncrement = 0x28,
        JumpLanding = 0x29,
        DamageEnemy = 0x2B,
        CrashBomb = 0x2E,
        Teleport = 0x30,
        LeafShield = 0x31,
        Menu = 0x32,
        Doors = 0x34,
        Heat1 = 0x35,
        Heat2 = 0x36,
        Heat3 = 0x37,
        AtomicFire = 0x38,
        AirShooter = 0x3F,
        Death = 0x41,
        Item = 0x42,
        Doors2 = 0xFE
    }

    public override Game Game { get; } = new("Mega Man 2", "MegaMan2", "NES", ConnectorType.NESConnector);

    protected override bool IsReady(EffectRequest request)
        => Connector.Read8(0x00b1, out byte b) && (b < 0x80);

    protected override void StartEffect(EffectRequest request)
    {
        if (!IsReady(request))
        {
            DelayEffect(request, TimeSpan.FromSeconds(5));
            return;
        }

        string[] codeParams = FinalCode(request).Split('_');
        switch (codeParams[0])
        {
            case "ohko":
                {
                    byte origHP = 0;
                    var s = RepeatAction(request,
                        () => Connector.Read8(ADDR_HP, out origHP) && (origHP > 1),
                        () => Connector.SendMessage($"{request.DisplayViewer} disabled your structural shielding."), TimeSpan.FromSeconds(1),
                        () => Connector.Read8(ADDR_GAMEPLAY_MODE, out byte mode) && mode == 0xB2, TimeSpan.FromSeconds(1),
                        () => Connector.Write8(ADDR_HP, 0x00), TimeSpan.FromSeconds(1), true, "health");
                    s.WhenCompleted.Then(_ =>
                    {
                        Connector.Write8(ADDR_HP, origHP);
                        Connector.SendMessage("Your shielding has been restored.");
                    });
                    return;
                }
            case "lives":
                {
                    if (!byte.TryParse(codeParams[1], out byte lives))
                    {
                        Respond(request, EffectStatus.FailTemporary, "Invalid life quantity.");
                        return;
                    }
                    TryEffect(request,
                        () => Connector.RangeAdd8(ADDR_LIVES, lives, 0, 9, false),
                        () => true,
                        () =>
                        {
                            Connector.SendMessage($"{request.DisplayViewer} sent you {lives} live(s).");
                            PlaySFX(SFXType.Item);
                        });
                    return;
                }
            case "etank":
                TryEffect(request,
                    () => Connector.RangeAdd8(ADDR_ETANKS, 1, 0, 4, false),
                    () => true,
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent you an E-Tank.");
                        PlaySFX(SFXType.Item);
                    });
                return;
            case "hpfull":
                TryEffect(request,
                    () => Connector.Read8(ADDR_HP, out byte b) && (b < 14),
                    () => Connector.Write8(ADDR_HP, 28),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} refilled your health.");
                        PlaySFX(SFXType.HPIncrement);
                    }, TimeSpan.FromSeconds(1));
                return;
            case "bosshpfull":
                {
                    if (!Connector.Read8(ADDR_AREA, out byte area))
                    {
                        DelayEffect(request);
                        return;
                    }
                    if ((area == 0x09) || (area == 0x0B))
                    {
                        DelayEffect(request, TimeSpan.FromSeconds(30));
                        return;
                    }
                    TryEffect(request,
                        () => Connector.Read8(ADDR_BOSS_HP, out byte hp) && (hp != 0) && (hp <= 14),
                        () => Connector.Write8(ADDR_BOSS_HP, 28),
                        () =>
                        {
                            Connector.SendMessage($"{request.DisplayViewer} refilled {TryGetBossName()}'s health.");
                            PlaySFX(SFXType.HPIncrement);
                        }, TimeSpan.FromSeconds(1));
                    return;
                }
            case "iframes":
                var iframes = RepeatAction(request,
                    () => Connector.IsZero8(ADDR_IFRAMES),
                    () => Connector.Write8(ADDR_IFRAMES, 0xFF) && Connector.SendMessage($"{request.DisplayViewer} deployed an invulnerability field."), TimeSpan.FromSeconds(0.5),
                    () => Connector.Read8(ADDR_GAMEPLAY_MODE, out byte mode) && mode == 0xB2, TimeSpan.FromSeconds(5),
                    () => Connector.Write8(ADDR_IFRAMES, 0xFF), TimeSpan.FromSeconds(0.5), true);
                iframes.WhenCompleted.Then(_ =>
                {
                    Connector.Write8(ADDR_IFRAMES, 0x01);
                    Connector.SendMessage($"{request.DisplayViewer}'s invulnerability field has dispersed.");
                });
                return;
            case "moveman":
                {
                    var moveman = RepeatAction(request,
                        () => Connector.Read8(ADDR_MOVEMENT, out byte b) && (b == 0x03),
                        () => Connector.Write8(ADDR_MOVEMENT, 0x05) && Connector.SendMessage($"{request.DisplayViewer} forced Mega Man to keep moving."), TimeSpan.FromSeconds(0.01),
                        () => Connector.Read8(ADDR_GAMEPLAY_MODE, out byte mode) && mode == 0xB2, TimeSpan.FromSeconds(5),
                        () => Connector.Write8(ADDR_MOVEMENT, 0x05), TimeSpan.FromSeconds(0.01), true);
                    moveman.WhenCompleted.Then(_ =>
                    {
                        Connector.Write8(ADDR_MOVEMENT, 0x03);
                        Connector.SendMessage($"{request.DisplayViewer}'s movement effect has ended.");
                    });
                    return;
                }
            case "gameboy":
                {
                    var gameboy = RepeatAction(request,
                        () => Connector.Read8(ADDR_COLORS, out byte b) && (b == 0x1E),
                        () => Connector.Write8(ADDR_COLORS, 0x1F) && Connector.SendMessage($"{request.DisplayViewer} enabled Game Boy mode."), TimeSpan.FromSeconds(0.5),
                        () => Connector.Read8(ADDR_GAMEPLAY_MODE, out byte mode) && mode == 0xB2, TimeSpan.FromSeconds(5),
                        () => Connector.Write8(ADDR_COLORS, 0x1F), TimeSpan.FromSeconds(0.5), true);
                    gameboy.WhenCompleted.Then(_ =>
                    {
                        Connector.Write8(ADDR_COLORS, 0x1E);
                        Connector.SendMessage($"{request.DisplayViewer}'s Game Boy mode has ended.");
                    });
                    return;
                }
            case "timefreeze":
                {
                    var timefreeze = RepeatAction(request,
                        () => Connector.Read8(ADDR_FREEZE, out byte b) && (b == 0x00),
                        () => Connector.Write8(ADDR_FREEZE, 0x01) && Connector.SendMessage($"{request.DisplayViewer} has frozen time."), TimeSpan.FromSeconds(0.01),
                        () => Connector.Read8(ADDR_GAMEPLAY_MODE, out byte mode) && mode == 0xB2, TimeSpan.FromSeconds(5),
                        () => Connector.Write8(ADDR_FREEZE, 0x01), TimeSpan.FromSeconds(0.01), true);
                    timefreeze.WhenCompleted.Then(_ =>
                    {
                        Connector.Write8(ADDR_FREEZE, 0x00);
                        Connector.SendMessage($"{request.DisplayViewer}'s time freeze hasended.");
                    });
                    return;
                }
            case "moonwalk":
                var moonwalk = RepeatAction(request,
                    () => Connector.Read8(0x8904, out byte b) && (b != 0x49),
                    () => Connector.SendMessage("${request.DisplayViewer} inverted your left/right."), TimeSpan.FromSeconds(1),
                    () => Connector.Read8(ADDR_GAMEPLAY_MODE, out byte mode) && mode == 0xB2, TimeSpan.FromSeconds(1),
                    () => Connector.Write8(0x8904, 0x49), TimeSpan.FromSeconds(1), true);
                moonwalk.WhenCompleted.Then(_ =>
                {
                    Connector.Write8(0x8904, 0x29);
                    Connector.SendMessage($"{request.DisplayViewer}'s control inversion has ended.");
                });
                return;
            case "magfloors":
                var magfloors = RepeatAction(request,
                    () => Connector.Read8(0xd3c8, out byte b) && (b != 0x03),
                    () => Connector.SendMessage($"{request.DisplayViewer} has magnetized the floors."), TimeSpan.FromSeconds(1),
                    () => Connector.Read8(ADDR_GAMEPLAY_MODE, out byte mode) && mode == 0xB2, TimeSpan.FromSeconds(1),
                    () => Connector.Write8(0xd3c8, 0x03), TimeSpan.FromSeconds(1), true);
                magfloors.WhenCompleted.Then(_ =>
                {
                    Connector.Write8(0xd3c8, 0x00);
                    Connector.SendMessage($"{request.DisplayViewer}'s magnetic field has ended.");
                });
                return;
        }
    }

    private string TryGetBossName()
    {
        try { return Connector.Read8(ADDR_AREA, out byte b) ? _aInfo[b] : "the boss"; }
        catch { return "the boss"; }
    }

    protected override bool StopEffect(EffectRequest request)
    {
        string[] codeParams = FinalCode(request).Split('_');

        switch (codeParams[0])
        {
            case "revive":
                {
                    var wType = _wType[codeParams[1]];
                    Connector.SendMessage($"{wType.weapon} is back online.");
                    return Connector.SetBits(ADDR_WEAPONS, (byte)wType.bossFlag, out _);
                }
            default:
                return base.StopEffect(request);
        }
    }

    public override bool StopAllEffects()
    {
        bool success = base.StopAllEffects();
        try
        {
            success &= Connector.Write8(0x8904, 0x29);
            success &= Connector.Write8(0xd3c8, 0x00);
        }
        catch { success = false; }
        return success;
    }

    private bool PlaySFX(SFXType type) =>
        Connector.Write8(ADDR_SFX, (byte)type) &&
        Connector.Write8(ADDR_SFX_ENABLE, 1);
}