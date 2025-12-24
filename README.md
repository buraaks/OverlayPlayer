# 🎬 OverlayPlayer

OverlayPlayer is a transparent, borderless, and click-through media player that stays on top of your desktop. It supports GIF and Video files, perfect for streamers or anyone who wants stylish animations on their desktop.

## ✨ Features

-   **Always on Top:** Stays above other windows.
-   **Transparency & Borderless:** No frames and a fully transparent background.
-   **Click-Through:** Interaction passes through the animation as if it's not there.
-   **Auto-Positioning:** Automatically docks to the bottom-left corner of the screen.
-   **Real-time Settings:** Change opacity, size, volume, and more instantly without saving.
-   **Slideshow Mode:** Automatically cycle through media in a folder with customizable intervals.
-   **Multi-Language Support:** Fully localized in English and Turkish.
-   **Wide Format Support:** Supports `.gif`, `.png`, `.jpg`, `.jpeg`, `.bmp`, `.mp4`, `.avi`, `.mov`, `.wmv`.
-   **Tray Control:** Access the modern Settings window or exit via the system tray.
-   **Settings Persistence:** Remembers your preferences automatically.
-   **Interactive Mode:** Unlock position to drag and resize the player manually.
-   **Reset to Defaults:** Easily restore original settings with a single click.
-   **Run at Startup:** Option to automatically launch with Windows.

## 🚀 Installation & Usage

### Using the Ready Version
1.  Download the latest `OverlayPlayer.exe` from [Releases](https://github.com/buraaks/OverlayPlayer/releases).
2.  Run the executable.
3.  Select a GIF or Video from the file selection screen.

### Building from Source
If you want to build the project yourself:
1.  Clone the repository: `git clone https://github.com/buraaks/OverlayPlayer.git`
2.  Ensure you have `.NET 8 SDK` installed.
3.  Open a terminal in the project folder and run: `powershell ./publish.ps1`
4.  Your single-file `.exe` will be ready in the `Publish` folder.

## 🛠️ Technologies Used
-   **C# / WPF** (.NET 8)
-   **WPF-Animated-Gif** (for GIF playback support)
-   **Windows API (User32.dll)** (for click-through and window management)

## 📝 License
This project is licensed under the MIT License. Feel free to use and develop it as you wish.

---
*Developed by: [Burak](https://github.com/buraaks)*
