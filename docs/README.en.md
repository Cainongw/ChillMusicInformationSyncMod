# Chill Music Information Sync

[简体中文](../README.md) | [English](README.en.md) | [日本語](README.ja.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework 4.7.2](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net472)
[![BepInEx](https://img.shields.io/badge/BepInEx-Plugin-green.svg)](https://github.com/BepInEx/BepInEx)

A BepInEx plugin for the game "*Chill with You*" that enables bidirectional synchronization with Windows Media Control API (SMTC)

---

[![Chill with You](../imgs/header_schinese.jpg)](https://store.steampowered.com/app/3548580/)

> "Chill with You" is a visual novel game where you work with Congyin, a girl who loves writing stories. You can customize the artist's original music, ambient sounds, and scenery to create a focused working environment. As your relationship with Congyin deepens, you may discover a special connection with her.

---

## Actual Usage Effects:

![alt text](../imgs/image.png)

## ⚠️ ATTENTION ⚠️

Using SMTC requires your music player to support it, and not all players support all SMTC features.

For example, the Apple Music desktop app cannot sync progress bars bidirectionally.

Common music players that support SMTC include:

- Spotify
- Groove Music
- YouTube (Chrome)
- Bilibili (Chrome)
- Apple Music (does not support in-game progress bar dragging)
- KuGou Music (progress bar issues)
- NetEase Cloud Music with [BetterNCM](https://github.com/std-microblock/chromatic/tree/v2)

## Main Features

### Player Side

- Song title synchronization
- Artist synchronization
- Album cover synchronization
- Playback status synchronization
- Bidirectional progress bar synchronization

### Game Side

- Previous track
- Next track
- Pause/Play
- Progress bar dragging

## Usage Guide

Due to limitations of the Unity Mono runtime, Windows API cannot be called directly. This project uses [SMTC-Bridge-Cpp](https://github.com/Cainongw/SMTC-Bridge-Cpp) to operate at the native layer.

The Release already includes this, so if you don't need to compile it yourself, you can ignore the above.

---

### **Requirements:**

- Windows 10 1607 or later
- The game itself
- [BepInEx 5.4.23.4](https://github.com/BepInEx/BepInEx/releases) (same as [RealTimeWeatherMod](https://github.com/Small-tailqwq/RealTimeWeatherMod))

### Installation Steps

1. Ensure BepInEx framework is properly installed
2. Launch the game once
3. Extract all contents from the Release to BepInEx\plugins
4. Launch the game

## About Shuffle/Repeat

Music players have poor support for shuffle and repeat controls.

Even though the SMTC library includes controls for shuffle and repeat, after testing, no player actually supports them.

Additionally, each player implements the logic differently. Some combine shuffle/list/single repeat together, while others separate shuffle and repeat.

The best way to implement this would be to simulate keyboard shortcuts.

Therefore, there is currently no plan to support this. The current plan is to disable and hide these two UI buttons in the game during playback.

## Notes

- After the Mod takes over the game UI, if you want to play built-in game songs, you just need to click on the song in the playlist button.

## Development and Building

1. Clone the repository locally
2. Copy `Assembly-CSharp.dll` from the game to ./libs
3. Open with Visual Studio or run dotnet build

## Todo List

- [x] Add song cover synchronization
- [x] Hide the game's loop/shuffle buttons during playback
- [x] Add animations for cover/button appearance and disappearance

## About Other Mods

If you're interested in other mods for this game, see: [awesome-chillwithyou](https://github.com/clsty/awesome-chillwithyou)
