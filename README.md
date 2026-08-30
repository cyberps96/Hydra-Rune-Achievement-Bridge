# Universal RUNE to Hydra Achievement Bridge

A lightweight background utility written in C# that automatically bridges achievement progress from **RUNE emulator** (`achievements.ini`) to **Goldberg format** (`achievements.json`) in real-time for seamless **Hydra Launcher** tracking and popups.

---

## Features

- **Universal AppID Detection:** Automatically discovers and monitors any game folder located under `Steam\RUNE`.
- **Real-Time Sync:** Utilizes `FileSystemWatcher` with zero CPU overhead to capture achievements the exact moment they are earned.
- **Startup Sync:** Automatically parses and converts all pre-existing achievements across installed games on launch.

---

## How to Use

1. Download the latest release (`HydraRuneBridge.rar`) from the **[Releases](https://github.com/cyberps96/Hydra-Rune-Achievement-Bridge/releases)** section.
2. Extract and run `HydraRuneBridge.exe` in the background.
3. Launch and play your game through Hydra Launcher.
4. Enjoy real-time popups and achievement updates!

---

## Supported Emulators

- **RUNE** -> **Goldberg / Hydra Launcher**
