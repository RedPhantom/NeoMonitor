using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorViewer
{
    public static class DbController
    {
        public static SqlConnection con { get; set; }

        public static void Init()
        {
            try
            {
                con = new SqlConnection(@"Server=BOBTOP\SQLEXPRESS;Database=NeoMonitor;Integrated Security=True;");
                con.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not initialize a database connection: {ex.Message}");
                throw;
            }
           
        }
    }
}
