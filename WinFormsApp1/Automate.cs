using System.Windows.Automation;

namespace SimpleLogUploader {
    /// <summary>
    /// Handles the "Auto-Upload" automation loop:
    ///   0. At the start of each cycle, check for a leftover "Done" button and click it if present.
    ///   1. Upload the target log file (invokes Go!).
    ///   2. Poll every 3 seconds for the "Delete Now" button to appear (up to a 2 minute timeout).
    ///   3. Once found, run the delete sequence (Delete Now -> Yes -> Done).
    ///   4. After Done is invoked, start a new Upload cycle. Repeat until toggled off.
    /// </summary>
    public partial class Main {

        private static readonly TimeSpan DeletePollInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan DeletePollTimeout = TimeSpan.FromMinutes(4);

        private CancellationTokenSource? automateCts;
        private bool isAutomating = false;

        private void Automate_Click(object sender, EventArgs e) {
            if (isAutomating) {
                StopAutomation();
            } else {
                StartAutomation();
            }
        }

        private void StartAutomation() {
            if (string.IsNullOrEmpty(selectedFolder)) {
                Log("Please select a folder first.");
                return;
            }

            isAutomating = true;
            automateCts = new CancellationTokenSource();

            btnAutomate.Text = "Stop Auto-Upload";
            btnUpload.Enabled = false;
            btnDelete.Enabled = false;

            Log("Auto-Upload started.");

            _ = RunAutomationLoop(automateCts.Token);
        }

        private void StopAutomation() {
            if (!isAutomating) return;

            Log("Auto-Upload stopping...");
            automateCts?.Cancel();
        }

        private void OnAutomationStopped() {
            isAutomating = false;
            automateCts?.Dispose();
            automateCts = null;

            btnAutomate.Text = "Auto-Upload";
            btnAutomate.Enabled = true;
            btnUpload.Enabled = true;
            btnDelete.Enabled = true;

            Log("Auto-Upload stopped.");
        }



        private async Task RunAutomationLoop(CancellationToken token) {
            try {
                while (!token.IsCancellationRequested) {
                    bool doneCleared = await CheckAndClickDoneButton();
                    if (doneCleared) {
                        Log("[Auto] Found a leftover Done button and clicked it before starting upload.");
                    }

                    targetFile = GetTargetFile(selectedFolder);
                    if (string.IsNullOrEmpty(targetFile)) {
                        Log("No files found in selected folder. Stopping Auto-Upload.");
                        break;
                    }

                    Log("[Auto] Uploading " + fileMode.ToLower() + " file: " + Path.GetFileName(targetFile));
                    bool uploaded = await UploadLog(targetFile, token);
                    if (!uploaded) {
                        Log("[Auto] Upload step failed. Stopping Auto-Upload.");
                        break;
                    }

                    if (token.IsCancellationRequested) {
                        Log("[Auto] Stop requested after upload completed.");
                        break;
                    }

                    Log("[Auto] Waiting for Delete Now button to appear...");
                    bool deleteReady = await WaitForDeleteNowButton(token);
                    if (!deleteReady) {
                        if (token.IsCancellationRequested)
                            Log("[Auto] Stop requested while waiting for Delete Now button.");
                        else
                            Log("[Auto] Timed out waiting for Delete Now button. Stopping Auto-Upload.");
                        break;
                    }

                    Log("[Auto] Delete Now button detected. Starting delete sequence...");
                    bool deleted = await DeleteLog(token);
                    if (!deleted) {
                        Log("[Auto] Delete step failed. Stopping Auto-Upload.");
                        break;
                    }

                    Log("[Auto] Delete sequence complete. Looping back to Upload.");
                }
            } catch (OperationCanceledException) {
                Log("[Auto] Stopped.");
            } catch (Exception ex) {
                Log("[Auto] Unexpected error: " + ex.Message);
            } finally {
                OnAutomationStopped();
            }
        }

        /// <summary>
        /// Polls every 3 seconds for the "Delete Now" button in the uploader window.
        /// Returns true as soon as it's found, false on timeout or cancellation.
        /// </summary>
        private async Task<bool> WaitForDeleteNowButton(CancellationToken token) {
            DateTime deadline = DateTime.UtcNow + DeletePollTimeout;

            while (DateTime.UtcNow < deadline) {
                if (token.IsCancellationRequested) return false;

                if (DeleteNowButtonExists()) return true;

                try {
                    await Task.Delay(DeletePollInterval, token);
                } catch (TaskCanceledException) {
                    return false;
                }
            }

            return false;
        }

        private bool DeleteNowButtonExists() {
            AutomationElement? window = null;
            foreach (AutomationElement el in AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                if (el.Current.Name.Contains("Warcraft") || el.Current.Name.Contains("Logs")) {
                    window = el;
                    break;
                }
            }

            if (window == null) return false;

            AutomationElement? deleteButton = window.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Delete Now"));

            return deleteButton != null;
        }

        /// <summary>
        /// Checks the uploader window for a leftover "Done" button (e.g. from a delete
        /// sequence that completed but wasn't dismissed) and clicks it if present.
        /// Returns true if a Done button was found and invoked, false otherwise.
        /// </summary>
        private async Task<bool> CheckAndClickDoneButton() {
            AutomationElement? window = null;
            foreach (AutomationElement el in AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)) {
                if (el.Current.Name.Contains("Warcraft") || el.Current.Name.Contains("Logs")) {
                    window = el;
                    break;
                }
            }

            if (window == null) return false;

            AutomationElement? doneButton = window.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Done"));

            if (doneButton == null) return false;

            await Task.Run(() => {
                InvokePattern? invoke = doneButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                invoke?.Invoke();
            });
            await Task.Delay(500);
            SendKeys.SendWait(" ");

            return true;
        }
    }
}