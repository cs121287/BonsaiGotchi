using System;
using System.ComponentModel;

namespace BonsaiGotchi
{
    /// <summary>
    /// Designer file for the BonsaiGotchiForm
    /// </summary>
    public partial class BonsaiGotchiForm
    {
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.MinimumSize = new System.Drawing.Size(1000, 700);
            this.Text = "BonsaiGotchi";
            this.Icon = Properties.Resources.BonsaiIcon;
        }
    }
}