# MarqueeLight Codex Integration

This folder connects Codex CLI hooks to the same local MarqueeLight HTTP API used by the OpenCode plugin.

## Project-local install

The repository already includes project-local Codex config in `.codex/`.

```powershell
codex.cmd features enable hooks
```

Restart Codex in this repository after enabling the feature flag.

## Global install

If you want the light to work in every Codex project, copy `hooks.json` to your global Codex config and change each command to an absolute script path.

```powershell
Copy-Item .\codex-plugin\hooks.json $env:USERPROFILE\.codex\hooks.json
```

Then replace `codex-plugin/marqueelight-codex-hook.js` with:

```text
C:/Users/zhang/Desktop/MarqueeLight/codex-plugin/marqueelight-codex-hook.js
```

## State Mapping

| Codex event | Light state |
| --- | --- |
| `UserPromptSubmit` / `PreToolUse` | green + marquee |
| `PermissionRequest` | red + blink |
| `Stop` | green + steady |

Set `MARQUEELIGHT_API_BASE` if MarqueeLight is not using `http://localhost:10080`.
