using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Redbox.HAL.Common.GUI.Functions
{
    public class LocationNumberPad : Form
    {
        private IContainer components;
        private Button button1;
        private Button button2;
        private Button button3;
        private Button button4;
        private Button button5;
        private Button button7;
        private Button button8;
        private Button button9;
        private Button button6;
        private TextBox m_displayTextBox;
        private Button button11;
        private Button button12;
        private Button button10;
        private Button button13;

        public LocationNumberPad() => this.InitializeComponent();

        public int Number
        {
            get
            {
                int result;
                return int.TryParse(this.m_displayTextBox.Text, out result) ? result : -1;
            }
        }

        private void OnNumberButtonPush(object sender, EventArgs e)
        {
            this.m_displayTextBox.Text += this.GetNumber(sender);
        }

        private string GetNumber(object sender)
        {
            return !(sender is Button button) || !(button.Tag is string tag) ? string.Empty : tag;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.m_displayTextBox.Text))
                this.DialogResult = DialogResult.Cancel;
            else
                this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            this.m_displayTextBox.Text = string.Empty;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.button1 = new Button();
            this.button2 = new Button();
            this.button3 = new Button();
            this.button4 = new Button();
            this.button5 = new Button();
            this.button7 = new Button();
            this.button8 = new Button();
            this.button9 = new Button();
            this.button6 = new Button();
            this.m_displayTextBox = new TextBox();
            this.button11 = new Button();
            this.button12 = new Button();
            this.button10 = new Button();
            this.button13 = new Button();
            this.SuspendLayout();
            this.button1.BackColor = Color.LightGray;
            this.button1.Location = new Point(12, 62);
            this.button1.Name = "button1";
            this.button1.Size = new Size(75, 49);
            this.button1.TabIndex = 0;
            this.button1.Tag = (object)"1";
            this.button1.Text = "1";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new EventHandler(this.OnNumberButtonPush);
            this.button2.BackColor = Color.LightGray;
            this.button2.Location = new Point(105, 62);
            this.button2.Name = "button2";
            this.button2.Size = new Size(75, 49);
            this.button2.TabIndex = 1;
            this.button2.Tag = (object)"2";
            this.button2.Text = "2";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new EventHandler(this.OnNumberButtonPush);
            this.button3.BackColor = Color.LightGray;
            this.button3.Location = new Point(197, 62);
            this.button3.Name = "button3";
            this.button3.Size = new Size(75, 49);
            this.button3.TabIndex = 2;
            this.button3.Tag = (object)"3";
            this.button3.Text = "3";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new EventHandler(this.OnNumberButtonPush);
            this.button4.BackColor = Color.LightGray;
            this.button4.Location = new Point(12, 130);
            this.button4.Name = "button4";
            this.button4.Size = new Size(75, 49);
            this.button4.TabIndex = 3;
            this.button4.Tag = (object)"4";
            this.button4.Text = "4";
            this.button4.UseVisualStyleBackColor = false;
            this.button4.Click += new EventHandler(this.OnNumberButtonPush);
            this.button5.BackColor = Color.LightGray;
            this.button5.Location = new Point(105, 130);
            this.button5.Name = "button5";
            this.button5.Size = new Size(75, 49);
            this.button5.TabIndex = 4;
            this.button5.Tag = (object)"5";
            this.button5.Text = "5";
            this.button5.UseVisualStyleBackColor = false;
            this.button5.Click += new EventHandler(this.OnNumberButtonPush);
            this.button7.BackColor = Color.LightGray;
            this.button7.Location = new Point(12, 204);
            this.button7.Name = "button7";
            this.button7.Size = new Size(75, 49);
            this.button7.TabIndex = 6;
            this.button7.Tag = (object)"7";
            this.button7.Text = "7";
            this.button7.UseVisualStyleBackColor = false;
            this.button7.Click += new EventHandler(this.OnNumberButtonPush);
            this.button8.BackColor = Color.LightGray;
            this.button8.Location = new Point(105, 204);
            this.button8.Name = "button8";
            this.button8.Size = new Size(75, 49);
            this.button8.TabIndex = 7;
            this.button8.Tag = (object)"8";
            this.button8.Text = "8";
            this.button8.UseVisualStyleBackColor = false;
            this.button8.Click += new EventHandler(this.OnNumberButtonPush);
            this.button9.BackColor = Color.LightGray;
            this.button9.Location = new Point(197, 204);
            this.button9.Name = "button9";
            this.button9.Size = new Size(75, 49);
            this.button9.TabIndex = 8;
            this.button9.Tag = (object)"9";
            this.button9.Text = "9";
            this.button9.UseVisualStyleBackColor = false;
            this.button9.Click += new EventHandler(this.OnNumberButtonPush);
            this.button6.BackColor = Color.LightGray;
            this.button6.Location = new Point(197, 130);
            this.button6.Name = "button6";
            this.button6.Size = new Size(75, 49);
            this.button6.TabIndex = 9;
            this.button6.Tag = (object)"6";
            this.button6.Text = "6";
            this.button6.UseVisualStyleBackColor = false;
            this.button6.Click += new EventHandler(this.OnNumberButtonPush);
            this.m_displayTextBox.BackColor = SystemColors.ControlLightLight;
            this.m_displayTextBox.Location = new Point(12, 21);
            this.m_displayTextBox.Name = "m_displayTextBox";
            this.m_displayTextBox.ReadOnly = true;
            this.m_displayTextBox.Size = new Size(270, 20);
            this.m_displayTextBox.TabIndex = 11;
            this.button11.BackColor = Color.LightGray;
            this.button11.Location = new Point(314, 62);
            this.button11.Name = "button11";
            this.button11.Size = new Size(75, 49);
            this.button11.TabIndex = 12;
            this.button11.Text = "Ok";
            this.button11.UseVisualStyleBackColor = false;
            this.button11.Click += new EventHandler(this.button11_Click);
            this.button12.BackColor = Color.LightGray;
            this.button12.Location = new Point(314, 204);
            this.button12.Name = "button12";
            this.button12.Size = new Size(75, 49);
            this.button12.TabIndex = 13;
            this.button12.Text = "Clear";
            this.button12.UseVisualStyleBackColor = false;
            this.button12.Click += new EventHandler(this.button12_Click);
            this.button10.BackColor = Color.LightGray;
            this.button10.Location = new Point(314, 130);
            this.button10.Name = "button10";
            this.button10.Size = new Size(75, 49);
            this.button10.TabIndex = 14;
            this.button10.Text = "Cancel";
            this.button10.UseVisualStyleBackColor = false;
            this.button10.Click += new EventHandler(this.button10_Click);
            this.button13.BackColor = Color.LightGray;
            this.button13.Location = new Point(105, 271);
            this.button13.Name = "button13";
            this.button13.Size = new Size(75, 49);
            this.button13.TabIndex = 15;
            this.button13.Tag = (object)"0";
            this.button13.Text = "0";
            this.button13.UseVisualStyleBackColor = false;
            this.button13.Click += new EventHandler(this.OnNumberButtonPush);
            this.AutoScaleDimensions = new SizeF(6f, 13f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(419, 333);
            this.Controls.Add((Control)this.button13);
            this.Controls.Add((Control)this.button10);
            this.Controls.Add((Control)this.button12);
            this.Controls.Add((Control)this.button11);
            this.Controls.Add((Control)this.m_displayTextBox);
            this.Controls.Add((Control)this.button6);
            this.Controls.Add((Control)this.button9);
            this.Controls.Add((Control)this.button8);
            this.Controls.Add((Control)this.button7);
            this.Controls.Add((Control)this.button5);
            this.Controls.Add((Control)this.button4);
            this.Controls.Add((Control)this.button3);
            this.Controls.Add((Control)this.button2);
            this.Controls.Add((Control)this.button1);
            this.Name = "NumberPad";
            this.Text = "NumberPad";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
