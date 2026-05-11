namespace Prodright
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            label1 = new Label();
            nudThreshold = new NumericUpDown();
            dgvItems = new DataGridView();
            txbSummary = new TextBox();
            btnLoad = new Button();
            rtbProductStory = new RichTextBox();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            tabPage3 = new TabPage();
            rtbS4Product = new RichTextBox();
            label2 = new Label();
            textBox1 = new TextBox();
            btnCallApi = new Button();
            tabPage4 = new TabPage();
            richTextBox1 = new RichTextBox();
            menuStrip1 = new MenuStrip();
            showApiCallsToolStripMenuItem = new ToolStripMenuItem();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvItems).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            tabPage3.SuspendLayout();
            tabPage4.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(groupBox2);
            groupBox1.Controls.Add(dgvItems);
            groupBox1.Controls.Add(txbSummary);
            groupBox1.Location = new Point(6, 6);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(1003, 593);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "SAP <--> Store via PO";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(nudThreshold);
            groupBox2.Location = new Point(727, 18);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(270, 164);
            groupBox2.TabIndex = 5;
            groupBox2.TabStop = false;
            groupBox2.Text = "Thresholds";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 24);
            label1.Name = "label1";
            label1.Size = new Size(168, 15);
            label1.TabIndex = 5;
            label1.Text = "Bulk Discount Max Discount %";
            // 
            // nudThreshold
            // 
            nudThreshold.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            nudThreshold.Location = new Point(218, 22);
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new Size(46, 23);
            nudThreshold.TabIndex = 4;
            nudThreshold.TextAlign = HorizontalAlignment.Right;
            nudThreshold.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // dgvItems
            // 
            dgvItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvItems.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvItems.Location = new Point(6, 188);
            dgvItems.Name = "dgvItems";
            dgvItems.ReadOnly = true;
            dgvItems.Size = new Size(991, 399);
            dgvItems.TabIndex = 2;
            // 
            // txbSummary
            // 
            txbSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txbSummary.Location = new Point(6, 18);
            txbSummary.Multiline = true;
            txbSummary.Name = "txbSummary";
            txbSummary.Size = new Size(715, 164);
            txbSummary.TabIndex = 1;
            // 
            // btnLoad
            // 
            btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoad.Location = new Point(953, 12);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(75, 23);
            btnLoad.TabIndex = 0;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += BtnLoadFiles_Click;
            // 
            // rtbProductStory
            // 
            rtbProductStory.Dock = DockStyle.Fill;
            rtbProductStory.Location = new Point(3, 3);
            rtbProductStory.Name = "rtbProductStory";
            rtbProductStory.Size = new Size(1009, 599);
            rtbProductStory.TabIndex = 5;
            rtbProductStory.Text = "";
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Location = new Point(12, 36);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1023, 633);
            tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1015, 605);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "SAP PO Analysis";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(rtbProductStory);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1015, 605);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Product Story";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(rtbS4Product);
            tabPage3.Controls.Add(label2);
            tabPage3.Controls.Add(textBox1);
            tabPage3.Controls.Add(btnCallApi);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Size = new Size(1015, 605);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Data from S4";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // rtbS4Product
            // 
            rtbS4Product.Dock = DockStyle.Left;
            rtbS4Product.Location = new Point(0, 0);
            rtbS4Product.Name = "rtbS4Product";
            rtbS4Product.Size = new Size(829, 605);
            rtbS4Product.TabIndex = 3;
            rtbS4Product.Text = "";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(835, 13);
            label2.Name = "label2";
            label2.Size = new Size(63, 15);
            label2.TabIndex = 2;
            label2.Text = "Product ID";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(904, 10);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(100, 23);
            textBox1.TabIndex = 1;
            textBox1.Text = "1020458001";
            // 
            // btnCallApi
            // 
            btnCallApi.Location = new Point(929, 39);
            btnCallApi.Name = "btnCallApi";
            btnCallApi.Size = new Size(75, 23);
            btnCallApi.TabIndex = 0;
            btnCallApi.Text = "Call API";
            btnCallApi.UseVisualStyleBackColor = true;
            btnCallApi.Click += btnCallApi_Click;
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(richTextBox1);
            tabPage4.Location = new Point(4, 24);
            tabPage4.Name = "tabPage4";
            tabPage4.Size = new Size(1015, 605);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "Commentary";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Location = new Point(0, 0);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(1015, 605);
            richTextBox1.TabIndex = 0;
            richTextBox1.Text = resources.GetString("richTextBox1.Text");
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { showApiCallsToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1047, 24);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // showApiCallsToolStripMenuItem
            // 
            showApiCallsToolStripMenuItem.Name = "showApiCallsToolStripMenuItem";
            showApiCallsToolStripMenuItem.Size = new Size(97, 20);
            showApiCallsToolStripMenuItem.Text = "Show Api Calls";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1047, 681);
            Controls.Add(tabControl1);
            Controls.Add(btnLoad);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvItems).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            tabPage4.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBox1;
        private DataGridView dgvItems;
        private TextBox txbSummary;
        private Button btnLoad;
        private NumericUpDown nudThreshold;
        private RichTextBox rtbProductStory;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private GroupBox groupBox2;
        private Label label1;
        private TabPage tabPage3;
        private TextBox textBox1;
        private Button btnCallApi;
        private Label label2;
        private RichTextBox rtbS4Product;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem showApiCallsToolStripMenuItem;
        private TabPage tabPage4;
        private RichTextBox richTextBox1;
    }
}
