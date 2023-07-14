# Trader Utils

It makes it very easy to add merchants to the game and rewrite existing trades. <br>
Automatically generates the configuration (can be turned off). <br>
Wide functionality and features that are not available in the vanilla game. <br>

### Merging the DLLs into your mod

Download the TraderUtils.dll and the ServerSync.dll from the release section to the right.
Including the DLLs is best done via ILRepack (https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task). You can load
this package (ILRepack.Lib.MSBuild.Task) from NuGet.

If you have installed ILRepack via NuGet, simply create a file named `ILRepack.targets` in your project and copy the
following content into the file

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Target Name="ILRepacker" AfterTargets="Build">
        <ItemGroup>
            <InputAssemblies Include="$(TargetPath)"/>
            <InputAssemblies Include="$(OutputPath)\TraderUtils.dll"/>
        </ItemGroup>
        <ILRepack Parallel="true" DebugInfo="true" Internalize="true" InputAssemblies="@(InputAssemblies)"
                  OutputFile="$(TargetPath)" TargetKind="SameAsPrimaryAssembly" LibraryPath="$(OutputPath)"/>
    </Target>
</Project>
```

Make sure to set the TraderUtils.dll in your project to "Copy to output directory" in the properties of the DLLs and to
add
a reference to it.
After that, simply add `using TraderUtils;`.
Then initialize TraderUtils by this line of code `TradesConfiguration.Initialize(Config, configSync);`.

## Example project

This code creates 3 new trades for Haldor, 2 to Hildir and 3 to TheFarmer by Marlthon.
To create new trade you need to create new CustomTrade and set its values.

1. How to set for what trader this trade related? Use `.SetTrader("TraderPrefabName")`
2. How to set what item is selling? Use `.SetItem("ItemPrefabName")`
3. How to set for what price that item is selling? Use `.SetPrice(some number)`
4. How to set how much is selling? Use `.SetStack(some number)`
5. How to set what item should player spend to buy this item? Use `.SetMoneyItem("ItemPrefabName")`
6. How to that world should have some key to buy this item?
   Use `.SetRequiredGlobalKey("KeyName")` [More about global keys](#about-global-keys).
7. How to prevent player from configuring this trade in the configuration? Use `.SetConfigurable(true / false)` **By
   default all trades are configurable**
8. How to hide this trade, but it can be enabled from the configuration? Use `.SetEnabled((true / false)`

```csharp
using BepInEx;
using TraderUtils;

namespace TestTrader
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class TraderPlugin : BaseUnityPlugin
    {
        internal const string ModName = "TestTrader", ModVersion = "1.0.0", ModGUID = "com.Frogger." + ModName;
        internal static TraderPlugin _self;

        private void Awake()
        {
            new CustomTrade()
                .SetTrader("Haldor")
                .SetItem("Carrot")
                .SetPrice(25)
                .SetStack(5);
            new CustomTrade()
                .SetTrader("Haldor")
                .SetItem("FishingRod")
                .SetPrice(100)
                .SetStack(1);
            new CustomTrade()
                .SetTrader("Haldor")
                .SetItem("Onion")
                .SetPrice(25)
                .SetStack(5);
                
                new CustomTrade()
                .SetTrader("Hildir")
                .SetItem("Coins")
                .SetPrice(5)
                .SetStack(25)
                .SetMoneyItem("Onion");
            new CustomTrade()
                .SetTrader("Hildir")
                .SetItem("Coins")
                .SetPrice(10)
                .SetStack(2)
                .SetMoneyItem("Carrot");
          
                
            new CustomTrade()
                .SetTrader("TheFarmer")
                .SetItem("AxeBlackMetal")
                .SetPrice(1000)
                .SetStack(1)
                .SetConfigurable(false);
            new CustomTrade()
                .SetTrader("TheFarmer")
                .SetItem("SledgeStagbreaker")
                .SetPrice(3)
                .SetStack(1)
                .SetMoneyItem("TrophyDeer")
                .SetConfigurable(true);
            new CustomTrade()
                .SetTrader("TheFarmer")
                .SetItem("TrophyDragonQueen")
                .SetPrice(2)
                .SetStack(1)
                .SetMoneyItem("TrophyEikthyr");
        }
    }
}
```

### About global keys

Global keys are the parameters of the world that are changed during the passage. Mods can also add their own keys. Here
is a list of all vanilla keys:

* PlayerDamage
* EnemyDamage
* WorldLevel
* EventRate
* ResourceRate
* StaminaRate
* MoveStaminaRate
* StaminaRegenRate
* SkillGainRate
* SkillReductionRate
* EnemySpeedSize
* PlayerEvents
* Fire
* DeathKeepEquip
* DeathDeleteItems
* DeathDeleteUnequipped
* DeathSkillsReset
* NoBuildCost
* NoCraftCost
* AllPiecesUnlocked
* NoWorkbench
* AllRecipesUnlocked
* WorldLevelLockedTools
* PassiveMobs
* NoMap
* NoPortals
* NoBossPortals
* DungeonBuild
* TeleportAll
* Preset
* NonServerOption
* defeated_eikthyr
* defeated_dragon
* defeated_goblinking
* defeated_gdking
* defeated_bonemass
* activeBosses
* KilledTroll
* killed_surtling
* KilledBat
* Count