using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Redbox.HAL.Common.GUI.Functions
{
    public class NumberPadForm : Form
    {
        private IContainer components;
        private NumberPad m_numberPad;
        private Button m_okButton;
        private Button m_cancelButton;

        public NumberPadForm() => this.InitializeComponent();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Number
        {
            get => this.m_numberPad.Number;
            set => this.m_numberPad.Text = value;
        }

        private void OnOK(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnCancel(object sender, EventArgs e)
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
            this.m_numberPad = new NumberPad();
            this.m_okButton = new Button();
            this.m_cancelButton = new Button();
            this.SuspendLayout();
            this.m_numberPad.Dock = DockStyle.Fill;
            this.m_numberPad.Location = new Point(3, 3);
            this.m_numberPad.Name = "m_numberPad";
            this.m_numberPad.Size = new Size(378, 260);
            this.m_numberPad.TabIndex = 0;
            this.m_okButton.Location = new Point(289, 152);
            this.m_okButton.Name = "m_okButton";
            this.m_okButton.Size = new Size(89, 48);
            this.m_okButton.TabIndex = 1;
            this.m_okButton.Text = "OK";
            this.m_okButton.UseVisualStyleBackColor = true;
            this.m_okButton.Click += new EventHandler(this.OnOK);
            this.m_cancelButton.DialogResult = DialogResult.Cancel;
            this.m_cancelButton.Location = new Point(289, 206);
            this.m_cancelButton.Name = "m_cancelButton";
            this.m_cancelButton.Size = new Size(89, 54);
            this.m_cancelButton.TabIndex = 2;
            this.m_cancelButton.Text = "Cancel";
            this.m_cancelButton.UseVisualStyleBackColor = true;
            this.m_cancelButton.Click += new EventHandler(this.OnCancel);
            this.AcceptButton = (IButtonControl)this.m_okButton;
            this.AutoScaleDimensions = new SizeF(6f, 13f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = (IButtonControl)this.m_cancelButton;
            this.ClientSize = new Size(384, 266);
            this.ControlBox = false;
            this.Controls.Add((Control)this.m_cancelButton);
            this.Controls.Add((Control)this.m_okButton);
            this.Controls.Add((Control)this.m_numberPad);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Name = nameof(NumberPadForm);
            this.Padding = new Padding(3);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Enter Number";
            this.ResumeLayout(false);
        }
    }
}
