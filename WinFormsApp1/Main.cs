using System.Diagnostics;
using System.Windows.Automation;

namespace SimpleLogUploader {
    public partial class Main : Form {

        private string selectedFolder = string.Empty;
        private string largestFile = string.Empty;
        private string accessToken = string.Empty;
        private string uploaderPath = string.Empty;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const string DefaultUploaderPath = @"C:\Users\sakul\AppData\Local\Programs\Warcraft Logs Uploader\Warcraft Logs Uploader.exe";

        public Main() {
            InitializeComponent();

            btnSelectFolder.Click += new EventHandler(BrowseFolder_Click);
            btnLogin.Click += new EventHandler(Login_Click);
            btnUpload.Click += new EventHandler(Upload_Click);
            btnUploaderPath.Click += new EventHandler(BrowseUploaderPath_Click);

            _ = LoadTokenOnStartup();

            // Load saved folder
            selectedFolder = Properties.Settings.Default.FolderPath ?? string.Empty;
            if (!string.IsNullOrEmpty(selectedFolder)) {
                txtFolderPath.Text = selectedFolder;
                largestFile = GetLargestFile(selectedFolder);
                Log($"Loaded saved folder: {selectedFolder}");
            }

            // Load saved uploader path
            uploaderPath = Properties.Settings.Default.UploaderPath ?? DefaultUploaderPath;
            txtUploaderPath.Text = uploaderPath;
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

            largestFile = GetLargestFile(selectedFolder);

            if (string.IsNullOrEmpty(largestFile)) {
                Log("No files found in selected folder.");
                return;
            }

            Log($"Uploading largest file: {Path.GetFileName(largestFile)}");
            await UploadLog(largestFile);
        }

        private void BrowseFolder_Click(object sender, EventArgs e) {
            using FolderBrowserDialog dialog = new FolderBrowserDialog {
                Description = "Select a folder",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == DialogResult.OK) {
                selectedFolder = dialog.SelectedPath;
                largestFile = GetLargestFile(selectedFolder);
                txtFolderPath.Text = selectedFolder;
                Log($"Selected folder: {selectedFolder}");
                Log($"Largest file: {Path.GetFileName(largestFile)}");

                Properties.Settings.Default.FolderPath = selectedFolder;
                Properties.Settings.Default.Save();
            }
        }

        private void BrowseUploaderPath_Click(object sender, EventArgs e) {
            using OpenFileDialog dialog = new OpenFileDialog {
                Title = "Select Warcraft Logs Uploader",
                Filter = "Executable files (*.exe)|*.exe",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == DialogResult.OK) {
                string path = dialog.FileName;

                if (!path.EndsWith(@"Warcraft Logs Uploader\Warcraft Logs Uploader.exe",
                        StringComparison.OrdinalIgnoreCase)) {
                    Log("Invalid selection. Please select 'Warcraft Logs Uploader\\Warcraft Logs Uploader.exe'.");
                    return;
                }

                uploaderPath = path;
                txtUploaderPath.Text = path;
                Log($"Uploader path set: {path}");

                Properties.Settings.Default.UploaderPath = path;
                Properties.Settings.Default.Save();
            }
        }

        private string GetLargestFile(string folderPath) {
            string[] files = Directory.GetFiles(folderPath);

            if (files.Length == 0)
                return string.Empty;

            return files
                .OrderByDescending(f => new FileInfo(f).Length)
                .First();
        }

        private async void Login_Click(object sender, EventArgs e) {
            string savedClientId = Properties.Settings.Default.ClientId ?? AuthHelper.ClientId;
            string savedRedirect = Properties.Settings.Default.RedirectUrl ?? AuthHelper.RedirectUrl;

            using var settingsForm = new SettingsForm(savedClientId, savedRedirect, uploaderPath, selectedFolder);
            if (settingsForm.ShowDialog(this) != DialogResult.OK) return;

            Properties.Settings.Default.ClientId = settingsForm.ClientId;
            Properties.Settings.Default.RedirectUrl = settingsForm.RedirectUrl;
            Properties.Settings.Default.Save();

            AuthHelper.ClientId = settingsForm.ClientId;
            AuthHelper.RedirectUrl = settingsForm.RedirectUrl;

            accessToken = await AuthHelper.GetAccessToken();
            Log("Logged in successfully!");
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
                Log($"Process '{appName}' already running: {existing != null}");

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
                Log($"Window found: '{window.Current.Name}'");

                AutomationElement? uploadTab = null;
                Task findTab = Task.Run(() => {
                    uploadTab = window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Upload a Log"));
                });

                bool tabFound = await Task.WhenAny(findTab, Task.Delay(10000)) == findTab;
                Log($"Upload a Log tab found: {tabFound && uploadTab != null}");

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
                Log($"Choose button found: {chooseFound && chooseButton != null}");

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

                Log(fileDialog == null ? "File dialog not found." : $"File dialog found: '{fileDialog.Current.Name}'");

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

                Log(window == null ? "Could not re-find uploader window." : $"Re-found window: '{window.Current.Name}'");

                if (window == null) return;

                AutomationElement? goButton = null;
                Task findGo = Task.Run(() => {
                    goButton = window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Go!"));
                });

                bool goFound = await Task.WhenAny(findGo, Task.Delay(10000)) == findGo;
                Log($"Go button found: {goFound && goButton != null}");

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
                Log($"Error: {ex.Message}");
            }
        }
    }
}