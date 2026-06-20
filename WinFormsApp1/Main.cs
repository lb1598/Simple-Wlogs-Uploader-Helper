using System.Diagnostics;
using System.Windows.Automation;

namespace SimpleLogUploader {
    public partial class Main : Form {

        private string selectedFolder = string.Empty;
        private string targetFile = string.Empty;
        private string accessToken = string.Empty;
        private string uploaderPath = string.Empty;
        private string fileMode = "Largest";

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const string DefaultUploaderPath = @"C:\Users\sakul\AppData\Local\Programs\Warcraft Logs Uploader\Warcraft Logs Uploader.exe";

        public Main() {
            InitializeComponent();

            btnLogin.Click += new EventHandler(Login_Click);
            btnUpload.Click += new EventHandler(Upload_Click);
            btnDelete.Click += new EventHandler(Delete_Click);
            btnAutomate.Click += new EventHandler(Automate_Click);

            if (DesignMode) return;

            _ = LoadTokenOnStartup();

            selectedFolder = Properties.Settings.Default.FolderPath ?? string.Empty;
            if (!string.IsNullOrEmpty(selectedFolder)) {
                targetFile = GetTargetFile(selectedFolder);
                Log("Loaded saved folder: " + selectedFolder);
            }

            uploaderPath = Properties.Settings.Default.UploaderPath ?? DefaultUploaderPath;
            fileMode = Properties.Settings.Default.FileMode ?? "Largest";
        }

        private void Log(string message) {
            lblDebug.AppendText(Environment.NewLine + message);
            lblDebug.ScrollToCaret();
        }

        /// <summary>
        /// Polls for a named button by re-finding the uploader window on each attempt.
        /// Returns the element if found, null on timeout or cancellation.
        /// </summary>
        private async Task<AutomationElement?> WaitForButton(
            string buttonName,
            TimeSpan interval,
            TimeSpan timeout,
            CancellationToken token) {

            DateTime deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline) {
                if (token.IsCancellationRequested) return null;

                AutomationElement? found = await Task.Run(() => {
                    AutomationElement? window = FindUploaderWindow();
                    if (window == null) return null;
                    try {
                        return window.FindFirst(TreeScope.Descendants,
                            new PropertyCondition(AutomationElement.NameProperty, buttonName));
                    } catch (ElementNotAvailableException) {
                        return null;
                    }
                });

                if (found != null) return found;

                try {
                    await Task.Delay(interval, token);
                } catch (TaskCanceledException) {
                    return null;
                }
            }

            return null;
        }

        private AutomationElement? FindUploaderWindow() {
            try {
                foreach (AutomationElement el in AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                    try {
                        if (el.Current.Name.Contains("Warcraft Logs"))
                            return el;
                    } catch (ElementNotAvailableException) {
                        continue;
                    }
                }
            } catch (ElementNotAvailableException) { }
            return null;
        }

        private async Task<AutomationElement?> WaitForUploaderWindow(CancellationToken token, int attempts = 30, int delayMs = 200) {
            for (int i = 0; i < attempts; i++) {
                await Task.Delay(delayMs, token);
                token.ThrowIfCancellationRequested();
                AutomationElement? window = await Task.Run(FindUploaderWindow);
                if (window != null) return window;
            }
            return null;
        }

        private async Task LoadTokenOnStartup() {
            string savedClientId = Properties.Settings.Default.ClientId ?? string.Empty;
            string savedRedirect = Properties.Settings.Default.RedirectUrl ?? string.Empty;

            if (string.IsNullOrEmpty(savedClientId) || string.IsNullOrEmpty(savedRedirect)) {
                Log("No credentials saved. Click Login to set up.");
                return;
            }

            AuthHelper.ClientId = savedClientId;
            AuthHelper.RedirectUrl = savedRedirect;

            accessToken = await AuthHelper.GetOrRefreshToken();
            if (!string.IsNullOrEmpty(accessToken))
                Log("Logged in automatically!");
        }

        private async void Upload_Click(object sender, EventArgs e) {
            if (string.IsNullOrEmpty(selectedFolder)) {
                Log("Please select a folder first.");
                return;
            }

            targetFile = GetTargetFile(selectedFolder);

            if (string.IsNullOrEmpty(targetFile)) {
                Log("No files found in selected folder.");
                return;
            }

            Log("Uploading " + fileMode.ToLower() + " file: " + Path.GetFileName(targetFile));
            await UploadLog(targetFile);
        }

        private string GetTargetFile(string folderPath) => fileMode switch {
            "Newest" => GetNewestFile(folderPath),
            "Oldest" => GetOldestFile(folderPath),
            _ => GetLargestFile(folderPath)
        };

