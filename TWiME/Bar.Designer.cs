﻿namespace TWiME {
    partial class Bar {
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
            this.SuspendLayout();
            // 
            // Bar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 51);
            this.DoubleBuffered = true;
            this.Name = "Bar";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Bar_FormClosing);
            this.Load += new System.EventHandler(this.Bar_Load);
            this.Shown += new System.EventHandler(this.Bar_Shown);
            this.LocationChanged += new System.EventHandler(this.Bar_LocationChanged);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Bar_Paint);
            this.ResumeLayout(false);

        }

        #endregion

    }
}

