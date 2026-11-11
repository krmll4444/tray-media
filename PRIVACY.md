# Privacy

Tray Media is a local Windows utility. It does not require an account and does not include analytics, advertising, or telemetry SDKs.

## Data this app may store on your device

| Data | Location | Purpose |
|------|----------|---------|
| Settings (`Autostart`, `PinToTray`) | `%LocalAppData%\TrayMedia\settings.json` | Remember preferences |
| Autostart entry | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | Optional launch at sign-in |
| Tray pin preference | Windows notification icon settings (when supported) | Optional tray visibility |

## Data this app reads from Windows

- System Media Transport Controls (Now Playing metadata, artwork, playback position) to show and control media that is already playing on your PC.
- Theme / accent colors and tray-related UI settings to match Windows chrome.

That media information stays on your device. Tray Media does not upload it.

## Network

The application does not open network listeners and does not call Tray Media cloud services. Opening linked Windows Settings pages uses the local `ms-settings:` protocol.

## Contact

Author: **krmll4444**
