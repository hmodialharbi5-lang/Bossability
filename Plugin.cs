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
    public override string Description => "Kill bosses → unlock real abilities";
    public override Version Version => new(2, 2, 0);

    private static readonly string DataPath = Path.Combine(TShock.SavePath, "BossAbilities.json");
    private Dictionary<string, Dictionary<int, bool>> playerData = new();
    private System.Timers.Timer? timer;

    private readonly Dictionary<int, string> Names = new()
    {
        {50,"Super Jump"},{4,"Predator Vision"},{13,"Life Force"},{266,"Life Force"},
        {222,"Bee Wings"},{35,"Bone Armor"},{668,"Frost Shield"},{113,"Hellwalker"},
        {657,"Royal Speed"},{125,"Overclock"},{126,"Overclock"},{134,"Overclock"},
        {127,"Overclock"},{262,"Jungle Heart"},{245,"Stone Skin"},{370,"Ocean Lord"},
        {636,"Prismatic Power"},{439,"Arcane Mastery"},{398,"Godslayer"}
    };

    public Plugin(Main game) : base(game) { }

    public override void Initialize()
    {
        LoadData();

        ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        GeneralHooks.ReloadEvent += OnReload;

        timer = new System.Timers.Timer(1500);
        timer.Elapsed += (s, e) => ApplyAll();
        timer.AutoReset = true;
        timer.Start();

        Commands.ChatCommands.Add(new Command("bossabilities.use", Cmd, "abilities", "ba"));
        Commands.ChatCommands.Add(new Command("bossabilities.admin", AdminCmd, "baadmin"));

        TShock.Log.ConsoleInfo("[BossAbilities] v2.2 loaded - aggressive mode");
    }

    private string Key(TSPlayer p) => p.Account?.Name ?? p.Name ?? "unknown";

    private bool Has(TSPlayer p, int id)
    {
        var k = Key(p);
        return playerData.TryGetValue(k, out var d) && d.TryGetValue(id, out bool on) && on;
    }

    private void OnNpcKilled(NpcKilledEventArgs args)
    {
        var npc = args.npc;
        if (npc == null || !npc.active) return;

        int id = npc.netID;
        if (id is 13 or 14 or 15) id = 13;
        if (id is 266 or 267) id = 266;
        if (id is 125 or 126) id = 125;
        if (id is 134 or 135 or 136) id = 134;
        if (id is 127 or 128 or 129 or 130 or 131) id = 127;
        if (id is 398 or 397 or 396) id = 398;

        if (!Names.ContainsKey(id)) return;

        foreach (var plr in TShock.Players)
        {
            if (plr == null || !plr.Active || !plr.IsLoggedIn) continue;
            if (plr.TPlayer.Distance(npc.Center) < 2000f)
                Unlock(plr, id);
        }
    }

    private void Unlock(TSPlayer p, int id)
    {
        var k = Key(p);
        if (!playerData.ContainsKey(k)) playerData[k] = new();
        if (playerData[k].ContainsKey(id)) return;

        playerData[k][id] = true;
        p.SendSuccessMessage($"Unlocked [c/FFD700:{Names[id]}]");
        TSPlayer.All.SendInfoMessage($"{p.Name} unlocked [c/FFD700:{Names[id]}]");
        SaveData();
    }

    private void OnUpdate(EventArgs args)
    {
        foreach (var p in TShock.Players)
        {
            if (p == null || !p.Active || !p.IsLoggedIn || p.TPlayer == null) continue;
            var t = p.TPlayer;

            // Bee Wings - force flight every frame
            if (Has(p, 222))
            {
                t.wings = 22;
                t.wingsLogic = 22;
                t.wingTime = 999;
                t.wingTimeMax = 999;
            }

            // Super Jump
            if (Has(p, 50))
            {
                t.jumpSpeedBoost = 3.5f;
                t.noFallDmg = true;
            }

            // Royal Speed
            if (Has(p, 657))
            {
                t.moveSpeed = 1.5f;
            }

            // Ocean Lord
            if (Has(p, 370))
            {
                t.gills = true;
                t.ignoreWater = true;
            }

            // Hellwalker
            if (Has(p, 113))
            {
                t.lavaImmune = true;
                t.fireWalk = true;
            }
        }
    }

    private void ApplyAll()
    {
        foreach (var p in TShock.Players)
        {
            if (p == null || !p.Active || !p.IsLoggedIn) continue;

            // Strong permanent buffs
            if (Has(p, 4))   { p.SetBuff(12, 600, true); p.SetBuff(17, 600, true); p.SetBuff(16, 600, true); }
            if (Has(p, 13) || Has(p, 266)) { p.SetBuff(2, 600, true); p.SetBuff(113, 600, true); }
            if (Has(p, 35))  { p.SetBuff(5, 600, true); p.SetBuff(114, 600, true); }
            if (Has(p, 668)) { p.SetBuff(5, 600, true); p.SetBuff(47, 600, true); }
            if (Has(p, 125) || Has(p, 134) || Has(p, 127)) { p.SetBuff(115, 600, true); p.SetBuff(117, 600, true); p.SetBuff(2, 600, true); }
            if (Has(p, 262)) { p.SetBuff(2, 600, true); p.SetBuff(113, 600, true); p.SetBuff(48, 600, true); }
            if (Has(p, 245)) { p.SetBuff(5, 600, true); p.SetBuff(114, 600, true); }
            if (Has(p, 636)) { p.SetBuff(115, 600, true); p.SetBuff(5, 600, true); p.SetBuff(114, 600, true); }
            if (Has(p, 439)) { p.SetBuff(6, 600, true); p.SetBuff(7, 600, true); }
            if (Has(p, 398)) { p.SetBuff(115, 600, true); p.SetBuff(117, 600, true); p.SetBuff(5, 600, true); p.SetBuff(114, 600, true); p.SetBuff(2, 600, true); p.SetBuff(113, 600, true); }
        }
    }

    private void OnJoin(JoinEventArgs args)
    {
        Task.Run(async () =>
        {
            await Task.Delay(4000);
            var p = TShock.Players[args.Who];
            if (p != null && p.Active && p.IsLoggedIn)
            {
                ApplyAll();
                p.SendInfoMessage("[BossAbilities] Abilities applied.");
            }
        });
    }

    private void Cmd(CommandArgs args)
    {
        var p = args.Player;
        if (!p.IsLoggedIn) { p.SendErrorMessage("Login first"); return; }
        var k = Key(p);

        if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() is "toggle" or "t")
        {
            if (args.Parameters.Count < 2) { p.SendErrorMessage("/abilities toggle <name>"); return; }
            string s = string.Join(" ", args.Parameters.Skip(1)).ToLower();
            if (playerData.TryGetValue(k, out var d))
            {
                foreach (var kv in d.ToList())
                {
                    if (Names.TryGetValue(kv.Key, out var n) && n.ToLower().Contains(s))
                    {
                        d[kv.Key] = !kv.Value;
                        p.SendSuccessMessage($"{n} is now {(d[kv.Key] ? "ON" : "OFF")}");
                        SaveData();
                        return;
                    }
                }
            }
            p.SendErrorMessage("Not found");
            return;
        }

        if (!playerData.TryGetValue(k, out var list) || list.Count == 0)
        {
            p.SendInfoMessage("No abilities yet");
            return;
        }

        p.SendSuccessMessage("=== Abilities ===");
        foreach (var kv in list)
            if (Names.TryGetValue(kv.Key, out var n))
                p.SendInfoMessage($"• {n} {(kv.Value ? "ON" : "OFF")}");
    }

    private void AdminCmd(CommandArgs args)
    {
        if (args.Parameters.Count < 1) { args.Player.SendInfoMessage("/baadmin give|list|reset"); return; }
        string sub = args.Parameters[0].ToLower();

        if (sub == "give" && args.Parameters.Count >= 3)
        {
            var t = TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault();
            if (t == null || !int.TryParse(args.Parameters[2], out int id)) { args.Player.SendErrorMessage("Invalid"); return; }
            Unlock(t, id);
            args.Player.SendSuccessMessage("Given");
        }
        else if (sub == "list" && args.Parameters.Count >= 2)
        {
            var t = TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault();
            if (t == null) { args.Player.SendErrorMessage("Not found"); return; }
            var k = Key(t);
            if (playerData.TryGetValue(k, out var d))
                foreach (var kv in d) if (Names.ContainsKey(kv.Key)) args.Player.SendInfoMessage($"{Names[kv.Key]} = {kv.Value}");
            else args.Player.SendInfoMessage("None");
        }
        else if (sub == "reset" && args.Parameters.Count >= 2)
        {
            var t = TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault();
            if (t == null) return;
            playerData.Remove(Key(t));
            SaveData();
            args.Player.SendSuccessMessage("Reset");
        }
    }

    private void LoadData()
    {
        try
        {
            if (File.Exists(DataPath))
                playerData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, bool>>>(File.ReadAllText(DataPath)) ?? new();
        }
        catch { }
    }

    private void SaveData()
    {
        try { File.WriteAllText(DataPath, JsonSerializer.Serialize(playerData, new JsonSerializerOptions { WriteIndented = true })); }
        catch { }
    }

    private void OnReload(ReloadEventArgs args) { LoadData(); args.Player.SendSuccessMessage("Reloaded"); }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            GeneralHooks.ReloadEvent -= OnReload;
            timer?.Stop();
            timer?.Dispose();
            SaveData();
        }
        base.Dispose(disposing);
    }
}
