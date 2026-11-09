# Trailer Cam

A client-side BepInEx plugin for filming flybys with Valheim's free camera. Press **F8** in-world to detach the camera. It starts from your current view with movement smoothing and hides the HUD until you return.

| Control | Action |
| --- | --- |
| F8 | Enter or exit camera mode |
| WASD | Fly forward, back, and sideways |
| Mouse | Look around |
| Scroll wheel | Increase or decrease flight speed |
| Space / Left Ctrl | Rise or descend using your Jump and Crouch bindings |
| Esc | Exit camera mode |

Your character stays in the world and can still take damage. Terrain and objects are streamed around your character, so start near the area you want to film. Capture the footage with your usual screen recorder.

## Install

Select **Trailer Cam** in the [Valheim Mod Installer](https://github.com/Glutoblop/ValheimPlugins), or download `TrailerCam.zip` from the [latest release](https://github.com/Glutoblop/TrailerCam/releases/latest) and extract it into your Valheim folder with BepInEx installed.

To change the hotkey, open **Esc → Mod Settings → Trailer Cam → Camera → Toggle Key**. Click the current binding, hold any modifiers you want, and press the main key. For example, hold **Alt + Ctrl + Shift** and press **F8**. **Reset** restores **F8**.

The Trailer Cam tab also has settings for starting speed, smoothing, and HUD hiding. Settings are saved in `BepInEx/config/jordan.valheim.trailercam.cfg`.

## Build

Build with the .NET SDK and a local Valheim install with BepInEx. The build copies the plugin DLL to `dist/`.

```powershell
dotnet build -c Release -p:GameDir="C:\Path\To\Valheim"
```

## Release

Run `release\Release.bat` or `powershell -File release\release.ps1 -Changes "Release notes"` from a clean, pushed `main` branch. The releaser bumps the version, builds, commits, pushes, and publishes the GitHub release. Use `-BuildOnly` to build the package locally.