        private string GetLargestFile(string folderPath) {
            string[] files = Directory.GetFiles(folderPath, "*.txt");
            if (files.Length == 0) return string.Empty;
            return files.MaxBy(f => new FileInfo(f).Length) ?? string.Empty;
        }

        private string GetOldestFile(string folderPath) {
            string[] files = Directory.GetFiles(folderPath, "*.txt");
            if (files.Length == 0) return string.Empty;
            return files.MinBy(f => new FileInfo(f).LastWriteTime) ?? string.Empty;
        }

        private string GetNewestFile(string folderPath) {
            string[] files = Directory.GetFiles(folderPath, "*.txt");
            if (files.Length == 0) return string.Empty;
            return files.MaxBy(f => new FileInfo(f).LastWriteTime) ?? string.Empty;
        }

        private async void Login_Click(object sender, EventArgs e) {
            string savedClientId = Properties.Settings.Default.ClientId ?? AuthHelper.ClientId;
            string savedRedirect = Properties.Settings.Default.RedirectUrl ?? AuthHelper.RedirectUrl;

            using var settingsForm = new SettingsForm(savedClientId, savedRedirect, uploaderPath, selectedFolder, fileMode);
            if (settingsForm.ShowDialog(this) != DialogResult.OK) return;

            Properties.Settings.Default.ClientId = settingsForm.ClientId;
            Properties.Settings.Default.RedirectUrl = settingsForm.RedirectUrl;
            Properties.Settings.Default.UploaderPath = settingsForm.UploaderPath;
            Properties.Settings.Default.FolderPath = settingsForm.FolderPath;
            Properties.Settings.Default.FileMode = settingsForm.FileMode;
            Properties.Settings.Default.Save();

            AuthHelper.ClientId = settingsForm.ClientId;
            AuthHelper.RedirectUrl = settingsForm.RedirectUrl;
            uploaderPath = settingsForm.UploaderPath;
            selectedFolder = settingsForm.FolderPath;
            fileMode = settingsForm.FileMode;

            if (!string.IsNullOrEmpty(selectedFolder)) {
                targetFile = GetTargetFile(selectedFolder);
                Log("Folder set: " + selectedFolder);
            }

            if (string.IsNullOrWhiteSpace(settingsForm.ClientId) || string.IsNullOrWhiteSpace(settingsForm.RedirectUrl)) {
                Log("Settings saved. Fill in Client ID and Redirect URL to log in.");
                return;
            }

            accessToken = await AuthHelper.GetAccessToken();
            Log("Logged in successfully!");
        }

        private async void Delete_Click(object sender, EventArgs e) {
            Log("Starting delete sequence...");
            await DeleteLog();
        }

        private async Task<bool> DeleteLog(CancellationToken token = default) {
            try {
                if (!File.Exists(uploaderPath)) {
                    Log("Warcraft Logs Uploader not found. Please set the uploader path.");
                    return false;
                }

                string appName = Path.GetFileNameWithoutExtension(uploaderPath);
                Process? existing = Process.GetProcessesByName(appName)
                    .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
                Log("Process '" + appName + "' already running: " + (existing != null));

                if (existing == null) {
                    Log("Uploader is not running. Nothing to delete.");
                    return false;
                }

                SetForegroundWindow(existing.MainWindowHandle);
                Log("Brought existing window to foreground.");

                // Step 1: Delete Now
                AutomationElement? deleteButton = await WaitForButton("Delete Now",
                    interval: TimeSpan.FromMilliseconds(500),
                    timeout: TimeSpan.FromSeconds(10),
                    token);
                Log("Delete Now button found: " + (deleteButton != null));
                if (deleteButton == null) { Log("Delete Now button not found or timed out."); return false; }

                await Task.Run(() => (deleteButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern)?.Invoke());
                await Task.Delay(150, token);
                token.ThrowIfCancellationRequested();
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Delete Now button.");

                // Step 2: Yes
                AutomationElement? yesButton = await WaitForButton("Yes",
                    interval: TimeSpan.FromMilliseconds(500),
                    timeout: TimeSpan.FromSeconds(10),
                    token);
                Log("Yes button found: " + (yesButton != null));
                if (yesButton == null) { Log("Yes button not found or timed out."); return false; }

                await Task.Run(() => (yesButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern)?.Invoke());
                await Task.Delay(150, token);
                token.ThrowIfCancellationRequested();
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Yes button.");

                // Step 3: Done
                AutomationElement? doneButton = await WaitForButton("Done",
                    interval: TimeSpan.FromMilliseconds(500),
                    timeout: TimeSpan.FromSeconds(10),
                    token);
                Log("Done button found: " + (doneButton != null));
                if (doneButton == null) { Log("Done button not found or timed out."); return false; }

                await Task.Run(() => (doneButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern)?.Invoke());
                await Task.Delay(150, token);
                token.ThrowIfCancellationRequested();
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Done button. Delete sequence complete!");

                return true;

            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Log("Error: " + ex.Message);
                return false;
            }
        }

