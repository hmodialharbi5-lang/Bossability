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
    public override string Description => "Kill bosses → unlock real unique abilities";
    public override Version Version => new(2, 1, 0);

    private static readonly string DataPath = Path.Combine(TShock.SavePath, "BossAbilities.json");
    private Dictionary<string, Dictionary<int, bool>> playerData = new();
    private System.Timers.Timer? abilityTimer;

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

        abilityTimer = new System.Timers.Timer(2000); // every 2 seconds
        abilityTimer.Elapsed += (s, e) => ApplyBuffs();
        abilityTimer.AutoReset = true;
        abilityTimer.Start();

        Commands.ChatCommands.Add(new Command("bossabilities.use", AbilitiesCommand, "abilities", "ba")
        {
            HelpText = "View or toggle your boss abilities"
        });

        Commands.ChatCommands.Add(new Command("bossabilities.admin", AdminCommand, "baadmin"));

        TShock.Log.ConsoleInfo("[BossAbilities] v2.1 Loaded!");
    }

    private void OnNpcKilled(NpcKilledEventArgs args)
    {
        var npc = args.npc;
        if (npc == null || !npc.active) return;

        int type = npc.netID;
        int bossId = type;

        if (type is 13 or 14 or 15) bossId = 13;
        if (type is 266 or 267) bossId = 266;
        if (type is 125 or 126) bossId = 125;
        if (type is 134 or 135 or 136) bossId = 134;
        if (type is 127 or 128 or 129 or 130 or 131) bossId = 127;
        if (type is 398 or 397 or 396) bossId = 398;

        if (!BossNames.ContainsKey(bossId)) return;

        foreach (var player in TShock.Players)
        {
            if (player == null || !player.Active || !player.IsLoggedIn) continue;
            if (player.TPlayer.Distance(npc.Center) < 1800f)
            {
                UnlockAbility(player, bossId);
            }
        }
    }

    private void UnlockAbility(TSPlayer player, int bossId)
    {
        string key = GetKey(player);
        if (!playerData.ContainsKey(key))
            playerData[key] = new Dictionary<int, bool>();

        if (!playerData[key].ContainsKey(bossId))
        {
            playerData[key][bossId] = true;
            player.SendSuccessMessage($"Unlocked: [c/FFD700:{BossNames[bossId]}]");
            TSPlayer.All.SendInfoMessage($"{player.Name} unlocked [c/FFD700:{BossNames[bossId]}]");
            SaveData();
        }
    }

    private string GetKey(TSPlayer player)
    {
        return player.Account?.Name ?? player.Name;
    }

    private bool Has(TSPlayer player, int id)
    {
        string key = GetKey(player);
        return playerData.TryGetValue(key, out var dict) &&
               dict.TryGetValue(id, out bool on) && on;
    }

    private void OnGameUpdate(EventArgs args)
    {
        // Run every frame for movement / flight related abilities
        foreach (var player in TShock.Players)
        {
            if (player == null || !player.Active || !player.IsLoggedIn || player.TPlayer == null)
                continue;

            var p = player.TPlayer;

            // ===== Bee Wings - Real Flight =====
            if (Has(player, 222))
            {
                p.wings = 22;
                p.wingsLogic = 22;
                p.wingTime = 300;
                p.wingTimeMax = 300;
            }

            // ===== Super Jump =====
            if (Has(player, 50))
            {
                p.jumpSpeedBoost = Math.Max(p.jumpSpeedBoost, 3.2f);
                p.noFallDmg = true;
            }

            // ===== Royal Speed =====
            if (Has(player, 657))
            {
                p.moveSpeed = Math.Max(p.moveSpeed, 1.45f);
            }

            // ===== Ocean Lord =====
            if (Has(player, 370))
            {
                p.gills = true;
                p.ignoreWater = true;
                if (p.wet)
                    p.moveSpeed = Math.Max(p.moveSpeed, 1.6f);
            }

            // ===== Hellwalker =====
            if (Has(player, 113))
            {
                p.lavaImmune = true;
                p.fireWalk = true;
            }
        }
    }

    private void ApplyBuffs()
    {
        foreach (var player in TShock.Players)
        {
            if (player == null || !player.Active || !player.IsLoggedIn) continue;

            // Predator Vision
            if (Has(player, 4))
            {
                player.SetBuff(12, 400, true); // Night Owl
                player.SetBuff(17, 400, true); // Hunter
                player.SetBuff(16, 400, true); // Spelunker
            }

            // Life Force
            if (Has(player, 13) || Has(player, 266))
            {
                player.SetBuff(2, 400, true);   // Regeneration
                player.SetBuff(113, 400, true); // Lifeforce
            }

            // Bone Armor
            if (Has(player, 35))
            {
                player.SetBuff(5, 400, true);   // Ironskin
                player.SetBuff(114, 400, true); // Endurance
            }

            // Frost Shield
            if (Has(player, 668))
            {
                player.SetBuff(5, 400, true);
                player.SetBuff(47, 400, true);  // Warmth
            }

            // Overclock
            if (Has(player, 125) || Has(player, 134) || Has(player, 127))
            {
                player.SetBuff(115, 400, true); // Wrath
                player.SetBuff(117, 400, true); // Rage
                player.SetBuff(2, 400, true);
            }

            // Jungle Heart
            if (Has(player, 262))
            {
                player.SetBuff(2, 400, true);
                player.SetBuff(113, 400, true);
                player.SetBuff(48, 400, true);  // Honey
            }

            // Stone Skin
            if (Has(player, 245))
            {
                player.SetBuff(5, 400, true);
                player.SetBuff(114, 400, true);
            }

            // Prismatic Power
            if (Has(player, 636))
            {
                player.SetBuff(115, 400, true);
                player.SetBuff(5, 400, true);
                player.SetBuff(114, 400, true);
            }

            // Arcane Mastery
            if (Has(player, 439))
            {
                player.SetBuff(6, 400, true);   // Magic Power
                player.SetBuff(7, 400, true);   // Mana Regen
            }

            // Godslayer
            if (Has(player, 398))
            {
                player.SetBuff(115, 400, true);
                player.SetBuff(117, 400, true);
                player.SetBuff(5, 400, true);
                player.SetBuff(114, 400, true);
                player.SetBuff(2, 400, true);
                player.SetBuff(113, 400, true);
            }
        }
    }

    private void OnJoin(JoinEventArgs args)
    {
        Task.Run(async () =>
        {
            await Task.Delay(3000);
            var player = TShock.Players[args.Who];
            if (player != null && player.Active && player.IsLoggedIn)
            {
                ApplyBuffs();
                player.SendInfoMessage("[BossAbilities] Your abilities have been applied.");
            }
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

        string key = GetKey(player);

        if (args.Parameters.Count >= 1 && args.Parameters[0].ToLower() is "toggle" or "t")
        {
            if (args.Parameters.Count < 2)
            {
                player.SendErrorMessage("Usage: /abilities toggle <name>");
                return;
            }

            string search = string.Join(" ", args.Parameters.Skip(1)).ToLower();
            bool found = false;

            if (playerData.TryGetValue(key, out var abilities))
            {
                foreach (var kvp in abilities.ToList())
                {
                    if (BossNames.TryGetValue(kvp.Key, out var name) && name.ToLower().Contains(search))
                    {
                        abilities[kvp.Key] = !kvp.Value;
                        string status = abilities[kvp.Key] ? "[c/00FF00:ON]" : "[c/FF5555:OFF]";
                        player.SendSuccessMessage($"{name} → {status}");
                        SaveData();
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
                player.SendErrorMessage("Ability not found.");
            return;
        }

        if (!playerData.TryGetValue(key, out var unlocked) || unlocked.Count == 0)
        {
            player.SendInfoMessage("No abilities yet. Kill bosses!");
            return;
        }

        player.SendSuccessMessage("=== Your Abilities ===");
        foreach (var kvp in unlocked)
        {
            if (BossNames.TryGetValue(kvp.Key, out var name))
            {
                string status = kvp.Value ? "[c/00FF00:ON]" : "[c/FF5555:OFF]";
                player.SendInfoMessage($"• {name} {status}");
            }
        }
        player.SendInfoMessage("Toggle: /abilities toggle <name>");
    }

    private void AdminCommand(CommandArgs args)
    {
        if (args.Parameters.Count < 1)
        {
            args.Player.SendInfoMessage("/baadmin list <player>");
            args.Player.SendInfoMessage("/baadmin give <player> <bossid>");
            args.Player.SendInfoMessage("/baadmin reset <player>");
            return;
        }

        string sub = args.Parameters[0].ToLower();

        if (sub == "list" && args.Parameters.Count >= 2)
        {
            var target = TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault();
            if (target == null) { args.Player.SendErrorMessage("Player not found"); return; }

            string key = GetKey(target);
            if (playerData.TryGetValue(key, out var list))
            {
                args.Player.SendInfoMessage($"{target.Name}:");
                foreach (var kvp in list)
                    if (BossNames.ContainsKey(kvp.Key))
                        args.Player.SendInfoMessage($"- {BossNames[kvp.Key]} ({(kvp.Value ? "ON" : "OFF")})");
            }
            else args.Player.SendInfoMessage("None");
        }
        else if (sub == "give" && args.Parameters.Count >= 3)
        {
            var target = TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault();
            if (target == null || !int.TryParse(args.Parameters[2], out int id))
            {
                args.Player.SendErrorMessage("Invalid");
                return;
            }
            UnlockAbility(target, id);
            args.Player.SendSuccessMessage("Done");
        }
        else if (sub == "reset" && args.Parameters.Count >= 2)
        {
            var target = TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault();
            if (target == null) { args.Player.SendErrorMessage("Player not found"); return; }
            playerData.Remove(GetKey(target));
            SaveData();
            args.Player.SendSuccessMessage("Reset");
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
                TShock.Log.ConsoleInfo($"[BossAbilities] Loaded data for {playerData.Count} players");
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[BossAbilities] Load error: {ex.Message}");
        }
    }

    private void SaveData()
    {
        try
        {
            File.WriteAllText(DataPath, JsonSerializer.Serialize(playerData, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[BossAbilities] Save error: {ex.Message}");
        }
    }

    private void OnReload(ReloadEventArgs args)
    {
        LoadData();
        args.Player.SendSuccessMessage("[BossAbilities] Reloaded");
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
