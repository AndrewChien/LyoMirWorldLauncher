namespace Launcher
{
    partial class FrmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            cmbServers = new ComboBox();
            lblTitle = new Label();
            lblSocketStatus = new Label();
            picBanner = new PictureBox();
            webBrowser = new WebBrowser();
            btnRegister = new Button();
            btnChangePassword = new Button();
            btnGetBackPassword = new Button();
            btnEnterGame = new Button();
            btnOpenWeb = new Button();
            btnExit = new Button();
            timerTitleScroll = new System.Windows.Forms.Timer(components);
            timerDownload = new System.Windows.Forms.Timer(components);
            timerPatchAndExit = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)picBanner).BeginInit();
            SuspendLayout();
            // 
            // cmbServers
            // 
            cmbServers.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbServers.Enabled = false;
            cmbServers.FormattingEnabled = true;
            cmbServers.Location = new Point(12, 12);
            cmbServers.Name = "cmbServers";
            cmbServers.Size = new Size(240, 25);
            cmbServers.TabIndex = 0;
            cmbServers.SelectedIndexChanged += cmbServers_SelectedIndexChanged;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(12, 48);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(0, 17);
            lblTitle.TabIndex = 1;
            // 
            // lblSocketStatus
            // 
            lblSocketStatus.AutoSize = true;
            lblSocketStatus.ForeColor = Color.DimGray;
            lblSocketStatus.Location = new Point(12, 76);
            lblSocketStatus.Name = "lblSocketStatus";
            lblSocketStatus.Size = new Size(80, 17);
            lblSocketStatus.TabIndex = 2;
            lblSocketStatus.Text = "未连接服务器";
            // 
            // picBanner
            // 
            picBanner.BorderStyle = BorderStyle.FixedSingle;
            picBanner.Location = new Point(270, 12);
            picBanner.Name = "picBanner";
            picBanner.Size = new Size(240, 100);
            picBanner.SizeMode = PictureBoxSizeMode.Zoom;
            picBanner.TabIndex = 3;
            picBanner.TabStop = false;
            // 
            // webBrowser
            // 
            webBrowser.Location = new Point(12, 128);
            webBrowser.MinimumSize = new Size(20, 20);
            webBrowser.Name = "webBrowser";
            webBrowser.Size = new Size(498, 260);
            webBrowser.TabIndex = 4;
            // 
            // btnRegister
            // 
            btnRegister.Location = new Point(12, 404);
            btnRegister.Name = "btnRegister";
            btnRegister.Size = new Size(90, 30);
            btnRegister.TabIndex = 5;
            btnRegister.Text = "注册";
            btnRegister.UseVisualStyleBackColor = true;
            btnRegister.Click += btnRegister_Click;
            // 
            // btnChangePassword
            // 
            btnChangePassword.Location = new Point(108, 404);
            btnChangePassword.Name = "btnChangePassword";
            btnChangePassword.Size = new Size(90, 30);
            btnChangePassword.TabIndex = 6;
            btnChangePassword.Text = "改密";
            btnChangePassword.UseVisualStyleBackColor = true;
            btnChangePassword.Click += btnChangePassword_Click;
            // 
            // btnGetBackPassword
            // 
            btnGetBackPassword.Location = new Point(204, 404);
            btnGetBackPassword.Name = "btnGetBackPassword";
            btnGetBackPassword.Size = new Size(90, 30);
            btnGetBackPassword.TabIndex = 7;
            btnGetBackPassword.Text = "找回密码";
            btnGetBackPassword.UseVisualStyleBackColor = true;
            btnGetBackPassword.Click += btnGetBackPassword_Click;
            // 
            // btnEnterGame
            // 
            btnEnterGame.Location = new Point(300, 404);
            btnEnterGame.Name = "btnEnterGame";
            btnEnterGame.Size = new Size(90, 30);
            btnEnterGame.TabIndex = 8;
            btnEnterGame.Text = "进入游戏";
            btnEnterGame.UseVisualStyleBackColor = true;
            btnEnterGame.Click += btnEnterGame_Click;
            // 
            // btnOpenWeb
            // 
            btnOpenWeb.Location = new Point(396, 404);
            btnOpenWeb.Name = "btnOpenWeb";
            btnOpenWeb.Size = new Size(54, 30);
            btnOpenWeb.TabIndex = 9;
            btnOpenWeb.Text = "官网";
            btnOpenWeb.UseVisualStyleBackColor = true;
            btnOpenWeb.Click += btnOpenWeb_Click;
            // 
            // btnExit
            // 
            btnExit.Location = new Point(456, 404);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(54, 30);
            btnExit.TabIndex = 10;
            btnExit.Text = "退出";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // timerTitleScroll
            // 
            timerTitleScroll.Tick += timerTitleScroll_Tick;
            // 
            // timerDownload
            // 
            timerDownload.Interval = 250;
            timerDownload.Tick += timerDownload_Tick;
            // 
            // timerPatchAndExit
            // 
            timerPatchAndExit.Interval = 250;
            timerPatchAndExit.Tick += timerPatchAndExit_Tick;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(522, 450);
            Controls.Add(btnExit);
            Controls.Add(btnOpenWeb);
            Controls.Add(btnEnterGame);
            Controls.Add(btnGetBackPassword);
            Controls.Add(btnChangePassword);
            Controls.Add(btnRegister);
            Controls.Add(webBrowser);
            Controls.Add(picBanner);
            Controls.Add(lblSocketStatus);
            Controls.Add(lblTitle);
            Controls.Add(cmbServers);
            Name = "FrmMain";
            Text = "LyoMirWorld 测试登录器";
            FormClosing += FrmMain_FormClosing;
            Load += FrmMain_Load;
            ((System.ComponentModel.ISupportInitialize)picBanner).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cmbServers;
        private Label lblTitle;
        private Label lblSocketStatus;
        private PictureBox picBanner;
        private WebBrowser webBrowser;
        private Button btnRegister;
        private Button btnChangePassword;
        private Button btnGetBackPassword;
        private Button btnEnterGame;
        private Button btnOpenWeb;
        private Button btnExit;
        private System.Windows.Forms.Timer timerTitleScroll;
        private System.Windows.Forms.Timer timerDownload;
        private System.Windows.Forms.Timer timerPatchAndExit;
    }
}
