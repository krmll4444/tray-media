# Security Policy

## Supported versions

Security fixes are accepted for the current `main` branch and the latest published beta/release.

## What this app is

Tray Media runs as a standard-user (`asInvoker`) desktop process. It is not a service, does not require Administrator, and has no remote API.

## Reporting a vulnerability

Please open a **private** GitHub security advisory on the repository when available, or contact the author (**krmll4444**) with:

- A short description of the issue
- Steps to reproduce
- Impact (e.g. settings overwrite, Run-key abuse, unexpected elevation)

Do not open a public issue for unfixed vulnerabilities that could harm other users.

## Scope examples

In scope:

- Tampering with `%LocalAppData%\TrayMedia\settings.json`
- Injection via the HKCU Run autostart value
- Writing outside allowed settings paths
- Unexpected elevation or `uiAccess` changes in `app.manifest`

Out of scope:

- Compromise of the Windows user account itself
- Malicious media apps that already control SMTC on the same machine
- Social engineering unrelated to this repository

## Local security checks

```powershell
.\test.ps1 -Category Security
```
