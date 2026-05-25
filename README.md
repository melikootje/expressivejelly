# Yuorself (Jellyfin Web Theme Injector)

Server plugin for Jellyfin **10.11.8** that injects a Material You Expressive-inspired theme (CSS + optional JS motion) into **Jellyfin Web clients** by registering an `index.html` transformation with **IAmParadox’s File Transformation** plugin.

Includes:

- Expressive theme CSS (derived from `dist/jellyfinexpressive.css`)
- Squiggly buffering progress-bar morph (derived from `dist/jellyfinexpressive.user.js`)

## Requirements

- Jellyfin Server `10.11.8`
- File Transformation plugin installed (IAmParadox)
- (Optional) Moonfin installed: compatible; this plugin adds a separate injection block and avoids duplicating itself.

## Install

1. Install **File Transformation** plugin.
2. Install this plugin (`Yuorself`) into Jellyfin.
3. Restart Jellyfin.
4. Hard refresh the web client (`Ctrl+Shift+R`).

## What it injects

- A stylesheet: `./Yuorself/theme.css`
- A script: `./Yuorself/theme.js`

These are served by the plugin itself and only loaded by web clients (because the injection is done in `index.html`).

## Build

`dotnet build /Users/meliko/jellyfinexpressive/Yuorself.sln -c Release`

The plugin DLL lands at:

`/Users/meliko/jellyfinexpressive/Jellyfin.Plugin.Yuorself/bin/Release/net9.0/Jellyfin.Plugin.Yuorself.dll`

