using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Redbox.HAL.Common.GUI.Functions
{
    public class NumberPad : UserControl
    {
        private IContainer components;
        private TableLayoutPanel m_tableLayoutPanel;
        private Button m_sevenButton;
        private Button m_eightButton;
        private Button m_nineButton;
        private Button m_fourButton;
        private Button m_fiveButton;
        private Button m_sixButton;
        private Button m_oneButton;
        private Button m_twoButton;
        private Button m_threeButton;
        private Button m_zeroButton;
        private Button m_clearButton;
        private TextBox m_textBox;

        public NumberPad()
        {
            this.InitializeComponent();
            this.m_textBox.MaxLength = 40;
        }

        public void Clear() => this.m_textBox.Clear();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Number
        {
            get => this.m_textBox.Text;
            set => this.m_textBox.Text = value;
        }

        public int MaxLength
        {
            get => this.m_textBox.MaxLength;
            set => this.m_textBox.MaxLength = value;
        }

        public event EventHandler NumberChanged;

        private void OnNumberPadButtonClicked(object sender, EventArgs e)
        {
            if (!(sender is Button button))
                return;
            if (button.Text == "C")
                this.Clear();
            else
                this.m_textBox.Text += button.Text;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control)
                return;
            switch (e.KeyCode)
            {
                case Keys.C:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    if (this.m_textBox.SelectedText.Length <= 0)
                        break;
                    this.m_textBox.Copy();
                    break;
                case Keys.V:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    if (!Clipboard.GetDataObject().GetDataPresent(DataFormats.Text))
                        break;
                    this.m_textBox.Paste();
                    break;
                case Keys.X:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    if (this.m_textBox.SelectedText.Length <= 0)
                        break;
                    this.m_textBox.Cut();
                    break;
            }
        }

        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b')
                return;
            e.Handled = true;
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            if (this.NumberChanged == null)
                return;
            this.NumberChanged((object)this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.m_tableLayoutPanel = new TableLayoutPanel();
            this.m_sevenButton = new Button();
            this.m_eightButton = new Button();
            this.m_nineButton = new Button();
            this.m_fourButton = new Button();
            this.m_fiveButton = new Button();
            this.m_sixButton = new Button();
            this.m_oneButton = new Button();
            this.m_twoButton = new Button();
            this.m_threeButton = new Button();
            this.m_zeroButton = new Button();
            this.m_clearButton = new Button();
            this.m_textBox = new TextBox();
            this.m_tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            this.m_tableLayoutPanel.ColumnCount = 4;
            this.m_tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this.m_tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this.m_tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this.m_tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_sevenButton, 0, 1);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_eightButton, 1, 1);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_nineButton, 2, 1);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_fourButton, 0, 2);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_fiveButton, 1, 2);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_sixButton, 2, 2);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_oneButton, 0, 3);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_twoButton, 1, 3);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_threeButton, 2, 3);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_zeroButton, 0, 4);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_clearButton, 3, 1);
            this.m_tableLayoutPanel.Controls.Add((Control)this.m_textBox, 0, 0);
            this.m_tableLayoutPanel.Dock = DockStyle.Fill;
            this.m_tableLayoutPanel.Location = new Point(0, 0);
            this.m_tableLayoutPanel.Name = "m_tableLayoutPanel";
            this.m_tableLayoutPanel.RowCount = 5;
            this.m_tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 13.04348f));
            this.m_tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 21.73913f));
            this.m_tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 21.73913f));
            this.m_tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 21.73913f));
            this.m_tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 21.73913f));
            this.m_tableLayoutPanel.Size = new Size(373, 222);
            this.m_tableLayoutPanel.TabIndex = 0;
            this.m_sevenButton.Dock = DockStyle.Fill;
            this.m_sevenButton.Location = new Point(3, 31);
            this.m_sevenButton.Name = "m_sevenButton";
            this.m_sevenButton.Size = new Size(87, 42);
            this.m_sevenButton.TabIndex = 1;
            this.m_sevenButton.Text = "7";
            this.m_sevenButton.UseVisualStyleBackColor = true;
            this.m_sevenButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_eightButton.Dock = DockStyle.Fill;
            this.m_eightButton.Location = new Point(96, 31);
            this.m_eightButton.Name = "m_eightButton";
            this.m_eightButton.Size = new Size(87, 42);
            this.m_eightButton.TabIndex = 2;
            this.m_eightButton.Text = "8";
            this.m_eightButton.UseVisualStyleBackColor = true;
            this.m_eightButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_nineButton.Dock = DockStyle.Fill;
            this.m_nineButton.Location = new Point(189, 31);
            this.m_nineButton.Name = "m_nineButton";
            this.m_nineButton.Size = new Size(87, 42);
            this.m_nineButton.TabIndex = 3;
            this.m_nineButton.Text = "9";
            this.m_nineButton.UseVisualStyleBackColor = true;
            this.m_nineButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_fourButton.Dock = DockStyle.Fill;
            this.m_fourButton.Location = new Point(3, 79);
            this.m_fourButton.Name = "m_fourButton";
            this.m_fourButton.Size = new Size(87, 42);
            this.m_fourButton.TabIndex = 5;
            this.m_fourButton.Text = "4";
            this.m_fourButton.UseVisualStyleBackColor = true;
            this.m_fourButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_fiveButton.Dock = DockStyle.Fill;
            this.m_fiveButton.Location = new Point(96, 79);
            this.m_fiveButton.Name = "m_fiveButton";
            this.m_fiveButton.Size = new Size(87, 42);
            this.m_fiveButton.TabIndex = 6;
            this.m_fiveButton.Text = "5";
            this.m_fiveButton.UseVisualStyleBackColor = true;
            this.m_fiveButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_sixButton.Dock = DockStyle.Fill;
            this.m_sixButton.Location = new Point(189, 79);
            this.m_sixButton.Name = "m_sixButton";
            this.m_sixButton.Size = new Size(87, 42);
            this.m_sixButton.TabIndex = 7;
            this.m_sixButton.Text = "6";
            this.m_sixButton.UseVisualStyleBackColor = true;
            this.m_sixButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_oneButton.Dock = DockStyle.Fill;
            this.m_oneButton.Location = new Point(3, (int)sbyte.MaxValue);
            this.m_oneButton.Name = "m_oneButton";
            this.m_oneButton.Size = new Size(87, 42);
            this.m_oneButton.TabIndex = 8;
            this.m_oneButton.Text = "1";
            this.m_oneButton.UseVisualStyleBackColor = true;
            this.m_oneButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_twoButton.Dock = DockStyle.Fill;
            this.m_twoButton.Location = new Point(96, (int)sbyte.MaxValue);
            this.m_twoButton.Name = "m_twoButton";
            this.m_twoButton.Size = new Size(87, 42);
            this.m_twoButton.TabIndex = 9;
            this.m_twoButton.Text = "2";
            this.m_twoButton.UseVisualStyleBackColor = true;
            this.m_twoButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_threeButton.Dock = DockStyle.Fill;
            this.m_threeButton.Location = new Point(189, (int)sbyte.MaxValue);
            this.m_threeButton.Name = "m_threeButton";
            this.m_threeButton.Size = new Size(87, 42);
            this.m_threeButton.TabIndex = 10;
            this.m_threeButton.Text = "3";
            this.m_threeButton.UseVisualStyleBackColor = true;
            this.m_threeButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_tableLayoutPanel.SetColumnSpan((Control)this.m_zeroButton, 3);
            this.m_zeroButton.Dock = DockStyle.Fill;
            this.m_zeroButton.Location = new Point(3, 175);
            this.m_zeroButton.Name = "m_zeroButton";
            this.m_zeroButton.Size = new Size(273, 44);
            this.m_zeroButton.TabIndex = 11;
            this.m_zeroButton.Text = "0";
            this.m_zeroButton.UseVisualStyleBackColor = true;
            this.m_zeroButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_clearButton.Dock = DockStyle.Fill;
            this.m_clearButton.Location = new Point(282, 31);
            this.m_clearButton.Name = "m_clearButton";
            this.m_clearButton.Size = new Size(88, 42);
            this.m_clearButton.TabIndex = 4;
            this.m_clearButton.Text = "C";
            this.m_clearButton.UseVisualStyleBackColor = true;
            this.m_clearButton.Click += new EventHandler(this.OnNumberPadButtonClicked);
            this.m_tableLayoutPanel.SetColumnSpan((Control)this.m_textBox, 4);
            this.m_textBox.Dock = DockStyle.Fill;
            this.m_textBox.Location = new Point(3, 3);
            this.m_textBox.Name = "m_textBox";
            this.m_textBox.Size = new Size(367, 20);
            this.m_textBox.TabIndex = 0;
            this.m_textBox.TextAlign = HorizontalAlignment.Right;
            this.m_textBox.TextChanged += new EventHandler(this.OnTextChanged);
            this.m_textBox.KeyDown += new KeyEventHandler(this.OnKeyDown);
            this.m_textBox.KeyPress += new KeyPressEventHandler(this.OnKeyPress);
            this.AutoScaleDimensions = new SizeF(6f, 13f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Controls.Add((Control)this.m_tableLayoutPanel);
            this.Name = nameof(NumberPad);
            this.Size = new Size(373, 222);
            this.m_tableLayoutPanel.ResumeLayout(false);
            this.m_tableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
