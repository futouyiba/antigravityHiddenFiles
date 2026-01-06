# Agent Recovery Guide

If Antigravity (the agent) appears to be stuck or the "Stop" button is unresponsive, follow these steps to reset the state:

## 1. Refresh the Workspace
The simplest way to fix UI sync issues (like an unresponsive "Stop" button) is to refresh your browser or IDE:
- **VS Code**: Run the command `Developer: Reload Window` from the Command Palette (`Ctrl+Shift+P`).
- **Browser**: Simply refresh the page.

## 2. Force Stop via Terminal
If a background command is still running and blocking progress, you can kill the process manually:
- Open a new terminal.
- Identify the process and kill it:
  ```powershell
  # Windows (PowerShell)
  Get-Process git | Stop-Process -Force
  ```

## 3. Clear Agent Task State
If the agent is stuck in a loop or a "deadlock":
- In the task view, look for a "Reset" or "Stop Agent" option if available.
- If not, reloading the window (Step 1) is usually sufficient to clear the transient agent state.

## 4. Check for Lock Files
If the agent cannot run new commands because of a previous failure:
- Delete `.git/index.lock` if it exists.
  ```powershell
  Remove-Item .git/index.lock -Force
  ```

> [!TIP]
> This "hanging" often happens during high I/O operations like `git lfs migrate`. For large repositories, consider running these commands in a separate terminal instead of via the agent interface to avoid UI timeouts.
