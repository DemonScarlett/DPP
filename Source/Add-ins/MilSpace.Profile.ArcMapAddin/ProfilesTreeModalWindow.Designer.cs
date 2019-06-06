﻿namespace MilSpace.Profile
{
    partial class ProfilesTreeModalWindow
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
            System.Windows.Forms.TreeNode treeNode7 = new System.Windows.Forms.TreeNode("Отрезки");
            System.Windows.Forms.TreeNode treeNode8 = new System.Windows.Forms.TreeNode("Веер");
            System.Windows.Forms.TreeNode treeNode9 = new System.Windows.Forms.TreeNode("Графика");
            this.profilesTreeView = new System.Windows.Forms.TreeView();
            this.buttonsPanel = new System.Windows.Forms.Panel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.buttonsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // profilesTreeView
            // 
            this.profilesTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.profilesTreeView.FullRowSelect = true;
            this.profilesTreeView.ImageKey = "0.png";
            this.profilesTreeView.Location = new System.Drawing.Point(-1, 0);
            this.profilesTreeView.Name = "profilesTreeView";
            treeNode7.ImageKey = "vector-path-line.png";
            treeNode7.Name = "Points";
            treeNode7.SelectedImageIndex = 205;
            treeNode7.Text = "Отрезки";
            treeNode8.ImageKey = "Editing-Line-icon3.png";
            treeNode8.Name = "Fun";
            treeNode8.SelectedImageIndex = 208;
            treeNode8.Text = "Веер";
            treeNode9.ImageKey = "vector-polygon.png";
            treeNode9.Name = "Primitives";
            treeNode9.SelectedImageIndex = 209;
            treeNode9.Text = "Графика";
            this.profilesTreeView.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode7,
            treeNode8,
            treeNode9});
            this.profilesTreeView.SelectedImageKey = "Ok.png";
            this.profilesTreeView.Size = new System.Drawing.Size(233, 246);
            this.profilesTreeView.TabIndex = 36;
            // 
            // buttonsPanel
            // 
            this.buttonsPanel.BackColor = System.Drawing.SystemColors.Window;
            this.buttonsPanel.Controls.Add(this.cancelButton);
            this.buttonsPanel.Controls.Add(this.okButton);
            this.buttonsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonsPanel.Location = new System.Drawing.Point(0, 245);
            this.buttonsPanel.Name = "buttonsPanel";
            this.buttonsPanel.Size = new System.Drawing.Size(233, 30);
            this.buttonsPanel.TabIndex = 37;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(165, 5);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(60, 20);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(5, 5);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(60, 20);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // ProfilesTreeModalWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(233, 275);
            this.Controls.Add(this.buttonsPanel);
            this.Controls.Add(this.profilesTreeView);
            this.MinimumSize = new System.Drawing.Size(240, 300);
            this.Name = "ProfilesTreeModalWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Дерево профилей";
            this.buttonsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.TreeView profilesTreeView;
        private System.Windows.Forms.Panel buttonsPanel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}