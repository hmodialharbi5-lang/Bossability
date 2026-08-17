using System.Reflection;
using System.Text.Json;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace BossAbilities;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    public override string Name => "BossAbilities";
    public override string Author => "Grok";
    public override string Description => "Kill bosses → unlock real unique abilities (toggleable)";
    public override Version Version => new(2, 0, 0);

    private static readonly string DataPath = Path.Combine(TShock.SavePath, "BossAbilities.json");

    // AccountName → (BossID → isEnabled)
    private Dictionary<string, Dictionary<int, bool>> playerData = new();

    private System.Timers.Timer? abilityTimer;

    // Boss ID → Ability Name
    private readonly Dictionary<int, string> BossNames = new()
    {
        { 50,  "Super Jump" },
        { 4,   "Predator Vision" },
        { 13,  "Life Force" },
        { 266, "Life Force" },
        { 222, "Bee Wings" },
        { 35,  "Bone Armor" },
        { 668, "Frost Shield" },
        { 113, "Hellwalker" },
        { 657, "Royal Speed" },
        { 125, "Overclock" },
        { 126, "Overclock" },
        { 134, "Overclock" },
        { 127, "Overclock" },
        { 262, "Jungle Heart" },
        { 245, "Stone Skin" },
        { 370, "Ocean Lord" },
        { 636, "Prismatic Power" },
        { 439, "Arcane Mastery" },
        { 398, "Godslayer" }
    };

    public Plugin(Main game) : base(game) { }

    public override void Initialize()
    {
        LoadData();

        ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        GeneralHooks.ReloadEvent += OnReload;

        // Apply abilities every 3 seconds (for buffs & stats that need refresh)
        abilityTimer = new System.Timers.Timer(3000);
        abilityTimer.Elapsed += (s, e) => ApplyPeriodicEffects();
        abilityTimer.AutoReset = true;
        abilityTimer.Start();

        Commands.ChatCommands.Add(new Command("bossabilities.use", AbilitiesCommand, "abilities", "bossabilities", "ba")
        {
            HelpText = "View or toggle your boss abilities"
        });

        Commands.ChatCommands.Add(new Command("bossabilities.admin", AdminCommand, "baadmin")
        {
            HelpText = "Admin tools for BossAbilities"
        });

        TShock.Log.ConsoleInfo("[BossAbilities] Loaded! Kill bosses to unlock real powers.");
    }

    private void OnNpcKilled(NpcKilledEventArgs args)
    {
        var npc = args.npc;
        if (npc == null || !npc.active) return;

        int type = npc.netID;
        int bossId = type;

        // Normalize multi-part bosses
        if (type is 13 or 14 or 15) bossId = 13;          // Eater of Worlds
        if (type is 266 or 267) bossId = 266;             // Brain of Cthulhu
        if (type is 125 or 126) bossId = 125;             // Twins
        if (type is 134 or 135 or 136) bossId = 134;      // Destroyer
        if (type is 127 or 128 or 129 or 130 or 131) bossId = 127; // Skeletron Prime
        if (type is 398 or 397 or 396) bossId = 398;      // Moon Lord

        if (!BossNames.ContainsKey(bossId)) return;

        foreach (var player in TShock.Players)
        {
            if (player == null || !player.Active || !player.IsLoggedIn) continue;

            if (player.TPlayer.Distance(npc.Center) < 1600f) // ~100 tiles
            {
                UnlockAbility(player, bossId);
            }
        }
    }

    private void UnlockAbility(TSPlayer player, int bossId)
    {
        string key = player.Account.Name;

        if (!playerData.ContainsKey(key))
            playerData[key] = new Dictionary<int, bool>();

        if (!playerData[key].ContainsKey(bossId))
        {
            playerData[key][bossId] = true; // Enabled by default
            string name = BossNames[bossId];
            player.SendSuccessMessage($"You unlocked the ability: [c/FFD700:{name}]!");
            TSPlayer.All.SendInfoMessage($"{player.Name} unlocked [c/FFD700:{name}]!");
            SaveData();
        }
    }

    private bool HasAbility(TSPlayer player, int bossId)
    {
        string key = player.Account.Name;
        return playerData.TryGetValue(key, out var abilities) &&
               abilities.TryGetValue(bossId, out bool enabled) && enabled;
    }

    private void OnGameUpdate(EventArgs args)
    {
        foreach (var player in TShock.Players)
        {
            if (player == null || !player.Active || !player.IsLoggedIn) continue;

            var tp = player.TPlayer;

            // === Bee Wings (Queen Bee) - True Flight ===
            if (HasAbility(player, 222))
            {
                tp.wingTime = 9999;
                tp.wingTimeMax = 9999;
                tp.wings = 22; // Bee wings visual
                tp.wingsLogic = 22;
            }

            // === Super Jump (King Slime) ===
            if (HasAbility(player, 50))
            {
                tp.jumpSpeedBoost += 2.5f;
                tp.noFallDmg = true;
            }

            // === Royal Speed (Queen Slime) ===
            if (HasAbility(player, 657))
            {
                tp.moveSpeed += 0.4f;
            }

            // === Ocean Lord (Duke Fishron) ===
            if (HasAbility(player, 370))
            {
                tp.gills = true;
                tp.accMerman = true;
                if (tp.wet)
                {
                    tp.moveSpeed += 0.5f;
                }
            }

            // === Hellwalker (Wall of Flesh) - Lava Immunity ===
            if (HasAbility(player, 113))
            {
                tp.lavaImmune = true;
                tp.fireWalk = true;
            }
        }
    }

    private void ApplyPeriodicEffects()
    {
        foreach (var player in TShock.Players)
        {
            if (player == null || !player.Active || !player.IsLoggedIn) continue;

            // Predator Vision (Eye of Cthulhu)
            if (HasAbility(player, 4))
            {
                player.SetBuff(12, 360, true);  // Night Owl
                player.SetBuff(17, 360, true);  // Hunter
                player.SetBuff(16, 360, true);  // Spelunker
            }

            // Life Force (Eater / Brain)
            if (HasAbility(player, 13) || HasAbility(player, 266))
            {
                player.SetBuff(2, 360, true);   // Regeneration
                player.SetBuff(113, 360, true); // Lifeforce
            }

            // Bone Armor (Skeletron)
            if (HasAbility(player, 35))
            {
                player.SetBuff(5, 360, true);   // Ironskin
                player.SetBuff(114, 360, true); // Endurance
            }

            // Frost Shield (Deerclops)
            if (HasAbility(player, 668))
            {
                player.SetBuff(5, 360, true);   // Ironskin
                player.SetBuff(47, 360, true);  // Warmth
            }

            // Overclock (Mechanical Bosses)
            if (HasAbility(player, 125) || HasAbility(player, 134) || HasAbility(player, 127))
            {
                player.SetBuff(115, 360, true); // Wrath
                player.SetBuff(117, 360, true); // Rage
                player.SetBuff(2, 360, true);   // Regeneration
            }

            // Jungle Heart (Plantera)
            if (HasAbility(player, 262))
            {
                player.SetBuff(2, 360, true);   // Regeneration
                player.SetBuff(113, 360, true); // Lifeforce
                player.SetBuff(48, 360, true);  // Honey
            }

            // Stone Skin (Golem)
            if (HasAbility(player, 245))
            {
                player.SetBuff(5, 360, true);   // Ironskin
                player.SetBuff(114, 360, true); // Endurance
                player.SetBuff(116, 360, true); // Inferno (visual tank)
            }

            // Prismatic Power (Empress)
            if (HasAbility(player, 636))
            {
                player.SetBuff(115, 360, true); // Wrath
                player.SetBuff(5, 360, true);   // Ironskin
                player.SetBuff(114, 360, true); // Endurance
            }

            // Arcane Mastery (Lunatic Cultist)
            if (HasAbility(player, 439))
            {
                player.SetBuff(6, 360, true);   // Magic Power
                player.SetBuff(7, 360, true);   // Mana Regeneration
            }

            // Godslayer (Moon Lord)
            if (HasAbility(player, 398))
            {
                player.SetBuff(115, 360, true); // Wrath
                player.SetBuff(117, 360, true); // Rage
                player.SetBuff(5, 360, true);   // Ironskin
                player.SetBuff(114, 360, true); // Endurance
                player.SetBuff(2, 360, true);   // Regeneration
                player.SetBuff(113, 360, true); // Lifeforce
            }
        }
    }

    private void OnJoin(JoinEventArgs args)
    {
        Task.Delay(2500).ContinueWith(_ =>
        {
            var player = TShock.Players[args.Who];
            if (player != null && player.Active && player.IsLoggedIn)
                ApplyPeriodicEffects();
        });
    }

    private void AbilitiesCommand(CommandArgs args)
    {
        var player = args.Player;
        if (!player.IsLoggedIn)
        {
            player.SendErrorMessage("You must be logged in.");
            return;
        }

        string key = player.Account.Name;

        // Toggle
        if (args.Parameters.Count >= 1 && args.Parameters[0].ToLower() is "toggle" or "t")
        {
            if (args.Parameters.Count < 2)
            {
                player.SendErrorMessage("Usage: /abilities toggle <ability name>");
                return;
            }

            string search = string.Join(" ", args.Parameters.Skip(1)).ToLower();
            bool found = false;

            if (playerData.TryGetValue(key, out var abilities))
            {
                foreach (var kvp in abilities.ToList())
                {
                    if (BossNames.TryGetValue(kvp.Key, out var name) &&
                        name.ToLower().Contains(search))
                    {
                        abilities[kvp.Key] = !kvp.Value;
                        string status = abilities[kvp.Key] ? "[c/00FF00:ON]" : "[c/FF5555:OFF]";
                        player.SendSuccessMessage($"{name} is now {status}");
                        SaveData();
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
                player.SendErrorMessage("Ability not found or you don't have it.");
            return;
        }

        // List
        if (!playerData.TryGetValue(key, out var unlocked) || unlocked.Count == 0)
        {
            player.SendInfoMessage("You have no boss abilities yet. Go kill some bosses!");
            return;
        }

        player.SendSuccessMessage("=== Your Boss Abilities ===");
        foreach (var kvp in unlocked)
        {
            if (BossNames.TryGetValue(kvp.Key, out var name))
            {
                string status = kvp.Value ? "[c/00FF00:ON]" : "[c/FF5555:OFF]";
                player.SendInfoMessage($"• {name} {status}");
            }
        }
        player.SendInfoMessage("Use [c/FFFF00:/abilities toggle <name>] to turn one on/off");
    }

    private void AdminCommand(CommandArgs args)
    {
        if (args.Parameters.Count < 1)
        {
            args.Player.SendInfoMessage("Usage:");
            args.Player.SendInfoMessage("/baadmin list <player>");
            args.Player.SendInfoMessage("/baadmin give <player> <bossid>");
            args.Player.SendInfoMessage("/baadmin reset <player>");
            return;
        }

        string sub = args.Parameters[0].ToLower();

        if (sub == "list" && args.Parameters.Count >= 2)
        {
            var target = TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault();
            if (target == null) { args.Player.SendErrorMessage("Player not found."); return; }

            string key = target.Account?.Name ?? target.Name;
            if (playerData.TryGetValue(key, out var list))
            {
                args.Player.SendInfoMessage($"{target.Name}'s abilities:");
                foreach (var kvp in list)
                    if (BossNames.ContainsKey(kvp.Key))
                        args.Player.SendInfoMessage($"- {BossNames[kvp.Key]} ({(kvp.Value ? "ON" : "OFF")})");
            }
            else args.Player.SendInfoMessage("No abilities.");
        }
        else if (sub == "give" && args.Parameters.Count >= 3)
        {
            var target = TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault();
            if (target == null || !int.TryParse(args.Parameters[2], out int id))
            {
                args.Player.SendErrorMessage("Invalid player or boss id.");
                return;
            }
            UnlockAbility(target, id);
            args.Player.SendSuccessMessage($"Gave ability to {target.Name}");
        }
        else if (sub == "reset" && args.Parameters.Count >= 2)
        {
            var target = TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault();
            if (target == null) { args.Player.SendErrorMessage("Player not found."); return; }
            playerData.Remove(target.Account?.Name ?? target.Name);
            SaveData();
            args.Player.SendSuccessMessage($"Reset abilities for {target.Name}");
        }
    }

    private void LoadData()
    {
        try
        {
            if (File.Exists(DataPath))
            {
                var json = File.ReadAllText(DataPath);
                playerData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, bool>>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[BossAbilities] Failed to load data: {ex.Message}");
        }
    }

    private void SaveData()
    {
        try
        {
            var json = JsonSerializer.Serialize(playerData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(DataPath, json);
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[BossAbilities] Failed to save data: {ex.Message}");
        }
    }

    private void OnReload(ReloadEventArgs args)
    {
        LoadData();
        args.Player.SendSuccessMessage("[BossAbilities] Reloaded.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            GeneralHooks.ReloadEvent -= OnReload;

            abilityTimer?.Stop();
            abilityTimer?.Dispose();

            SaveData();
        }
        base.Dispose(disposing);
    }
}
