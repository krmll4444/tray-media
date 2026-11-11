# Tray Media

Windows 10 Fluent tray media popover. Керує системним Now Playing через SMTC.

**Автор:** krmll4444  
**Версія:** 1.0.0-beta.1  
**Стек:** C# / WPF

## Dev

Потрібен [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) з Windows Desktop workload.

```bash
dotnet run --project TrayMedia.App
```

## Build

Портативний self-contained exe (без встановленого .NET у користувача):

```powershell
.\build-native.ps1 -Mode portable
```

Мінімальний розмір (~20 MB), потрібен [.NET 7 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/7.0):

```powershell
.\build-native.ps1 -Mode runtime
```

Артефакти в `dist/`.
