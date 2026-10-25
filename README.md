# Doppel (decompiled)

Decompiled C# source of **Doppel 1.4.0** by **RUDYHILL** (Thunderstore: `Rudyhill_team-Doppel-1.4.0`),
an AI-bot code mod for BONELAB. All credit for the mod goes to its author.

- Source DLL: `Mods/Doppel.dll`, SHA-256 `7114be14755f4e6910fd7e1cc6687cff8db79fc6793ace77978e941d479e3b31`
- Decompiler: ILSpy (`ilspycmd` 8.2.0.7535), whole-project mode
- Mod deps: MelonLoader 0.7.3, BoneLib 3.1.3, optional LabFusion

The `[MelonInfo]` attribute inside the DLL still says version `1.3.0`, even though the package is 1.4.0.

## Layout

```
src/Doppel/
  Doppel.csproj
  Properties/AssemblyInfo.cs   MelonInfo / MelonGame attributes
  DoppelMod/Mod.cs             MelonMod entry: OnInitializeMelon, Update, FixedUpdate
  ThirdEyeBonelabMod/          original internal namespace
    AssetFiles.cs, Spawnables.cs
    Bots/                      all bot logic
```

Main pieces in `ThirdEyeBonelabMod/Bots`:

| File | What it does |
| --- | --- |
| `Entry.cs` | Builds the BoneMenu page and drives the per-frame update |
| `Bot.cs` | The bot itself (≈ half the codebase): states, combat, grabbing, climbing, driving hooks |
| `BotManager.cs`, `BotRig.cs` | Spawning and removing bots, and setting up the player rig |
| `Navigator.cs`, `PortalWays.cs` | Pathing and movement around the map |
| `Tactics.cs`, `Targets.cs`, `Race.cs`, `Orders.cs` | Team fighting, choosing targets, sides, commands |
| `Driving.cs`, `DriveSkill.cs`, `CarDoors.cs`, `Practice.cs` | Driving vehicles and self-practice |
| `GunMoves.cs`, `ShootSkill.cs`, `GunWeight.cs`, `GripBook.cs`, `HoldLibrary.cs` | Gun handling, reloads, punches, grips |
| `ClimbMoves.cs`, `WallGrab.cs` | Climbing |
| `Recorder.cs`, `MotionLibrary.cs`, `MoveSlots.cs`, `Lessons.cs`, `Lab.cs`, `SelfTaught.cs`, `Knowledge.cs`, `PlayerWatch.cs` | Recording the player and learning from it |
| `Clone.cs`, `ExactClone.cs` | Record and play back clones |
| `FusionLink.cs`, `NetBots.cs`, `DoppelMessage.cs`, `DoppelData.cs` | LabFusion multiplayer sync |

## Building

Building needs the game's interop assemblies, which MelonLoader generates on first launch. They are
not in this repo. Install MelonLoader 0.7.3, BoneLib and (for `FusionLink.cs`) LabFusion, run the game
once, then:

```
dotnet build src/Doppel/Doppel.csproj -c Release -p:BonelabPath="<path to BONELAB>"
```

The project was decompiled without those game assemblies. Before committing, I stripped ILSpy's
`//IL_xxxx: Unknown result type ...` comments, which only noted the missing references. If a spot
doesn't compile once the references are in place (usually a missing cast or an Il2Cpp enum
comparison), re-run ILSpy with `-r <BONELAB>/MelonLoader/Il2CppAssemblies` for a cleaner result.

The starter recordings that ship in the package's `UserData/Bot Player` folder (about 27 MB of data
files) are not included here.
