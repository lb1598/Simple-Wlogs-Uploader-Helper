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
            btnDelete = new Button();
            btnLogin = new Button();
            lblDebug = new TextBox();
            btnAutomate = new Button();
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
            // btnDelete
            // 
            btnDelete.BackColor = SystemColors.Control;
            btnDelete.ForeColor = Color.Black;
            btnDelete.Location = new Point(520, 122);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(75, 38);
            btnDelete.TabIndex = 3;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = false;
            // 
            // btnLogin
            // 
            btnLogin.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogin.Location = new Point(520, 12);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(75, 23);
            btnLogin.TabIndex = 1;
            btnLogin.Text = "Settings";
            btnLogin.UseVisualStyleBackColor = false;
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
            // btnAutomate
            // 
            btnAutomate.BackColor = SystemColors.Control;
            btnAutomate.ForeColor = Color.Black;
            btnAutomate.Location = new Point(12, 210);
            btnAutomate.Name = "btnAutomate";
            btnAutomate.Size = new Size(120, 30);
            btnAutomate.TabIndex = 8;
            btnAutomate.Text = "Auto-Upload";
            btnAutomate.UseVisualStyleBackColor = false;
            // 
            // Main
            // 
            BackColor = SystemColors.Control;
            ClientSize = new Size(607, 252);
            Controls.Add(btnAutomate);
            Controls.Add(btnDelete);
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
        private Button btnDelete;
        private Button btnAutomate;
    }
}