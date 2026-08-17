# BossAbilities - TShock Plugin (v2)

Kill bosses → unlock **real unique abilities**.  
Each ability can be toggled on/off.

## Real Abilities

| Boss                  | Ability            | Effect                                      |
|-----------------------|--------------------|---------------------------------------------|
| King Slime            | Super Jump         | Much higher jump + no fall damage           |
| Eye of Cthulhu        | Predator Vision    | Night vision + Hunter + Spelunker           |
| Eater of Worlds       | Life Force         | Strong life regeneration                    |
| Brain of Cthulhu      | Life Force         | Strong life regeneration                    |
| Queen Bee             | Bee Wings          | **True Flight** (no wings needed)           |
| Skeletron             | Bone Armor         | High defense + endurance                    |
| Deerclops             | Frost Shield       | Defense + cold resistance                   |
| Wall of Flesh         | Hellwalker         | Lava immunity + fire walk                   |
| Queen Slime           | Royal Speed        | Significantly increased movement speed      |
| Mechanical Bosses     | Overclock          | Damage + attack speed + regen               |
| Plantera              | Jungle Heart       | Very strong life regeneration               |
| Golem                 | Stone Skin         | Very high defense                           |
| Duke Fishron          | Ocean Lord         | Infinite breath + faster swimming           |
| Empress of Light      | Prismatic Power    | Damage + defense boost                      |
| Lunatic Cultist       | Arcane Mastery     | Magic power + mana regeneration             |
| Moon Lord             | Godslayer          | Strong damage + defense + regeneration      |

## Commands

### Player
- `/abilities` or `/ba` — Show your unlocked abilities
- `/abilities toggle <name>` — Turn an ability ON or OFF  
  (example: `/abilities toggle bee` or `/ba toggle godslayer`)

### Admin
- `/baadmin list <player>`
- `/baadmin give <player> <bossid>`
- `/baadmin reset <player>`

## Permissions
```
bossabilities.use
bossabilities.admin
```

```
/group addperm default bossabilities.use
/group addperm owner bossabilities.admin
```

## How to Build

1. Make sure paths in `BossAbilities.csproj` point to your TShock DLLs  
   (or just use the GitHub Actions workflow)

2. Build:
   ```bash
   dotnet build -c Release
   ```

3. Put `BossAbilities.dll` into `ServerPlugins` folder and restart the server.

## Notes
- Data is saved in `tshock/BossAbilities.json`
- Compatible with modern TShock (API 2.1 / .NET 9)
