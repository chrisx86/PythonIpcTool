# Python IPC Tool for WPF

A modern, intuitive WPF desktop application designed to facilitate Inter-Process Communication (IPC) with Python scripts. This tool is perfect for developers, data scientists, and engineers who need to integrate powerful Python functionalities (like data analysis, machine learning models, or automation scripts) into a .NET environment with a user-friendly graphical interface.

The application allows users to send data (in JSON format) to a Python script, execute it, and receive the processed results back in real-time, all through a clean and responsive UI.

## ✨ Features

- **Dual IPC Modes:** Seamlessly switch between two robust communication methods:
  - **Standard I/O:** A classic and reliable method using `stdin`, `stdout`, and `stderr`.
  - **Local Sockets:** A high-performance TCP socket-based communication for more complex or persistent interactions.
- **Profile Management:** Create, save, and manage multiple script profiles. Each profile stores the Python interpreter path, script path, and preferred IPC mode, allowing for quick switching between different tasks.
- **Modern & Responsive UI:** Built with WPF and the MahApps.Metro library, featuring:
  - **Dark/Light Mode:** Instantly switch between themes to suit your preference.
  - **Structured Logging:** A detailed, color-coded log viewer to monitor application status and Python script outputs in real-time.
  - **Asynchronous Operations:** The UI remains fully responsive even while executing long-running Python scripts.
- **Robust Error Handling:** Comprehensive error handling for invalid paths, script execution errors, and invalid JSON, preventing crashes and providing clear feedback.
- **Safety & Convenience:**
  - **Virtual Environment Detection:** Automatically detects if the selected Python interpreter is part of a `venv` or `conda` environment.
  - **Cancellation Support:** Abort long-running scripts at any time with a dedicated "Cancel" button.
- **Configuration Persistence:** All your profiles and theme settings are automatically saved and reloaded between sessions.

## 🛠️ System Requirements

- **Operating System:** Windows 10 / 11
- **.NET Runtime:** .NET 8 (or later) Desktop Runtime. (If using the self-contained version, this is not required).
- **Python:** Python 3.7 or newer. It's recommended to have Python added to your system's PATH environment variable.

## 🚀 Getting Started

1.  **Download:** Grab the latest release from the [Releases](https://github.com/your-username/your-repo/releases) page.
2.  **Unzip:** Extract the contents of the ZIP file to a location of your choice.
3.  **Run:** Execute `PythonIpcTool.exe`.

## 📖 How to Use

### 1. Configure a Profile

The application's functionality is centered around **Profiles**. A profile stores all the necessary settings to run a specific script.

1.  **Initial Profile:** The application starts with a "New Profile".
2.  **Profile Name:** Give your profile a descriptive name (e.g., "Data Analysis Script").
3.  **Python Interpreter Path:**
    -   Click **"Browse..."** to select your `python.exe`.
    -   This can be a global Python installation (e.g., `C:\Python39\python.exe`) or one from a virtual environment (e.g., `C:\MyProject\venv\Scripts\python.exe`). The tool will indicate if a virtual environment is detected.
    -   Alternatively, if `python` is in your system PATH, you can simply leave it as `python`.
4.  **Python Script Path:**
    -   Click **"Browse..."** to select the Python script you want to execute. A sample script, `simple_processor.py`, is included in the `PythonScripts` folder.
5.  **IPC Communication Mode:**
    -   **Standard I/O:** Recommended for simple, one-off tasks.
    -   **Local Socket:** Better for more complex or continuous communication scenarios.
6.  **Add/Remove Profiles:** Use the **`+`** and **`-`** buttons to add new profiles or remove the currently selected one. All changes are saved automatically.

### 2. Prepare Your Python Script

Your Python script must be written to handle IPC. It should:

-   Read a single line of JSON data from `sys.stdin` (for Standard I/O) or a socket.
-   Process the data.
-   Write a single line of JSON data back to `sys.stdout` (for Standard I/O) or the socket.
-   Write any errors to `sys.stderr`.

See the included `simple_processor.py` for a complete example.

### 3. Execute the Script

1.  **Input Data:** Enter the data you want to send to the script in the **Input Data (JSON)** text area. It must be valid JSON.
2.  **Execute:** Click the **"Execute Script"** button.
3.  **View Output:** The processed result from the Python script will appear in the **Output Data** panel.
4.  **Monitor Logs:** The entire process, including status messages, script output (`stdout`), and errors (`stderr`), will be logged in the **Status & Log** panel.

## 🏗️ For Developers (Building from Source)

### Technology Stack

- **Frontend:** C# 12, .NET 8, WPF
- **MVVM Framework:** CommunityToolkit.Mvvm (Source Generator based)
- **UI Toolkit:** MahApps.Metro
- **Logging:** Serilog

### Building the Project

1.  Clone the repository:
    ```bash
    git clone https://github.com/your-username/your-repo.git
    ```
2.  Open `PythonIpcTool.sln` in Visual Studio 2022.
3.  Ensure you have the .NET 8 SDK installed.
4.  Restore the NuGet packages.
5.  Build the solution (the build process will automatically run the MVVM source generators).
6.  Run the `PythonIpcTool` project.
