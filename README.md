# Tray Media

Windows 10 Fluent tray media popover. Controls system Now Playing via SMTC.

**Author:** krmll4444  
**Version:** 1.0.0-beta.1  
**Stack:** C# / WPF  
**License:** [MIT](LICENSE) © 2026 krmll4444

## Download

[Latest release](https://github.com/krmll4444/tray-media/releases/latest)

## Dev

Requires [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) with the Windows Desktop workload.

```bash
dotnet run --project TrayMedia.App
```

## Tests

Unit tests use **xUnit** (positioning, settings, playback state, time formatting).

Security / penetration-style tests cover local attack surfaces: tampered `settings.json`, Run-key injection, tray executable matching, and `asInvoker` manifest.

```powershell
.\test.ps1
.\test.ps1 -Category Security
.\test.ps1 -Category Unit
```

Or directly:

```bash
dotnet test TrayMedia.sln
dotnet test TrayMedia.sln --filter Category=Security
```

`.\build-native.ps1` always runs the full suite first and aborts on failure (`-SkipTests` to bypass).

UI / SMTC end-to-end tests are not included yet (would need FlaUI or similar).

## Build

Portable self-contained exe (no .NET install required on the target machine):

```powershell
.\build-native.ps1 -Mode portable
```

Smaller build (~20 MB); requires [.NET 7 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/7.0):

```powershell
.\build-native.ps1 -Mode runtime
```

### Installer

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`).

```powershell
.\build-installer.ps1 -Mode portable
```

Output: `dist/TrayMedia-Setup-1.0.0-beta.1.exe`

The wizard shows the EULA (license + privacy summary) and requires acceptance. **Launch at Windows sign-in** is checked by default (user can clear it).

Artifacts go to `dist/`.

## Legal

| Document | Purpose |
|----------|---------|
| [LICENSE](LICENSE) | MIT license text |
| [NOTICE](NOTICE) | Copyright and third-party attributions |
| [PRIVACY](PRIVACY.md) | What the app stores and reads locally |
| [SECURITY](SECURITY.md) | Vulnerability reporting |
| [CONTRIBUTING](CONTRIBUTING.md) | Contribution rules |
| [CODE_OF_CONDUCT](CODE_OF_CONDUCT.md) | Community standards |

Tray Media is provided **as is**, without warranty. See LICENSE.
