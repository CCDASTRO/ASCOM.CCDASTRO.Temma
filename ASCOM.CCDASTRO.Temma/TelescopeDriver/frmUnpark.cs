using System;
using System.Windows.Forms;

namespace ASCOM.CCDASTROTemma.Telescope
{
    public partial class frmUnpark : Form
    {
        private Button btnCurrent;
        private Button btnStored;
        private Button btnCancel;

        public bool UseStoredPosition { get; private set; }

        public frmUnpark()
        {
            Text = "Unpark Telescope";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(320, 120);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            btnCurrent = new Button();
            btnCurrent.Text = "Use Current Position";
            btnCurrent.SetBounds(10, 20, 140, 30);
            btnCurrent.Click += BtnCurrent_Click;

            btnStored = new Button();
            btnStored.Text = "Use Stored Park";
            btnStored.SetBounds(160, 20, 140, 30);
            btnStored.Click += BtnStored_Click;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.SetBounds(115, 70, 90, 28);
            btnCancel.Click += BtnCancel_Click;

            Controls.Add(btnCurrent);
            Controls.Add(btnStored);
            Controls.Add(btnCancel);

            AcceptButton = btnCurrent;
            CancelButton = btnCancel;
        }

        private void BtnCurrent_Click(object sender, EventArgs e)
        {
            UseStoredPosition = false;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnStored_Click(object sender, EventArgs e)
        {
            UseStoredPosition = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}

