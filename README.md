# Hydra Universal Achievement Manager (v2.0)

A lightweight C# CLI utility that enables seamless Steam achievement syncing with **Hydra Launcher** for cracked games using **RUNE** and **Goldberg / GSE** emulators.

---

## What's New in v2.0
- **Universal RUNE Parser:** Full support for both `.ini` styles (`[Section]` headers with `UnlockTime` and direct `ACH_NAME=1` key-values).
- **Auto-Schema Generator for Goldberg:** Download full achievement lists directly using Steam's public global API (No Web API key needed).
- **Auto-Sync on Startup:** Automatically syncs past unlocked achievements upon launching the bridge.

---

## How to Use

### Option 1: Setup a Game (Goldberg / GSE)
1. Run the application as **Administrator**.
2. Select **Option 1**.
3. Drag & drop the game directory (or enter the path).
4. The schema (`achievements.json`) will be generated inside `steam_settings` automatically.

### Option 2: Real-time Auto-Bridge (RUNE)
1. Run the application as **Administrator**.
2. Select **Option 2** and keep it running in the background.
3. Play any RUNE game; achievements will mirror to Hydra in real time.

---

## Requirements
- Windows 10/11 (x64)
- Run as Administrator (required for file monitoring permissions)
