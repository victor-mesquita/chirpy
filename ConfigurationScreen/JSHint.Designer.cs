﻿namespace Zippy.Chirp.ConfigurationScreen
{
    partial class JSHint
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.chkJSHint = new System.Windows.Forms.CheckBox();
            this.propertyGridOptions = new System.Windows.Forms.PropertyGrid();
            this.SuspendLayout();
            // 
            // chkJSHint
            // 
            this.chkJSHint.AutoSize = true;
            this.chkJSHint.Location = new System.Drawing.Point(3, 12);
            this.chkJSHint.Name = "chkJSHint";
            this.chkJSHint.Size = new System.Drawing.Size(83, 17);
            this.chkJSHint.TabIndex = 26;
            this.chkJSHint.Text = "Run JS Hint";
            this.chkJSHint.UseVisualStyleBackColor = true;
            // 
            // propertyGridOptions
            // 
            this.propertyGridOptions.Location = new System.Drawing.Point(3, 35);
            this.propertyGridOptions.Name = "propertyGridOptions";
            this.propertyGridOptions.Size = new System.Drawing.Size(378, 249);
            this.propertyGridOptions.TabIndex = 29;
            // 
            // JSHint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.propertyGridOptions);
            this.Controls.Add(this.chkJSHint);
            this.Name = "JSHint";
            this.Size = new System.Drawing.Size(387, 291);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkJSHint;
        private System.Windows.Forms.PropertyGrid propertyGridOptions;
    }
}
