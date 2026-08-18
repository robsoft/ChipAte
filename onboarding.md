# Onboarding: ChipAte

A CHIP-8 emulator written in C# using MonoGame (DesktopGL). Presents a windowed
UI (built with Gum.Forms) with a Main menu, Options screen, and a file-browser
for picking a ROM to run.

## Prerequisites

- .NET SDK 8.0 or later (the project targets `net8.0` with `RollForward: Major`,
  so a newer SDK on the machine is fine — this was verified against .NET SDK
  10.0.400).
- Windows, Linux, or macOS — MonoGame.Framework.DesktopGL is cross-platform.
  No extra native/graphics SDKs are required beyond what MonoGame's NuGet
  packages pull in.

Check your SDK:

```
dotnet --version
```

## Project layout

- `ChipAte.slnx` — the solution file (references the one project below).
- `ChipAte/ChipAte.csproj` — the app project (WinExe, MonoGame DesktopGL).
  - `Program.cs` — composition root; wires up `Microsoft.Extensions.Logging`
    (console + debug) and DI, then runs `Chip8Wrapper` (the MonoGame `Game`).
  - `Chip8Wrapper.cs` / `Chip8WrapperUI.cs` — the MonoGame game loop, scene
    management (Main/Options/FileSelect/Game), rendering, keypad mapping, and
    the beep sound.
  - `Console/Chip8.cs` — the actual CHIP-8 CPU/interpreter core.
  - `Console/Chip8Debugger.cs` — debugger support around the core.
  - `MainViewModel.cs`, `OptionsViewModel.cs`, `FileSelectViewModel.cs`,
    `Options.cs`, `PlacesProvider.cs`, `DirectoryBrowser.cs`, `FileHelpers.cs`
    — the in-app UI screens (MVVM-ish, via `CommunityToolkit.Mvvm` /
    `Gum.Mvvm`) and the ROM file-browser.
  - `Content/Content.mgcb` — MonoGame Content Builder pipeline file (fonts/UI
    assets for Gum), built automatically as part of the normal build.
  - `Tests.cs` — currently just a scratch file of commented-out sample ROM
    paths used for manual testing; there is **no automated test suite**
    (no `dotnet test` target).

## First-time build

The project uses a local .NET tool (`dotnet-mgcb`, the MonoGame Content
Builder) declared in `ChipAte/.config/dotnet-tools.json`. The `.csproj` has a
pre-build target that runs `dotnet tool restore` automatically, so you don't
need to do this by hand — just build:

```
dotnet build ChipAte.slnx
```

First build will restore NuGet packages (MonoGame, Gum, CommunityToolkit.Mvvm,
Microsoft.Extensions.Logging.*) and the mgcb tool, then compile the content
pipeline and the app. Confirmed working: `Build succeeded, 0 Warning(s), 0
Error(s)`.

## Running

```
cd ChipAte
dotnet run
```

This opens a windowed app at the "Main" scene. Logging goes to both the
console and the debug output (via `Microsoft.Extensions.Logging`).

There's also a VS Code launch config at `ChipAte/.vscode/launch.json`
("C#: ChipAte Debug") if you prefer running/debugging from the editor.

### Loading a ROM

Use the in-app file browser (accessible from the Main screen) to navigate to
and load a `.ch8` ROM — you no longer need to edit source to point at a ROM
(older revisions of this repo required that; it's now done through the UI).

Note: `Options.cs` currently hardcodes the *default* starting folder for the
file browser to `c:\dev\chipate\roms`, which won't exist unless you create it
— this is just a starting point in the browser, not a requirement; you can
navigate anywhere from there. Sample/test ROMs are **not** included in this
repo (see readme.md for links to where to find CHIP-8 test suites and sample
ROMs).

### Keypad mapping

The CHIP-8 hex keypad is hardcoded to:

```
1  2  3  4
Q  W  E  R
A  S  D  F
Z  X  C  V
```

Other useful keys while a ROM is running: `Esc` returns to the Main menu,
`F8` reloads/resets the currently loaded ROM.

## Known state / gaps

- No automated tests — verification is manual, by running ROMs (including the
  Timendus CHIP-8 test suite linked from readme.md) and eyeballing behaviour.
  The emulator passes the standard test suite ROMs except `oob` (out of
  bounds).
- Options (sound, colors, screen size, last-used folder) are in-memory only —
  `Options.LoadOptions()` / `SaveOptions()` are stubs (see `Options.cs`), so
  nothing persists across runs yet.
- Several `TODO`s in `Chip8Wrapper.cs` around adjustable colors/scale, a
  proper debugger toggle, and key debouncing.
