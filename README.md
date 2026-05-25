# ExpressiveJelly (Jellyfin Web Theme Injector)

Server plugin for Jellyfin **10.11.8** that injects a Material You Expressive-inspired theme (CSS + JS motion) into **Jellyfin Web clients** by registering an `index.html` transformation with **IAmParadox’s File Transformation** plugin.

Includes:

- Expressive theme CSS (derived from `dist/jellyfinexpressive.css`)
- Squiggly buffering progress-bar morph (derived from `dist/jellyfinexpressive.user.js`)

## Requirements

- Jellyfin Server `10.11.8`
- File Transformation plugin installed (IAmParadox)
- (Optional) Moonfin installed: compatible; this plugin adds a separate injection block and avoids duplicating itself.

## Install

1. Install **File Transformation** plugin.
2. Install this plugin (`ExpressiveJelly`) into Jellyfin.
3. Restart Jellyfin.
4. Hard refresh the web client (`Ctrl+Shift+R`).

## GitHub build downloads

This repo includes a GitHub Actions workflow that builds the plugin DLL.

- For a stable direct download link, create a tag like `v1.0.0` and GitHub will attach `Jellyfin.Plugin.ExpressiveJelly.dll` to the Release.

## What it injects

- A stylesheet: `./ExpressiveJelly/theme.css`
- A script: `./ExpressiveJelly/theme.js`

These are served by the plugin itself and only loaded by web clients (because the injection is done in `index.html`).

## Build

`dotnet build /Users/meliko/jellyfinexpressive/ExpressiveJelly.sln -c Release`

The plugin DLL lands at:

`/Users/meliko/jellyfinexpressive/Jellyfin.Plugin.ExpressiveJelly/bin/Release/net9.0/Jellyfin.Plugin.ExpressiveJelly.dll`
