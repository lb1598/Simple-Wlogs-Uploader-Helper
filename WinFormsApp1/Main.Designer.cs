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
            btnUpload = new Button();
            btnLogin = new Button();
            lblDebug = new TextBox();
            SuspendLayout();
            // 
            // btnUpload
            // 
            btnUpload.BackColor = SystemColors.Control;
            btnUpload.ForeColor = Color.Black;
            btnUpload.Location = new Point(520, 166);
            btnUpload.Name = "btnUpload";
            btnUpload.Size = new Size(75, 38);
            btnUpload.TabIndex = 2;
            btnUpload.Text = "Upload";
            btnUpload.UseVisualStyleBackColor = false;
            // 
            // btnLogin
            // 
            btnLogin.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogin.Location = new Point(520, 12);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(75, 23);
            btnLogin.TabIndex = 1;
            btnLogin.Text = "Settings";
            btnLogin.UseVisualStyleBackColor = true;
            // 
            // lblDebug
            // 
            lblDebug.Location = new Point(12, 12);
            lblDebug.Multiline = true;
            lblDebug.Name = "lblDebug";
            lblDebug.ReadOnly = true;
            lblDebug.ScrollBars = ScrollBars.Vertical;
            lblDebug.Size = new Size(497, 192);
            lblDebug.TabIndex = 6;
            lblDebug.Text = "Status: ready";
            // 
            // Main
            // 
            BackColor = SystemColors.Control;
            ClientSize = new Size(607, 216);
            Controls.Add(lblDebug);
            Controls.Add(btnUpload);
            Controls.Add(btnLogin);
            Name = "Main";
            Text = "Simple Log Uploader";
            Load += Main_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnUpload;
        private Button btnLogin;
        private TextBox lblDebug;
    }
}