using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorViewer
{
    public partial class frmSettings : Form
    {
        public frmSettings()
        {
            InitializeComponent();
        }

        private void frmSettings_Load(object sender, EventArgs e)
        {
            numSampleInterval.Value = Properties.Settings.Default.SampleInterval;
            txtDisplayName.Text = Properties.Settings.Default.UserDisplayName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.SampleInterval = (int)numSampleInterval.Value;
            Properties.Settings.Default.UserDisplayName = txtDisplayName.Text;
            Close();
        }

        private void cbFreq_Changed(object sender, EventArgs e)
        {
            switch (cbFreq.SelectedIndex)
            {
                case 0:
                    numSampleInterval.Value = 1000 / 4;
                    break;

                case 1:
                    numSampleInterval.Value = 1000 / 40;
                    break;

                case 2:
                    numSampleInterval.Value = 1000 / 60;
                    break;

                default:
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"Setting a value here is important for showing an accurate timescale 
at the bottom of the graphs. Rounding a 0.6ms interval will cause 
a shift of 60 ms in a 100ms segment, for example.");
        }
    }
}
