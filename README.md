# StreamCodeMOD — Gorilla Tag BepInEx Mod

Displays a draggable, toggleable HUD overlay showing your **current room code** and **player count**.

---

## Features

- **Room Code** — shows the Photon room name you're currently in
- **Player Count** — live `current / max` player display
- **Toggle** — press `K` to show/hide the window
- **Draggable** — click and drag the header bar to reposition anywhere on screen

---

---

## Building

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/) or the [.NET SDK](https://dotnet.microsoft.com/).
2. Open `StreamCode.csproj`.
3. Set `<GorillaTagPath>` in the `.csproj` to your actual Gorilla Tag install path.
4. Build (`Ctrl+Shift+B`). The DLL will be auto-copied to `BepInEx/plugins/StreamCodeMOD/`.

---

## Manual Install

1. Copy `StreamCodeMOD.dll` into:
   ```
   <GorillaTagInstall>/BepInEx/plugins/StreamCodeMOD/StreamCodeMOD.dll
   ```
2. Launch Gorilla Tag.
3. Press **K** in-game to toggle the overlay.

---

## Usage

| Action | Input |
|---|---|
| Toggle HUD | `K` |
| Drag window | Click & drag the green header |

The overlay will show `NOT IN ROOM` and `—` when you are in the main menu or not connected to a Photon room.

---

## File Structure

```
RoomInfoMod/
├── StreamCode.csproj
├── PluginInfo.cs
├── StreamCodePlugin.cs
└── README.md
```
## Extra
```
Please dont skid my code, if you want to use it for your own mod, ask me first and give credit where its due, thanks.
```
