# Build

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
