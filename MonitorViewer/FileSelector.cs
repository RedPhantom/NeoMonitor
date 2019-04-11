using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorViewer
{
    public partial class FileSelector : Form
    {
        public int SelectedID { get; set; }
        bool selectionMade = false;

        public FileSelector()
        {
            InitializeComponent();
        }

        private void FileSelector_Load(object sender, EventArgs e)
        {
            progressBar1.Visible = true;
            progressBar1.Style = ProgressBarStyle.Marquee;

            var FileNamesTask = Task.Run(() =>
            {
                List<int> HeaderIDs = new List<int>();

                // load all header file names:
                SqlCommand cmd = new SqlCommand("SELECT [id] FROM [tblHeaders]", DbController.con);
                SqlDataReader headerDR = cmd.ExecuteReader();

                while (headerDR.Read())
                    HeaderIDs.Add(headerDR.GetInt32(0));

                headerDR.Close();
                return HeaderIDs;
            }).ContinueWith(hid =>
            {
                foreach (var item in hid.Result)
                {
                    listBox1.Items.Add(item);
                }

                progressBar1.Visible = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void LoadInformation()
        {

        }

        private void ListBoxSelectionChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count == 1)
            {
                SelectedID = int.Parse(listBox1.SelectedItems[0].ToString());
                btnOpen.Enabled = true;
            }
        }
    }
}
