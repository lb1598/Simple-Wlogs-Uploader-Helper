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

        private async Task DeleteLog() {
            try {
                if (!File.Exists(uploaderPath)) {
                    Log("Warcraft Logs Uploader not found. Please set the uploader path.");
                    return;
                }

                string appName = Path.GetFileNameWithoutExtension(uploaderPath);
                Process? existing = Process.GetProcessesByName(appName)
                    .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
                Log("Process '" + appName + "' already running: " + (existing != null));

                if (existing == null) {
                    Log("Uploader is not running. Nothing to delete.");
                    return;
                }

                SetForegroundWindow(existing.MainWindowHandle);
                Log("Brought existing window to foreground.");

                AutomationElement? window = null;
                for (int i = 0; i < 30; i++) {
                    await Task.Delay(500);
                    AutomationElement desktop = AutomationElement.RootElement;
                    foreach (AutomationElement el in desktop.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                        if (el.Current.Name.Contains("Warcraft") || el.Current.Name.Contains("Logs")) {
                            window = el;
                            break;
                        }
                    }
                    if (window != null) break;
                }

                if (window == null) {
                    Log("Could not find uploader window.");
                    return;
                }
                Log("Window found: '" + window.Current.Name + "'");

                // Step 1: Delete Now
                AutomationElement? deleteButton = null;
                Task findDelete = Task.Run(() => {
                    deleteButton = window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Delete Now"));
                });

                bool deleteFound = await Task.WhenAny(findDelete, Task.Delay(10000)) == findDelete;
                Log("Delete Now button found: " + (deleteFound && deleteButton != null));

                if (!deleteFound || deleteButton == null) {
                    Log("Delete Now button not found or timed out.");
                    return;
                }

                await Task.Run(() => {
                    InvokePattern? invoke = deleteButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    invoke?.Invoke();
                });
                await Task.Delay(500);
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Delete Now button.");

                await Task.Delay(1500);

                // Step 2: Yes (confirmation)
                window = null;
                foreach (AutomationElement el in AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                    if (el.Current.Name.Contains("Warcraft") || el.Current.Name.Contains("Logs")) {
                        window = el;
                        break;
                    }
                }

                Log(window == null ? "Could not re-find uploader window after Delete Now." : "Re-found window: '" + window.Current.Name + "'");
                if (window == null) return;

                AutomationElement? yesButton = null;
                Task findYes = Task.Run(() => {
                    yesButton = window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Yes"));
                });

                bool yesFound = await Task.WhenAny(findYes, Task.Delay(10000)) == findYes;
                Log("Yes button found: " + (yesFound && yesButton != null));

                if (!yesFound || yesButton == null) {
                    Log("Yes button not found or timed out.");
                    return;
                }

                await Task.Run(() => {
                    InvokePattern? invoke = yesButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    invoke?.Invoke();
                });
                await Task.Delay(500);
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Yes button.");

                await Task.Delay(1500);

                // Step 3: Done
                window = null;
                foreach (AutomationElement el in AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                    if (el.Current.Name.Contains("Warcraft") || el.Current.Name.Contains("Logs")) {
                        window = el;
                        break;
                    }
                }

                Log(window == null ? "Could not re-find uploader window after Yes." : "Re-found window: '" + window.Current.Name + "'");
                if (window == null) return;

                AutomationElement? doneButton = null;
                Task findDone = Task.Run(() => {
                    doneButton = window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Done"));
                });

                bool doneFound = await Task.WhenAny(findDone, Task.Delay(10000)) == findDone;
                Log("Done button found: " + (doneFound && doneButton != null));

                if (!doneFound || doneButton == null) {
                    Log("Done button not found or timed out.");
                    return;
                }

                await Task.Run(() => {
                    InvokePattern? invoke = doneButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    invoke?.Invoke();
                });
                await Task.Delay(500);
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Done button. Delete sequence complete!");

            } catch (Exception ex) {
                Log("Error: " + ex.Message);
            }
        }

        private async Task UploadLog(string filePath) {
            if (string.IsNullOrEmpty(selectedFolder)) return;

            try {
                if (!File.Exists(uploaderPath)) {
                    Log("Warcraft Logs Uploader not found. Please set the uploader path.");
                    return;
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
                    await Task.Delay(8000);
                    Log("App launched and waited 8 seconds.");
                }

                AutomationElement? window = null;
                for (int i = 0; i < 30; i++) {
                    await Task.Delay(500);
                    AutomationElement desktop = AutomationElement.RootElement;
                    foreach (AutomationElement el in desktop.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                        if (el.Current.Name.Contains("Warcraft") || el.Current.Name.Contains("Logs")) {
                            window = el;
                            break;
                        }
                    }
                    if (window != null) break;
                }

                if (window == null) {
                    Log("Could not find uploader window.");
                    return;
                }
                Log("Window found: '" + window.Current.Name + "'");

                AutomationElement? uploadTab = null;
                Task findTab = Task.Run(() => {
                    uploadTab = window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Upload a Log"));
                });

                bool tabFound = await Task.WhenAny(findTab, Task.Delay(10000)) == findTab;
                Log("Upload a Log tab found: " + (tabFound && uploadTab != null));

                if (tabFound && uploadTab != null) {
                    await Task.Run(() => {
                        InvokePattern? invoke = uploadTab.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        invoke?.Invoke();
                    });
                    await Task.Delay(500);
                    SendKeys.SendWait(" ");
                    Log("Invoked and sent Space on Upload a Log tab.");
                    await Task.Delay(2000);
                }

                AutomationElement? chooseButton = null;
                Task findChoose = Task.Run(() => {
                    chooseButton = window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Choose..."));
                });

                bool chooseFound = await Task.WhenAny(findChoose, Task.Delay(10000)) == findChoose;
                Log("Choose button found: " + (chooseFound && chooseButton != null));

                if (!chooseFound || chooseButton == null) {
                    Log("Choose button not found or timed out.");
                    return;
                }

                await Task.Run(() => {
                    InvokePattern? invoke = chooseButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    invoke?.Invoke();
                });
                await Task.Delay(500);
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Choose button.");

                await Task.Delay(2000);

                AutomationElement? fileDialog = null;
                for (int i = 0; i < 10; i++) {
                    await Task.Delay(500);
                    foreach (AutomationElement el in AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                        if (el.Current.ControlType == ControlType.Window &&
                            el.Current.Name != "Warcraft Logs" &&
                            el.Current.Name.Contains("Open")) {
                            fileDialog = el;
                            break;
                        }
                    }
                    if (fileDialog != null) break;
                }

                Log(fileDialog == null ? "File dialog not found." : "File dialog found: '" + fileDialog.Current.Name + "'");

                if (fileDialog == null) return;

                SetForegroundWindow(new IntPtr(fileDialog.Current.NativeWindowHandle));
                await Task.Delay(500);
                SendKeys.SendWait(Path.GetFileName(filePath));
                await Task.Delay(500);
                SendKeys.SendWait("{ENTER}");
                Log("File name typed and Enter pressed.");

                await Task.Delay(2000);

                window = null;
                foreach (AutomationElement el in AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                    if (el.Current.Name.Contains("Warcraft") || el.Current.Name.Contains("Logs")) {
                        window = el;
                        break;
                    }
                }

                Log(window == null ? "Could not re-find uploader window." : "Re-found window: '" + window.Current.Name + "'");

                if (window == null) return;

                AutomationElement? goButton = null;
                Task findGo = Task.Run(() => {
                    goButton = window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Go!"));
                });

                bool goFound = await Task.WhenAny(findGo, Task.Delay(10000)) == findGo;
                Log("Go button found: " + (goFound && goButton != null));

                if (!goFound || goButton == null) {
                    Log("Go button not found or timed out.");
                    return;
                }

                await Task.Run(() => {
                    InvokePattern? invoke = goButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    invoke?.Invoke();
                });
                await Task.Delay(500);
                SendKeys.SendWait(" ");
                Log("Invoked and sent Space on Go button. Done!");

            } catch (Exception ex) {
                Log("Error: " + ex.Message);
            }
        }

        private void Main_Load(object sender, EventArgs e) {
            //
        }
    }
}