        private async Task<bool> UploadLog(string filePath, CancellationToken token = default) {
            if (string.IsNullOrEmpty(selectedFolder)) return false;

            try {
                if (!File.Exists(uploaderPath)) {
                    Log("Warcraft Logs Uploader not found. Please set the uploader path.");
                    return false;
                }

                string appName = Path.GetFileNameWithoutExtension(uploaderPath);
                Process? existing = Process.GetProcessesByName(appName)
                    .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
                Log("Process '" + appName + "' already running: " + (existing != null));

                if (existing != null) {
                    SetForegroundWindow(existing.MainWindowHandle);
                    Log("Brought existing window to foreground.");
                } else {
                    Process? proc = Process.Start(new ProcessStartInfo {
                        FileName = uploaderPath,
                        UseShellExecute = true
                    });
                    proc?.WaitForInputIdle();
                    await Task.Delay(4000, token);
                    token.ThrowIfCancellationRequested();
                    Log("App launched and waited 4 seconds.");
                }

                AutomationElement? window = await WaitForUploaderWindow(token);
                if (window == null) { Log("Could not find uploader window."); return false; }
                Log("Window found: '" + window.Current.Name + "'");

                // Navigate to Upload tab
                AutomationElement? uploadTab = await WaitForButton("Upload a Log",
                    interval: TimeSpan.FromMilliseconds(500),
                    timeout: TimeSpan.FromSeconds(10),
                    token);
                Log("Upload a Log tab found: " + (uploadTab != null));

                if (uploadTab != null) {
                    await Task.Run(() => (uploadTab.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern)?.Invoke());
                    await Task.Delay(150, token);
                    token.ThrowIfCancellationRequested();
                    SendKeys.SendWait(" ");
                    Log("Invoked and sent Space on Upload a Log tab.");
                }

                // Choose file
                AutomationElement? chooseButton = await WaitForButton("Choose...",
                    interval: TimeSpan.FromMilliseconds(500),
                    timeout: TimeSpan.FromSeconds(10),
                    token);
                Log("Choose button found: " + (chooseButton != null));
                if (chooseButton == null) { Log("Choose button not found or timed out."); return false; }

                await Task.Run(() => (chooseButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern)?.Invoke());
                await Task.Delay(150, token);
                token.ThrowIfCancellationRequested();
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Choose button.");

                // Wait for file dialog
                AutomationElement? fileDialog = null;
                for (int i = 0; i < 10; i++) {
                    await Task.Delay(200, token);
                    token.ThrowIfCancellationRequested();
                    try {
                        foreach (AutomationElement el in AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                            try {
                                if (el.Current.ControlType == ControlType.Window &&
                                    !el.Current.Name.Contains("Warcraft Logs") &&
                                    el.Current.Name.Contains("Open")) {
                                    fileDialog = el;
                                    break;
                                }
                            } catch (ElementNotAvailableException) {
                                continue;
                            }
                        }
                    } catch (ElementNotAvailableException) { }
                    if (fileDialog != null) break;
                }

                Log(fileDialog == null ? "File dialog not found." : "File dialog found: '" + fileDialog.Current.Name + "'");
                if (fileDialog == null) return false;

                AutomationElement? fileNameBox = fileDialog.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "1148"));

                if (fileNameBox == null) { Log("File name box not found."); return false; }

                await Task.Run(() => (fileNameBox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern)?.SetValue(Path.GetFileName(filePath)));
                Log("File name set to: " + Path.GetFileName(filePath));

                await Task.Delay(200, token);
                token.ThrowIfCancellationRequested();

                AutomationElement? openButton = fileDialog.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "1"));

                if (openButton == null) { Log("Open button not found."); return false; }

                await Task.Run(() => (openButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern)?.Invoke());
                Log("Open clicked.");

                // Poll for Go button — window re-found internally by WaitForButton
                AutomationElement? goButton = await WaitForButton("Go!",
                    interval: TimeSpan.FromMilliseconds(500),
                    timeout: TimeSpan.FromSeconds(15),
                    token);
                Log("Go button found: " + (goButton != null));
                if (goButton == null) { Log("Go button not found or timed out."); return false; }

                await Task.Run(() => (goButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern)?.Invoke());
                await Task.Delay(150, token);
                token.ThrowIfCancellationRequested();
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Go button. Done!");

                return true;

            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Log("Error: " + ex.Message);
                return false;
            }
        }

        private void Main_Load(object sender, EventArgs e) {
            //
        }
    }
}