using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace MonitorViewer
{
    public partial class MarkingForm : Form
    {
        public string Comment { get; set; }
        public int Code { get; set; }


        public MarkingForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Comment = textBox1.Text;
            Code = comboBox1.SelectedIndex;
        }
    }
}
