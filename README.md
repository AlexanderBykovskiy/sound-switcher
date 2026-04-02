# Build

`dotnet build -c Release` - build

`dotnet run -c Release` - run

## Create install file

`dotnet publish -c Release -r win-x64 --self-contained true` - make install file (win64) to `installer-output\SoundSwitcher-Setup.exe`
