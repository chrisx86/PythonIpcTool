# Python IPC Tool for WPF

A modern, intuitive WPF desktop application designed to facilitate Inter-Process Communication (IPC) with Python scripts. This tool is perfect for developers, data scientists, and engineers who need to integrate powerful Python functionalities (like data analysis, machine learning models, or automation scripts) into a .NET environment with a user-friendly graphical interface.

The application allows users to send data (in JSON format) to a Python script, execute it, and receive the processed results back in real-time, all through a clean and responsive UI.

## ✨ Features

- **Dual IPC Modes:** Seamlessly switch between two robust communication methods:
  - **Standard I/O:** A classic and reliable method using `stdin`, `stdout`, and `stderr`, ideal for one-off, stateless tasks.
  - **Local Sockets:** A high-performance TCP socket-based communication for persistent, stateful, and streaming interactions.
- **Profile Management:** Create, save, and manage multiple script profiles. Each profile stores the Python interpreter path, script path, preferred IPC mode, and even default input data, allowing for quick switching between different tasks.
- **Modern & Responsive UI:** Built with WPF and the MahApps.Metro library, featuring:
  - **Dark/Light Mode:** Instantly switch between themes to suit your preference.
  - **Structured Logging:** A detailed, color-coded log viewer to monitor application status and Python script outputs in real-time.
  - **Asynchronous Operations:** The UI remains fully responsive even while executing long-running Python scripts.
- **Robust Error Handling:** Comprehensive error handling for invalid paths, script execution errors, and invalid JSON, preventing crashes and providing clear feedback.
- **Safety & Convenience:**
  - **Virtual Environment Detection:** Automatically detects if the selected Python interpreter is part of a `venv` or `conda` environment.
  - **Dependency Management:** A built-in command to install dependencies from a `requirements.txt` file directly into the selected virtual environment.
  - **Cancellation Support:** Abort long-running scripts at any time with a dedicated "Cancel" button.
- **Configuration Persistence:** All your profiles and theme settings are automatically saved and reloaded between sessions.

## 🛠️ System Requirements

- **Operating System:** Windows 10 / 11
- **.NET Runtime:** .NET 8 (or later) Desktop Runtime. (If using the self-contained version, this is not required).
- **Python:** Python 3.7 or newer. It's recommended to have Python added to your system's PATH environment variable.

## 📖 How to Use

### Direct Execution Mode (For Quick Tests)

1.  **Set Paths:** In the **"Current Configuration"** section, select your Python interpreter (`python.exe`) and the Python script (`.py`) you wish to run.
2.  **Choose Mode:** Select either **Standard I/O** or **Local Socket** as the communication mode.
3.  **Enter Input:** In the **"Input Data (JSON)"** panel, type or paste the JSON data you want to send to your script.
4.  **Execute:** Click the **"Execute Script"** button. The output will appear in the **"Output Data"** panel, and the execution process will be logged in the **"Status & Log"** panel.

### Profile Management (For Recurring Tasks)

1.  **Save a Profile:** After setting up a configuration you want to reuse, click the **Save (💾)** icon in the **"Saved Profiles"** section. You'll be prompted to give your profile a name. This will save the interpreter path, script path, IPC mode, and the current input data.
2.  **Load a Profile:** Simply select a profile from the dropdown list. This will instantly load all its saved settings into the "Current Configuration" and "Input Data" areas, ready for execution.
3.  **Remove a Profile:** Select a profile from the dropdown and click the **Remove (-)** icon.

### Python Script Guidelines

Your Python script must be written to handle IPC.

#### For Standard I/O:
- Read a **single line** of JSON data from `sys.stdin`.
- Perform the task.
- Write a **single line** of JSON data back to `sys.stdout`.
- Write any errors to `sys.stderr`.
- The script will terminate after one execution.

```python
# stdio_example.py
import sys, json
input_data = json.loads(sys.stdin.readline())
result = {"processed": input_data.get("value", "") + "!"}
sys.stdout.write(json.dumps(result) + '\n')
sys.stdout.flush()
```

#### For Local Socket:
- The script will be started with `socket <port>` as command-line arguments.
- Connect to `localhost` on the given port.
- Enter a `while True` loop to continuously read newline-terminated messages from the socket.
- Process each message and write a newline-terminated response back to the socket.

```python
# socket_example.py
import sys, json, socket
port = int(sys.argv[2])
with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    s.connect(('localhost', port))
    reader = s.makefile('r'); writer = s.makefile('w')
    while True:
        line = reader.readline()
        if not line: break
        # ... process line and write response ...
```

Check the `PythonScripts` folder in the release for more detailed examples.

## 🏗️ For Developers (Building from Source)

### Technology Stack

- **Frontend:** C# 12, .NET 8, WPF
- **MVVM Framework:** CommunityToolkit.Mvvm (Source Generator based)
- **UI Toolkit:** MahApps.Metro
- **Logging:** Serilog

### Building the Project

1.  Clone the repository:
2.  Open `PythonIpcTool.sln` in Visual Studio 2022 (or later).
3.  Ensure you have the .NET 8 SDK installed.
4.  Restore the NuGet packages.
5.  Build the solution (the build process will automatically run the MVVM source generators).
6.  Run the `PythonIpcTool` project.
