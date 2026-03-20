# Modern Windows Message Box Generator (CLI & UI)

**Purpose:** Generates a customizable "Task Dialog" box - a more modern version of the old Windows message box. Now includes a powerful CLI helper for integration into scripts and automations.

## 🏗️ Project Structure
The project has been refactored into three main components:
- **Common**: Shared logic and data structures used by both CLI and UI.
- **CLI (`msgbox.exe`)**: A command-line utility for triggering notifications with callback support and advanced timing.
- **UI (`msgboxui.exe`)**: The original graphical generator for designing and previewing dialogs.

## 🚀 CLI Usage (`msgbox.exe`)
The CLI version supports both modern `TaskDialog` and classic `MessageBox` modes.

### Basic Syntax
```powershell
msgbox.exe --title "Notification" --message "Hello World"
```

### Advanced Parameters
| Parameter | Shorthand | Description |
| :--- | :--- | :--- |
| `--heading` | `-h` | Set the dialog's main heading text. |
| `--footer` | `-f` | Set the footnote text at the bottom. |
| `--details` | `-d` | Set text for the "Show Details" expanded section. |
| `--checkbox`| `-x` | Display a verification checkbox with the given label. |
| `--type` | `-y` | Button set (`ok`, `okcancel`, `yesno`, `retrycancel`, etc.). |
| `--icon` | `-i` | Icon (`info`, `warning`, `error`, `shield`, `shieldred`, etc.). |
| `--timeout` | `-o` | Auto-close after `N` milliseconds. |
| `--flash` | `-fl`| Flash the window in the taskbar until active. |
| `--ding` | `-dg`| Play a system alert sound on appearance. |
| `--toast` | `-ts`| Show a native Windows Toast notification. |
| `--xsoverlay`| `-xs` | Mirror notification to XSOverlay (VR). |
| `--ovrtoolkit`| `-ov`| Mirror notification to OVR Toolkit (VR). |
| `--classic` | `-c` | Fallback to the classic Windows `MessageBox`. |
| `--callback`| `-cb`| HTTP GET URL to call on dismissal. |

### 🔗 Callback Placeholders
When using `--callback`, these placeholders are automatically replaced in the URL:
- `{ret_str}`: The text of the button clicked (e.g., `OK`, `Yes`, `Timeout`).
- `{ret_int}`: Standard Windows Dialog ID (e.g., `1` for OK, `2` for Cancel, `32000` for Timeout).
- `{ret_desc}`: A descriptive sentence of the result.
- `{cb_checked}`: `true`/`false` status of the verification checkbox.
- `{cb_text}`: The label text of the checkbox.
- `{time_started}`: Unix timestamp when the notification appeared.
- `{time_finished}`: Unix timestamp when the notification was closed/timed out.

## 🛠️ How to Compile
Requires .NET 9.0 SDK.

### CLI (Single File)
```powershell
dotnet publish Source/CLI/Modern-Windows-Message-Box-Generator.CLI.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

### UI (Single File)
```powershell
dotnet publish Source/UI/Modern-Windows-Message-Box-Generator.UI.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

## Credits
Based on the original [Modern-Windows-Message-Box-Generator](https://github.com/ThioJoe/Modern-Windows-Message-Box-Generator) by ThioJoe.
Modified by Bluscream to add full CLI support and automated callbacks.
