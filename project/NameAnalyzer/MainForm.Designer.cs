namespace NameAnalyzer
{
    partial class MainForm
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
            LevelPicker = new NumericUpDown();
            SubList = new ListBox();
            NamePicker = new ComboBox();
            menuStrip1 = new MenuStrip();
            parseToolStripMenuItem = new ToolStripMenuItem();
            MainMenu_Parse_SingleFile = new ToolStripMenuItem();
            MainMenu_Parse_Folder = new ToolStripMenuItem();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            ((System.ComponentModel.ISupportInitialize)LevelPicker).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // LevelPicker
            // 
            LevelPicker.Location = new Point(72, 55);
            LevelPicker.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            LevelPicker.Name = "LevelPicker";
            LevelPicker.Size = new Size(180, 30);
            LevelPicker.TabIndex = 0;
            LevelPicker.ValueChanged += LevelPicker_ValueChanged;
            // 
            // SubList
            // 
            SubList.Dock = DockStyle.Bottom;
            SubList.FormattingEnabled = true;
            SubList.ItemHeight = 24;
            SubList.Location = new Point(0, 161);
            SubList.Name = "SubList";
            SubList.Size = new Size(642, 460);
            SubList.TabIndex = 2;
            // 
            // NamePicker
            // 
            NamePicker.FormattingEnabled = true;
            NamePicker.Location = new Point(367, 55);
            NamePicker.Name = "NamePicker";
            NamePicker.Size = new Size(182, 32);
            NamePicker.TabIndex = 3;
            NamePicker.SelectedIndexChanged += NamePicker_SelectedIndexChanged;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { parseToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(642, 32);
            menuStrip1.TabIndex = 4;
            menuStrip1.Text = "menuStrip1";
            // 
            // parseToolStripMenuItem
            // 
            parseToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { MainMenu_Parse_SingleFile, MainMenu_Parse_Folder });
            parseToolStripMenuItem.Name = "parseToolStripMenuItem";
            parseToolStripMenuItem.Size = new Size(72, 28);
            parseToolStripMenuItem.Text = "Parse";
            // 
            // MainMenu_Parse_SingleFile
            // 
            MainMenu_Parse_SingleFile.Name = "MainMenu_Parse_SingleFile";
            MainMenu_Parse_SingleFile.Size = new Size(198, 34);
            MainMenu_Parse_SingleFile.Text = "Single File";
            MainMenu_Parse_SingleFile.Click += MainMenu_Parse_SingleFile_Click;
            // 
            // MainMenu_Parse_Folder
            // 
            MainMenu_Parse_Folder.Name = "MainMenu_Parse_Folder";
            MainMenu_Parse_Folder.Size = new Size(198, 34);
            MainMenu_Parse_Folder.Text = "Folder";
            MainMenu_Parse_Folder.Click += MainMenu_Parse_Folder_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 57);
            label1.Name = "label1";
            label1.Size = new Size(53, 24);
            label1.TabIndex = 5;
            label1.Text = "Level";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(299, 58);
            label2.Name = "label2";
            label2.Size = new Size(62, 24);
            label2.TabIndex = 5;
            label2.Text = "Name";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(215, 134);
            label3.Name = "label3";
            label3.Size = new Size(195, 24);
            label3.TabIndex = 5;
            label3.Text = "Sub-Names or Values";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(642, 621);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(NamePicker);
            Controls.Add(SubList);
            Controls.Add(LevelPicker);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            Text = "MainForm";
            ((System.ComponentModel.ISupportInitialize)LevelPicker).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private NumericUpDown LevelPicker;
        private Button ParseFolder;
        private ListBox SubList;
        private ComboBox NamePicker;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem parseToolStripMenuItem;
        private ToolStripMenuItem MainMenu_Parse_SingleFile;
        private ToolStripMenuItem MainMenu_Parse_Folder;
        private Label label1;
        private Label label2;
        private Label label3;
    }
}