public class SettingsForm : Form {
    public string ClientId { get; private set; } = string.Empty;
    public string RedirectUrl { get; private set; } = string.Empty;
    public string UploaderPath { get; private set; } = string.Empty;
    public string FolderPath { get; private set; } = string.Empty;
    public string FileMode { get; private set; } = "Largest";

    private TextBox txtClientId = new TextBox();
    private TextBox txtRedirectUrl = new TextBox();
    private TextBox txtUploaderPath = new TextBox();
    private TextBox txtFolderPath = new TextBox();
    private ComboBox cmbFileMode = new ComboBox();

    public SettingsForm(string currentClientId, string currentRedirectUrl, string currentUploaderPath, string currentFolderPath, string currentFileMode = "Largest") {
        Text = "Settings";
        Width = 460;
        Height = 350;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var lblClientId = new Label { Text = "Client ID:", Left = 16, Top = 16, Width = 110 };
        txtClientId = new TextBox { Left = 130, Top = 13, Width = 290, Text = currentClientId };

        var lblRedirect = new Label { Text = "Redirect URL:", Left = 16, Top = 52, Width = 110 };
        txtRedirectUrl = new TextBox { Left = 130, Top = 49, Width = 290, Text = currentRedirectUrl };

        var lblUploader = new Label { Text = "Uploader Path:", Left = 16, Top = 88, Width = 110 };
        txtUploaderPath = new TextBox { Left = 130, Top = 85, Width = 250, Text = currentUploaderPath };
        var btnBrowseUploader = new Button { Text = "...", Left = 384, Top = 84, Width = 36 };

        btnBrowseUploader.Click += (s, e) => {
            using var dialog = new OpenFileDialog {
                Title = "Select Warcraft Logs Uploader",
                Filter = "Executable files (*.exe)|*.exe",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
                txtUploaderPath.Text = dialog.FileName;
        };

        var lblFolder = new Label { Text = "Logs Folder:", Left = 16, Top = 124, Width = 110 };
        txtFolderPath = new TextBox { Left = 130, Top = 121, Width = 250, Text = currentFolderPath };
        var btnBrowseFolder = new Button { Text = "...", Left = 384, Top = 120, Width = 36 };

        btnBrowseFolder.Click += (s, e) => {
            using var dialog = new FolderBrowserDialog {
                Description = "Select logs folder",
                UseDescriptionForTitle = true,
                SelectedPath = txtFolderPath.Text
            };
            if (dialog.ShowDialog() == DialogResult.OK)
                txtFolderPath.Text = dialog.SelectedPath;
        };

        var lblFileMode = new Label { Text = "Upload File:", Left = 16, Top = 160, Width = 110 };
        cmbFileMode = new ComboBox {
            Left = 130,
            Top = 157,
            Width = 290,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbFileMode.Items.AddRange(new[] { "Largest", "Newest", "Oldest" });
        cmbFileMode.SelectedItem = currentFileMode;

        var btnSave = new Button { Text = "Save", Left = 130, Top = 215, Width = 100 };
        var btnCancel = new Button { Text = "Cancel", Left = 242, Top = 215, Width = 80 };

        btnSave.Click += (s, e) => {
            if (string.IsNullOrWhiteSpace(txtUploaderPath.Text) || string.IsNullOrWhiteSpace(txtFolderPath.Text)) {
                MessageBox.Show("Uploader Path and Logs Folder are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ClientId = txtClientId.Text.Trim();
            RedirectUrl = txtRedirectUrl.Text.Trim();
            UploaderPath = txtUploaderPath.Text.Trim();
            FolderPath = txtFolderPath.Text.Trim();
            FileMode = cmbFileMode.SelectedItem?.ToString() ?? "Largest";
            DialogResult = DialogResult.OK;
            Close();
        };

        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange(new Control[] {
            lblClientId, txtClientId,
            lblRedirect, txtRedirectUrl,
            lblUploader, txtUploaderPath, btnBrowseUploader,
            lblFolder, txtFolderPath, btnBrowseFolder,
            lblFileMode, cmbFileMode,
            btnSave, btnCancel
        });
    }
}