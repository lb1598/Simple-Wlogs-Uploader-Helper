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
            lblTiming = new TextBox();
            btnAutomate = new Button();
            btnToggleDebug = new Button();
            btnToggleTiming = new Button();
            SuspendLayout();
            // 
            // btnUpload
            // 
            btnUpload.BackColor = SystemColors.Control;
            btnUpload.ForeColor = Color.Black;
            btnUpload.Location = new Point(520, 241);
            btnUpload.Name = "btnUpload";
            btnUpload.Size = new Size(75, 38);
            btnUpload.TabIndex = 2;
            btnUpload.Text = "Upload";
            btnUpload.UseVisualStyleBackColor = false;
            btnUpload.Click += btnUpload_Click;
            // 
            // btnDelete
            // 
            btnDelete.BackColor = SystemColors.Control;
            btnDelete.ForeColor = Color.Black;
            btnDelete.Location = new Point(434, 241);
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
            lblDebug.Location = new Point(12, 41);
            lblDebug.Multiline = true;
            lblDebug.Name = "lblDebug";
            lblDebug.ReadOnly = true;
            lblDebug.ScrollBars = ScrollBars.Vertical;
            lblDebug.Size = new Size(497, 192);
            lblDebug.TabIndex = 6;
            lblDebug.Text = "Status: ready";
            // 
            // lblTiming
            // 
            lblTiming.Location = new Point(12, 41);
            lblTiming.Multiline = true;
            lblTiming.Name = "lblTiming";
            lblTiming.ReadOnly = true;
            lblTiming.ScrollBars = ScrollBars.Vertical;
            lblTiming.Size = new Size(583, 192);
            lblTiming.TabIndex = 7;
            lblTiming.Text = "Timing: ready";
            // 
            // btnAutomate
            // 
            btnAutomate.BackColor = SystemColors.Control;
            btnAutomate.ForeColor = Color.Black;
            btnAutomate.Location = new Point(12, 241);
            btnAutomate.Name = "btnAutomate";
            btnAutomate.Size = new Size(120, 38);
            btnAutomate.TabIndex = 8;
            btnAutomate.Text = "Auto-Upload";
            btnAutomate.UseVisualStyleBackColor = false;
            // 
            // btnToggleDebug
            // 
            btnToggleDebug.BackColor = SystemColors.Control;
            btnToggleDebug.ForeColor = Color.Black;
            btnToggleDebug.Location = new Point(12, 12);
            btnToggleDebug.Name = "btnToggleDebug";
            btnToggleDebug.Size = new Size(75, 23);
            btnToggleDebug.TabIndex = 9;
            btnToggleDebug.Text = "Log";
            btnToggleDebug.UseVisualStyleBackColor = false;
            btnToggleDebug.Click += BtnToggleDebug_Click;
            // 
            // btnToggleTiming
            // 
            btnToggleTiming.BackColor = SystemColors.Control;
            btnToggleTiming.ForeColor = Color.Black;
            btnToggleTiming.Location = new Point(93, 12);
            btnToggleTiming.Name = "btnToggleTiming";
            btnToggleTiming.Size = new Size(75, 23);
            btnToggleTiming.TabIndex = 10;
            btnToggleTiming.Text = "Timing";
            btnToggleTiming.UseVisualStyleBackColor = false;
            btnToggleTiming.Click += BtnToggleTiming_Click;
            // 
            // Main
            // 
            BackColor = SystemColors.Control;
            ClientSize = new Size(607, 287);
            Controls.Add(btnToggleDebug);
            Controls.Add(btnToggleTiming);
            Controls.Add(lblTiming);
            Controls.Add(lblDebug);
            Controls.Add(btnAutomate);
            Controls.Add(btnDelete);
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
        private TextBox lblTiming;
        private Button btnDelete;
        private Button btnAutomate;
        private Button btnToggleDebug;
        private Button btnToggleTiming;
    }
}