# Tray Media

Windows 10 Fluent tray media popover. Controls system Now Playing via SMTC.

**Author:** krmll4444  
**Version:** 1.0.0-beta.1  
**Stack:** C# / WPF

## Dev

Requires [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) with the Windows Desktop workload.

```bash
dotnet run --project TrayMedia.App
```

## Build

Portable self-contained exe (no .NET install required on the target machine):

```powershell
.\build-native.ps1 -Mode portable
```

Smaller build (~20 MB); requires [.NET 7 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/7.0):

```powershell
.\build-native.ps1 -Mode runtime
```

Artifacts go to `dist/`.
