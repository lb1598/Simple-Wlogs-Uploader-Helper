using System.Diagnostics;
using System.Windows.Automation;

namespace SimpleLogUploader {
    /// <summary>
    /// Handles the "Auto-Upload" automation loop:
    ///   0. At the start of each cycle, check for a leftover "Done" button and click it if present.
    ///   1. Upload the target log file (invokes Go!).
    ///   2. Poll every 3 seconds for the "Delete Now" button to appear (up to a 4 minute timeout).
    ///   3. Once found, run the delete sequence (Delete Now -> Yes -> Done).
    ///   4. After Done is invoked, start a new Upload cycle. Repeat until toggled off.
    /// </summary>
    public partial class Main {

        private static readonly TimeSpan DeletePollInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan DeletePollTimeout = TimeSpan.FromMinutes(4);

        private CancellationTokenSource? automateCts;
        private bool isAutomating = false;

        // ── Timing helper ────────────────────────────────────────────────────────
        /// <summary>
        /// Runs <paramref name="action"/>, logs elapsed time with <paramref name="label"/>,
        /// and returns whatever the action returns.
        /// </summary>
        private async Task<T> Timed<T>(string label, Func<Task<T>> action) {
            var sw = Stopwatch.StartNew();
            try {
                T result = await action();
                sw.Stop();
                Log($"[Timing] {label}: {sw.ElapsedMilliseconds} ms");
                return result;
            } catch {
                sw.Stop();
                Log($"[Timing] {label}: {sw.ElapsedMilliseconds} ms (threw exception)");
                throw;
            }
        }

        private async Task Timed(string label, Func<Task> action) {
            var sw = Stopwatch.StartNew();
            try {
                await action();
                sw.Stop();
                Log($"[Timing] {label}: {sw.ElapsedMilliseconds} ms");
            } catch {
                sw.Stop();
                Log($"[Timing] {label}: {sw.ElapsedMilliseconds} ms (threw exception)");
                throw;
            }
        }
        // ─────────────────────────────────────────────────────────────────────────

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
            int cycle = 0;
            var loopSw = Stopwatch.StartNew();

            try {
                while (!token.IsCancellationRequested) {
                    cycle++;
                    var cycleSw = Stopwatch.StartNew();
                    Log($"[Timing] ── Cycle {cycle} started ──────────────────────────");

                    // Step 0: Clear any leftover Done button
                    bool doneCleared = false;
                    if (cycle == 1) {
                        doneCleared = await Timed($"Cycle {cycle} – CheckAndClickDoneButton",
                            () => CheckAndClickDoneButton());
                        if (doneCleared)
                            Log("[Auto] Found a leftover Done button and clicked it before starting upload.");
                    }

                    targetFile = GetTargetFile(selectedFolder);
                    if (string.IsNullOrEmpty(targetFile)) {
                        Log("No files found in selected folder. Stopping Auto-Upload.");
                        break;
                    }

                    Log("[Auto] Uploading " + fileMode.ToLower() + " file: " + Path.GetFileName(targetFile));

                    // Step 1: Upload
                    bool uploaded = await Timed($"Cycle {cycle} – UploadLog",
                        () => UploadLog(targetFile, token));

                    if (!uploaded) {
                        Log("[Auto] Upload step failed. Stopping Auto-Upload.");
                        break;
                    }

                    if (token.IsCancellationRequested) {
                        Log("[Auto] Stop requested after upload completed.");
                        break;
                    }

                    // Step 2: Wait for Delete Now
                    Log("[Auto] Waiting for Delete Now button to appear...");
                    var waitDeleteSw = Stopwatch.StartNew();
                    AutomationElement? deleteNowButton = await WaitForButton("Delete Now",
                        interval: DeletePollInterval,
                        timeout: DeletePollTimeout,
                        token);
                    waitDeleteSw.Stop();
                    Log($"[Timing] Cycle {cycle} – WaitForDeleteNow: {waitDeleteSw.ElapsedMilliseconds} ms " +
                        $"(found: {deleteNowButton != null})");

                    if (deleteNowButton == null) {
                        if (token.IsCancellationRequested)
                            Log("[Auto] Stop requested while waiting for Delete Now button.");
                        else
                            Log("[Auto] Timed out waiting for Delete Now button. Stopping Auto-Upload.");
                        break;
                    }

                    // Step 3: Delete sequence
                    Log("[Auto] Delete Now button detected. Starting delete sequence...");
                    bool deleted = await Timed($"Cycle {cycle} – DeleteLog",
                        () => DeleteLog(token));

                    if (!deleted) {
                        Log("[Auto] Delete step failed. Stopping Auto-Upload.");
                        break;
                    }

                    cycleSw.Stop();
                    Log($"[Timing] ── Cycle {cycle} complete in {cycleSw.ElapsedMilliseconds} ms ──────────");
                    Log("[Auto] Delete sequence complete. Looping back to Upload.");
                }
            } catch (OperationCanceledException) {
                Log("[Auto] Stopped.");
            } catch (Exception ex) {
                Log("[Auto] Unexpected error: " + ex.Message);
            } finally {
                loopSw.Stop();
                Log($"[Timing] Total automation session: {loopSw.ElapsedMilliseconds} ms over {cycle} cycle(s).");
                OnAutomationStopped();
            }
        }

        /// <summary>
        /// Checks the uploader window for a leftover "Done" button and clicks it if present.
        /// Returns true if a Done button was found and invoked, false otherwise.
        /// </summary>
        private async Task<bool> CheckAndClickDoneButton() {
            var sw = Stopwatch.StartNew();

            AutomationElement? window = await Task.Run(FindUploaderWindow);
            Log($"[Timing]   CheckAndClickDoneButton – FindUploaderWindow: {sw.ElapsedMilliseconds} ms");

            if (window == null) return false;

            var findSw = Stopwatch.StartNew();
            AutomationElement? doneButton = await Task.Run(() => {
                try {
                    return window.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Done"));
                } catch (ElementNotAvailableException) {
                    return null;
                }
            });
            findSw.Stop();
            Log($"[Timing]   CheckAndClickDoneButton – FindFirst(Done): {findSw.ElapsedMilliseconds} ms (found: {doneButton != null})");

            if (doneButton == null) return false;

            var invokeSw = Stopwatch.StartNew();
            await Task.Run(() => (doneButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern)?.Invoke());
            await Task.Delay(500);
            SendKeys.SendWait(" ");
            invokeSw.Stop();
            Log($"[Timing]   CheckAndClickDoneButton – Invoke+Delay: {invokeSw.ElapsedMilliseconds} ms");

            return true;
        }
    }
}