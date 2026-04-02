# Build

`dotnet build -c Release` - build

`dotnet run -c Release` - run

## Create install file

1. Build the application:
   `dotnet publish -c Release -r win-x64 --self-contained true`
2. Installer (**Inno Setup**): Install from [jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php), then from the repository root:
   - **PowerShell:** `& "path\to\ISCC.exe" installer.iss` — common locations: `C:\Program Files (x86)\Inno Setup 6\ISCC.exe` or per-user: `%LocalAppData%\Programs\Inno Setup 6\ISCC.exe` (for example, `C:\Users\…\AppData\Local\Programs\Inno Setup 6\ISCC.exe`).
   - If the path is unknown: `Get-ChildItem -Path "$env:ProgramFiles","${env:ProgramFiles(x86)}","$env:LOCALAPPDATA\Programs" -Filter ISCC.exe -Recurse -ErrorAction SilentlyContinue`.
   - **Without CLI:** Open `installer.iss` in Inno Setup → **Build → Compile**.
   - **cmd.exe:** `"path\to\ISCC.exe" installer.iss`

Result: `installer-output\SoundSwitcher-Setup.exe` (source files — `bin\Release\net8.0-windows\win-x64\publish\`, see `installer.iss`).
