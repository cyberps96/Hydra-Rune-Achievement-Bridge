using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;

class Program
{
    
    private static readonly string RuneBaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
        @"Steam\RUNE");

    
    private static readonly string GoldbergBaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"Goldberg SteamEmu Saves");

    static void Main()
    {
        Console.Title = "Hydra <-> RUNE Universal Achievement Bridge";
        Console.WriteLine("=================================================");
        Console.WriteLine("    Universal RUNE to Hydra Achievement Bridge   ");
        Console.WriteLine("=================================================");

        if (!Directory.Exists(RuneBaseDir))
        {
            Directory.CreateDirectory(RuneBaseDir);
        }
        if (!Directory.Exists(GoldbergBaseDir))
        {
            Directory.CreateDirectory(GoldbergBaseDir);
        }

        
        SyncAllGames();

        
        using var watcher = new FileSystemWatcher(RuneBaseDir)
        {
            Filter = "achievements.ini",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;

        Console.WriteLine($"\n[+] Watching for ALL games under: {RuneBaseDir}");
        Console.WriteLine("[*] Bridge is active. Run this in background while gaming.");
        Console.WriteLine("Press 'Q' to exit.\n");

        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Thread.Sleep(300); 

        
        var dirInfo = new DirectoryInfo(Path.GetDirectoryName(e.FullPath));
        string appId = dirInfo.Name;

        if (!string.IsNullOrEmpty(appId) && long.TryParse(appId, out _))
        {
            SyncSingleGame(appId, e.FullPath);
        }
    }

    private static void SyncAllGames()
    {
        var gameDirs = Directory.GetDirectories(RuneBaseDir);
        foreach (var dir in gameDirs)
        {
            string appId = new DirectoryInfo(dir).Name;
            string iniFile = Path.Combine(dir, "achievements.ini");

            if (File.Exists(iniFile) && long.TryParse(appId, out _))
            {
                SyncSingleGame(appId, iniFile);
            }
        }
    }

    private static void SyncSingleGame(string appId, string iniPath)
    {
        try
        {
            var achievements = new Dictionary<string, object>();
            var lines = File.ReadAllLines(iniPath);

            string currentAch = "";
            long unlockTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var line in lines)
            {
                var match = Regex.Match(line.Trim(), @"^\[(.*)\]$");
                if (match.Success)
                {
                    currentAch = match.Groups[1].Value;
                    continue;
                }

                if (!string.IsNullOrEmpty(currentAch) && line.StartsWith("UnlockTime", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split('=');
                    if (parts.Length > 1 && long.TryParse(parts[1].Trim(), out long parsedTime))
                    {
                        unlockTime = parsedTime;
                    }

                    achievements[currentAch] = new
                    {
                        earned = true,
                        earned_time = unlockTime
                    };
                    currentAch = "";
                }
            }

            string targetGoldbergDir = Path.Combine(GoldbergBaseDir, appId);
            Directory.CreateDirectory(targetGoldbergDir);

            string targetJsonPath = Path.Combine(targetGoldbergDir, "achievements.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonOutput = JsonSerializer.Serialize(achievements, options);

            File.WriteAllText(targetJsonPath, jsonOutput);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [AppID: {appId}] Synced {achievements.Count} achievements to Hydra.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error syncing AppID {appId}: {ex.Message}");
        }
    }
}