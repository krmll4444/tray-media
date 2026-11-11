# Contributing

Thanks for helping improve Tray Media.

## Rules

1. **English only** for UI strings and repository docs (README, issues, PRs).
2. Keep the app a **standard-user** process (`asInvoker`). Do not add Admin/UAC requirements without an explicit design discussion.
3. Prefer **native Windows 10** look and behavior over custom chrome flourishes.
4. Do not commit secrets, personal settings, `dist/`, `bin/`, `obj/`, or local `tools/` outputs.
5. Run tests before proposing a build/release change:

   ```powershell
   .\test.ps1
   ```

6. `.\build-native.ps1` must keep running tests first unless `-SkipTests` is justified in the PR.
7. Security-sensitive changes (settings I/O, autostart, tray registry/COM) need coverage under `Category=Security` when practical.

## Pull requests

- Keep diffs focused; separate refactors from feature work when possible.
- Describe the user-visible change and how you tested it.
- Update README / NOTICE if you add dependencies or change license-relevant attribution.

## Code style

- C# / WPF, nullable enabled, match existing project patterns.
- No Cyrillic in product UI or committed docs.
