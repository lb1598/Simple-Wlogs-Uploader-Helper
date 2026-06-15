namespace SimpleLogUploader {
    partial class Main {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent() {
            btnSelectFolder = new Button();
            btnUpload = new Button();
            btnLogin = new Button();
            txtFolderPath = new TextBox();
            btnUploaderPath = new Button();
            txtUploaderPath = new TextBox();
            lblDebug = new TextBox();
            SuspendLayout();
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Location = new Point(12, 16);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(99, 23);
            btnSelectFolder.TabIndex = 0;
            btnSelectFolder.Text = "Select Folder";
            btnSelectFolder.UseVisualStyleBackColor = true;
            // 
            // btnUpload
            // 
            btnUpload.BackColor = SystemColors.Control;
            btnUpload.ForeColor = Color.Black;
            btnUpload.Location = new Point(12, 74);
            btnUpload.Name = "btnUpload";
            btnUpload.Size = new Size(583, 38);
            btnUpload.TabIndex = 2;
            btnUpload.Text = "Upload";
            btnUpload.UseVisualStyleBackColor = false;
            // 
            // btnLogin
            // 
            btnLogin.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogin.Location = new Point(520, 16);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(75, 23);
            btnLogin.TabIndex = 1;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = true;
            // 
            // txtFolderPath
            // 
            txtFolderPath.Location = new Point(119, 17);
            txtFolderPath.MaximumSize = new Size(400, 20);
            txtFolderPath.Name = "txtFolderPath";
            txtFolderPath.ReadOnly = true;
            txtFolderPath.Size = new Size(395, 20);
            txtFolderPath.TabIndex = 3;
            // 
            // btnUploaderPath
            // 
            btnUploaderPath.Location = new Point(12, 45);
            btnUploaderPath.Name = "btnUploaderPath";
            btnUploaderPath.Size = new Size(99, 23);
            btnUploaderPath.TabIndex = 4;
            btnUploaderPath.Text = "Uploader Path";
            btnUploaderPath.UseVisualStyleBackColor = true;
            // 
            // txtUploaderPath
            // 
            txtUploaderPath.Location = new Point(119, 48);
            txtUploaderPath.MaximumSize = new Size(400, 20);
            txtUploaderPath.Name = "txtUploaderPath";
            txtUploaderPath.ReadOnly = true;
            txtUploaderPath.Size = new Size(395, 20);
            txtUploaderPath.TabIndex = 5;
            // 
            // lblDebug
            // 
            lblDebug.Location = new Point(12, 118);
            lblDebug.Multiline = true;
            lblDebug.Name = "lblDebug";
            lblDebug.ReadOnly = true;
            lblDebug.ScrollBars = ScrollBars.Vertical;
            lblDebug.Size = new Size(583, 120);
            lblDebug.TabIndex = 6;
            lblDebug.Text = "Status: ready";
            // 
            // Main
            // 
            BackColor = SystemColors.Control;
            ClientSize = new Size(607, 250);
            Controls.Add(lblDebug);
            Controls.Add(txtUploaderPath);
            Controls.Add(btnUploaderPath);
            Controls.Add(txtFolderPath);
            Controls.Add(btnUpload);
            Controls.Add(btnLogin);
            Controls.Add(btnSelectFolder);
            Name = "Main";
            Text = "Simple Log Uploader";
            Load += Main_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnSelectFolder;
        private Button btnUpload;
        private Button btnLogin;
        private TextBox txtFolderPath;
        private Button btnUploaderPath;
        private TextBox txtUploaderPath;
        private TextBox lblDebug;
    }
}