using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scoutmans.SwissArmyKnife
{
    public partial class frmAbout : Form
    {
        public frmAbout()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.scoutman.pt");
        }

        private void label2_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/ScoutmanPt/Swiss.Army.Knife");
        }
    }
}
