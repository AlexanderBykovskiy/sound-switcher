# Sound Switcher

## Prerequisites

I work remotely, so my job is entirely computer-based. Throughout the day I need to join online meetings, listen to voice or video messages from colleagues, and sometimes just play music in my headphones while coding.

I use wired headphones that are always plugged in. For convenience, Windows can detect when headphones are connected and automatically switch between the main speakers and the headphones. However, it doesn’t handle Bluetooth headphones the same way, nor does it react when you simply take the headphones off your head without unplugging them.

These devices can be switched using the built-in mixer, but it’s never a one‑click action.
That’s why I decided to write a quick audio output device switcher. Luckily, AI can help a lot with this nowadays.

## How the application works

Once installed, the application is ready to run (you can add it to the startup queue so it launches automatically). By the way, if you have many tray icons, it might be hidden — you’ll need to pin it so it’s always visible.

A tray icon will appear, showing which audio device is currently active.

![Tray icon — active output device](Assets/readme/tray.png)

But the application needs to be configured. To do this, right‑click the tray icon and select Settings.

![Settings — devices and icons](Assets/readme/settings.png)

The settings window is very simple: you can check the devices that should be included in switching, and also assign an icon to each one (so it’s easy to recognize them). Click Save, and now each click on the tray icon will cycle through your chosen audio sources.

Would you like me to adapt this into a more concise, product‑style description (like for GitHub README or app store listing), or keep it as a straightforward technical translation?

## Build

`dotnet build -c Release` - build

`dotnet run -c Release` - run

## Create install file

1. Build the application:
   `dotnet publish -c Release -r win-x64 --self-contained true`
2. **Inno Setup** — install from [jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php). From the **repository root**, compile `installer.iss`.

   **Where `ISCC.exe` usually is**

   | Install type | Path |
   |--------------|------|
   | All users (typical) | `C:\Program Files (x86)\Inno Setup 6\ISCC.exe` |
   | Current user only | `%LocalAppData%\Programs\Inno Setup 6\ISCC.exe` |

   **How to build**

   1. **Inno Setup GUI** — open `installer.iss` → **Build → Compile**.
   2. **PowerShell** — replace the path if yours differs:
      ```powershell
      & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
      ```
   3. **cmd.exe**
      ```cmd
      "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
      ```

   **If you don’t know where `ISCC.exe` is** (search common folders):
   ```powershell
   Get-ChildItem -Path "$env:ProgramFiles","${env:ProgramFiles(x86)}","$env:LOCALAPPDATA\Programs" `
     -Filter ISCC.exe -Recurse -ErrorAction SilentlyContinue
   ```

Result: `installer-output\SoundSwitcher-Setup.exe` (source files — `bin\Release\net8.0-windows\win-x64\publish\`, see `installer.iss`).

Release file: [Win64 installer](https://github.com/AlexanderBykovskiy/sound-switcher/tree/main/installer-output)
