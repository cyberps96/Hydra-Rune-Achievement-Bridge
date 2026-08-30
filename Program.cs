using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

class Program
{
    private static readonly string RuneBaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
        @"Steam\RUNE");

    private static readonly string GoldbergTargetDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"Goldberg SteamEmu Saves");

    private static readonly string GseBaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"GSE Saves");

    static async Task Main()
    {
        Console.Title = "Hydra Universal Achievement Manager";

        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=================================================");
            Console.WriteLine("        Hydra Universal Achievement Manager      ");
            Console.WriteLine("=================================================");
            Console.ResetColor();
            Console.WriteLine("1. Setup a New Game (Goldberg / Voices38 Schema Setup)");
            Console.WriteLine("2. Start Real-time Auto-Bridge (RUNE <-> Hydra in background)");
            Console.WriteLine("3. Exit");
            Console.Write("\nChoose an option (1-3): ");

            string choice = Console.ReadLine()?.Trim();

            if (choice == "1")
            {
                await SetupNewGameWizard();
            }
            else if (choice == "2")
            {
                StartAutoBridge();
                break;
            }
            else if (choice == "3")
            {
                return;
            }
        }
    }

    private static async Task SetupNewGameWizard()
    {
        Console.Clear();
        Console.WriteLine("--- Setup Game Schema (Goldberg / GSE) ---");
        Console.Write("\nEnter Game Directory Path (or drag & drop the folder here): ");
        string gamePath = Console.ReadLine()?.Trim('"', ' ');

        if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[-] Invalid directory path!");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to return to menu...");
            Console.ReadKey();
            return;
        }

        var settingsDirs = Directory.GetDirectories(gamePath, "steam_settings", SearchOption.AllDirectories);
        string targetSettingsDir = settingsDirs.Length > 0 ? settingsDirs[0] : "";

        string appId = "";

        if (!string.IsNullOrEmpty(targetSettingsDir) && File.Exists(Path.Combine(targetSettingsDir, "steam_appid.txt")))
        {
            appId = (await File.ReadAllTextAsync(Path.Combine(targetSettingsDir, "steam_appid.txt"))).Trim();
            Console.WriteLine($"[+] Detected AppID from steam_appid.txt: {appId}");
        }
        else
        {
            Console.Write("Could not auto-detect steam_appid.txt. Please enter AppID manually: ");
            appId = Console.ReadLine()?.Trim();
        }

        if (string.IsNullOrEmpty(appId) || !long.TryParse(appId, out _))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[-] Invalid AppID!");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to return to menu...");
            Console.ReadKey();
            return;
        }

        if (string.IsNullOrEmpty(targetSettingsDir))
        {
            targetSettingsDir = Path.Combine(gamePath, "steam_settings");
            Directory.CreateDirectory(targetSettingsDir);
            await File.WriteAllTextAsync(Path.Combine(targetSettingsDir, "steam_appid.txt"), appId);
        }

        using var httpClient = new HttpClient();
        string achievementsFile = Path.Combine(targetSettingsDir, "achievements.json");

        Console.WriteLine("[*] Fetching schema from Steam Community...");
        await DownloadAndGenerateSchemaAsync(httpClient, appId, achievementsFile);

        string gseDir = Path.Combine(GseBaseDir, appId);
        Directory.CreateDirectory(gseDir);
        string gseJson = Path.Combine(gseDir, "achievements.json");
        if (!File.Exists(gseJson)) File.WriteAllText(gseJson, "{}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[✔] Game is fully prepared! You can now play and unlock achievements.");
        Console.ResetColor();
        Console.WriteLine("\nPress any key to return to menu...");
        Console.ReadKey();
    }

    private static void StartAutoBridge()
    {
        Console.Clear();
        Console.WriteLine("[*] Starting Background Auto-Bridge...");

        if (Directory.Exists(RuneBaseDir)) SyncAllRuneGames();
        EnsureGseGamesPrepared();

        using var runeWatcher = new FileSystemWatcher(RuneBaseDir)
        {
            Filter = "achievements.ini",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };

        runeWatcher.Changed += (s, e) => HandleRuneUpdate(e.FullPath);
        runeWatcher.Created += (s, e) => HandleRuneUpdate(e.FullPath);

        Console.WriteLine($"[+] Watching RUNE directory: {RuneBaseDir}");
        Console.WriteLine("[*] Bridge is active in background. Press 'Q' to exit.\n");

        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
    }

    private static async Task DownloadAndGenerateSchemaAsync(HttpClient client, string appId, string targetFilePath)
    {
        try
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            string url = $"https://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/?gameid={appId}";
            string response = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var goldbergAchievements = new List<object>();

            if (root.TryGetProperty("achievementpercentages", out var achPercentages) &&
                achPercentages.TryGetProperty("achievements", out var achievementsArray))
            {
                foreach (var ach in achievementsArray.EnumerateArray())
                {
                    string apiName = ach.GetProperty("name").GetString() ?? "";

                    if (!string.IsNullOrEmpty(apiName))
                    {
                        goldbergAchievements.Add(new
                        {
                            name = apiName,
                            displayName = apiName,
                            description = "",
                            hidden = 0
                        });
                    }
                }
            }

            if (goldbergAchievements.Count > 0)
            {
                string outputJson = JsonSerializer.Serialize(goldbergAchievements, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(targetFilePath, outputJson);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[✔] Successfully generated achievements.json ({goldbergAchievements.Count} achievements).");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[-] No achievements found for this Game ID.");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[-] Failed: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void EnsureGseGamesPrepared()
    {
        if (!Directory.Exists(GseBaseDir)) return;
        foreach (var dir in Directory.GetDirectories(GseBaseDir))
        {
            string appId = new DirectoryInfo(dir).Name;
            if (long.TryParse(appId, out _))
            {
                string jsonFile = Path.Combine(dir, "achievements.json");
                if (!File.Exists(jsonFile)) File.WriteAllText(jsonFile, "{}");
            }
        }
    }

    private static void HandleRuneUpdate(string filePath)
    {
        Thread.Sleep(300);
        string appId = new DirectoryInfo(Path.GetDirectoryName(filePath)).Name;
        if (long.TryParse(appId, out _)) ConvertRuneToGoldberg(appId, filePath);
    }

    private static void SyncAllRuneGames()
    {
        foreach (var dir in Directory.GetDirectories(RuneBaseDir))
        {
            string appId = new DirectoryInfo(dir).Name;
            string iniFile = Path.Combine(dir, "achievements.ini");
            if (File.Exists(iniFile) && long.TryParse(appId, out _)) ConvertRuneToGoldberg(appId, iniFile);
        }
    }

    
    private static void ConvertRuneToGoldberg(string appId, string iniPath)
    {
        try
        {
            var achievements = new Dictionary<string, object>();
            var lines = File.ReadAllLines(iniPath);
            string currentSection = "";

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                var sectionMatch = Regex.Match(line, @"^\[(.*)\]$");
                if (sectionMatch.Success)
                {
                    currentSection = sectionMatch.Groups[1].Value.Trim();
                    continue;
                }

                var kvMatch = Regex.Match(line, @"^(.*?)=(.*)$");
                if (kvMatch.Success)
                {
                    string key = kvMatch.Groups[1].Value.Trim();
                    string val = kvMatch.Groups[2].Value.Trim();

                    
                    if (key.Equals("UnlockTime", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(currentSection))
                    {
                        long unlockTime = long.TryParse(val, out long parsedTime) ? parsedTime : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        achievements[currentSection] = new { earned = true, earned_time = unlockTime };
                    }
                    
                    else if (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase) || val.Equals("unlocked", StringComparison.OrdinalIgnoreCase))
                    {
                        achievements[key] = new { earned = true, earned_time = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
                    }
                    
                    else if (long.TryParse(val, out long unixTime) && unixTime > 1000000000)
                    {
                        achievements[key] = new { earned = true, earned_time = unixTime };
                    }
                }
            }

            string targetDir = Path.Combine(GoldbergTargetDir, appId);
            Directory.CreateDirectory(targetDir);
            string targetJsonPath = Path.Combine(targetDir, "achievements.json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(targetJsonPath, JsonSerializer.Serialize(achievements, options));

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [RUNE -> Hydra] Synced {achievements.Count} achievements for AppID: {appId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error converting AppID {appId}: {ex.Message}");
        }
    }
}
