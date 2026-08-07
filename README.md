# TF2CrowdControl

## What is this?
_TF2Spectator for Crowd Control_ connects [Crowd Control effects](https://crowdcontrol.live/games/teamfortress2) to your Team Fortress 2 game.

1. Install Crowd Control
2. Set it up by following the [TF2 Game Guide](https://crowdcontrol.live/guides/TF2Spectator)
   * _Install this app, configure the app, configure your TF2 launch options_
4. Start all 3 programs: Crowd Control, this app, and TF2

Now your viewers can spend coins to mess with your game.

## Never used Crowd Control on Twitch?
Following [my guide for a better setup](https://docs.google.com/presentation/d/e/2PACX-1vT5WFJXYYx4IYQvzXxTAJtRdZABK9BrZCxYQSd5ZSO3cv11DqvxgSRhFCYnVB0QWZH7kqOpSuT_7hUi/pub) than the CC Twitch defaults.

## Does it work in Casual? MvM? Community Servers?
Yes.  

It just runs (non-cheat) console commands to impact you (not the server or other players) and everything has been tested on official Valve Casual servers.

_Some community servers could block certain commands, or consider some effects "spam" and kick you out._

_Some effects may make your teammates look at you funny and choose to call a kick on you_ - but that's part of the fun!

## Never played TF2?
It's [free](https://store.steampowered.com/app/440/Team_Fortress_2/), it's [fun](https://www.youtube.com/playlist?list=PLNfV0fCcLgRrQ5e1ESSG2MGwiMjVUnND2), it's a little addictive.

It's also old with some bad defaults.  I recommend doing this as soon as you launch the game:

1. `Options > Keyboard > Advanced > ✅ Enable Console` 

2. Then press the \` key (the key below ESC) to show the console.

3. Then paste this in console for better default options:
```
hud_fastswitch 1; tf_dingalingaling 1; tf_sniper_fullcharge_bell 1; tf_medigun_autoheal 1; hud_medichealtargetmarker 1; hud_medicautocallers 1; tf_colorblindassist 1; fov_desired 90; m_rawinput 1;
```
> NOTE: you should do the above BEFORE using this app.
> 
> (Some Crowd Control effects mess with your settings.  By design, this app will undo all changes that happened while it was in use)
