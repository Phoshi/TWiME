namespace TWiME {
    partial class WindowSwitcher {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.filter = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // filter
            // 
            this.filter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.filter.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.filter.Location = new System.Drawing.Point(0, 0);
            this.filter.Name = "filter";
            this.filter.Size = new System.Drawing.Size(422, 13);
            this.filter.TabIndex = 0;
            this.filter.TextChanged += new System.EventHandler(this.filter_TextChanged);
            // 
            // WindowSwitcher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(422, 212);
            this.Controls.Add(this.filter);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "WindowSwitcher";
            this.Text = "WindowSwitcher";
            this.TopMost = true;
            this.Deactivate += new System.EventHandler(this.WindowSwitcher_Deactivate);
            this.Load += new System.EventHandler(this.WindowSwitcher_Load);
            this.VisibleChanged += new System.EventHandler(this.WindowSwitcher_VisibleChanged);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.WindowSwitcher_Paint);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox filter;
    }
}