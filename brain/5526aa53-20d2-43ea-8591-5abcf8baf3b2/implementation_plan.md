# Install unity-mcp and Dependencies

You do not currently have `uv` installed. This plan outlines the steps to install it and the `unity-mcp` package.

## User Review Required

- **Installation Method**: I will use `winget` to install `uv`. Do you prefer another method?
- **Unity Package**: I will verify if I can install the Unity package automatically or if I should just guide you. The plan assumes I will attempt to help with `uv` first.

## Proposed Changes

### System

- Install `uv` using `winget install --id=astral-sh.uv -e`.

### Unity Project

- Add `unity-mcp` package via Git URL: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity`.

## Verification Plan

### Automated Verification
- Run `uv --version` to confirm installation.
- Check `Packages/manifest.json` in the Unity project to confirm `com.coplaydev.unity-mcp` is added.
