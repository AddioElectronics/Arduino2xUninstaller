namespace Arduino2xUninstaller
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.button_Uninstall = new System.Windows.Forms.Button();
            this.label_Progress = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.repoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 125);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(440, 24);
            this.progressBar.TabIndex = 0;
            // 
            // button_Uninstall
            // 
            this.button_Uninstall.Location = new System.Drawing.Point(140, 38);
            this.button_Uninstall.Name = "button_Uninstall";
            this.button_Uninstall.Size = new System.Drawing.Size(200, 60);
            this.button_Uninstall.TabIndex = 1;
            this.button_Uninstall.Text = "Uninstall";
            this.button_Uninstall.UseVisualStyleBackColor = true;
            this.button_Uninstall.Click += new System.EventHandler(this.button_Uninstall_Click);
            // 
            // label_Progress
            // 
            this.label_Progress.AutoSize = true;
            this.label_Progress.Location = new System.Drawing.Point(12, 107);
            this.label_Progress.Name = "label_Progress";
            this.label_Progress.Size = new System.Drawing.Size(0, 15);
            this.label_Progress.TabIndex = 2;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(464, 24);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.repoToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // repoToolStripMenuItem
            // 
            this.repoToolStripMenuItem.Name = "repoToolStripMenuItem";
            this.repoToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.repoToolStripMenuItem.Text = "Repo";
            this.repoToolStripMenuItem.Click += new System.EventHandler(this.repoToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 161);
            this.Controls.Add(this.label_Progress);
            this.Controls.Add(this.button_Uninstall);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Arduino 2x Uninstaller";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ProgressBar progressBar;
        private Button button_Uninstall;
        private Label label_Progress;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem repoToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
    }
}