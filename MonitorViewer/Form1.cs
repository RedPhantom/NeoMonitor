using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MonitorViewer
{
    public partial class Form1 : Form
    {
        SaveFile saveFile = new SaveFile();

        //Header hdr = new Header();
        //List<Marking> Markings = new List<Marking>();
        List<DateTime> TimeAxis = new List<DateTime>();

        //short[] FHR_data;
        //short[] UC_data;

        double SelectionStart = 0;
        double SelectionEnd = 0;

        bool IsShiftPressed = false;
        bool IsSpacePressed = false;
        bool autoGain = false;

        int id = -1;

        int sampleInterval = 250; // sample interval in milliseconds

        DataMode? dataMode;

        enum DataMode
        {
            File,
            DB
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            KeyPreview = true;
            DbController.Init();
            saveToolStripMenuItem.Enabled = false;

            sampleInterval = Properties.Settings.Default.SampleInterval;
        }

        private void KeyDownE(object sender, KeyEventArgs e)
        {
            IsShiftPressed = e.Modifiers == Keys.Shift;
            IsSpacePressed = e.Modifiers == Keys.Space;

            if (e.KeyCode == Keys.Space)
                AddMarker();

            if (e.KeyCode == Keys.Delete)
                RemoveMarker();

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.S)
                SaveChanges(dataMode);

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.O)
                OpenFileDialog();
        }

        private void KeyUpE(object sender, KeyEventArgs e)
        {
            IsShiftPressed = e.Modifiers == Keys.Shift;
            IsSpacePressed = e.Modifiers == Keys.Space;
        }

        private void OpenDBDialog()
        {
            Reset();

            FileSelector frm = new FileSelector();
            var res = frm.ShowDialog();

            if (res == DialogResult.OK)
            {
                id = frm.SelectedID;
                LoadData(id);

                chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                chart2.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            }

            //ResetView();
        }

        private void OpenFileDialog()
        {
            var res = fd.ShowDialog();

            if (res == DialogResult.OK)
            {
                string fileName = fd.FileName;
                OpenFile(fileName);
            }

        }

        private void OpenFile(string fileName)
        {
            dataMode = DataMode.File;
            try
            {
                string XML = System.IO.File.ReadAllText(fileName);

                var serializer = new DataContractSerializer(typeof(SaveFile));

                // https://stackoverflow.com/questions/12554186/how-to-serialize-deserialize-to-dictionaryint-string-from-custom-xml-not-us
                // https://stackoverflow.com/questions/1879395/how-do-i-generate-a-stream-from-a-string

                saveFile = (SaveFile)serializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(XML ?? "")));

                TimeAxis.Add(new DateTime());

                for (int i = 0; i < saveFile.FHR_Data.Length; i++)
                    TimeAxis.Add(TimeAxis[i].AddMilliseconds(sampleInterval));

                ResetView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}");
                throw;
            }

            saveToolStripMenuItem.Enabled = true;
            Reset();
            ResetView();

            CreateChart();

            RenderHeader();
            RenderMarkers();
        }

        private void LoadData(int id)
        {
            dataMode = DataMode.DB;

            saveToolStripMenuItem.Enabled = true;

            Reset();
            ResetView();

            ShowLoading();

            DownloadHeader(id);
            DownloadData(id);
            DownloadMarkings(id);

            CreateChart();

            RenderHeader();
            RenderMarkers();

            HideLoading();
        }

        private void Reset()
        {
            chart1.Series.Clear();
            chart2.Series.Clear();

            SelectionStart = 0;
            SelectionEnd = 0;

            chart1.ChartAreas[0].AxisX.StripLines.Clear();
            chart2.ChartAreas[0].AxisX.StripLines.Clear();
        }

        private void RenderMarkers()
        {
            chart1.ChartAreas[0].AxisX.StripLines.Clear();
            chart2.ChartAreas[0].AxisX.StripLines.Clear();

            RefreshDGV();

            foreach (Marking m in saveFile.Markings)
            {
                AddStrip(m.to, m.tc, m.s, m.c);
            }
        }

        private void RefreshDGV()
        {
            dgvM.Rows.Clear();

            foreach (Marking m in saveFile.Markings)
                dgvM.Rows.Add(m.to, m.tc, m.c, m.s);
        }

        private void RenderHeader()
        {
            //tb.Text = "";
            //tb.Text += $"File name \t\t {hdr.Id}\n";
            //tb.Text += $"pH \t\t {hdr.pH}\n";
            //tb.Text += $"BDecf \t\t {hdr.BDecf}\n";
            //tb.Text += $"pCO2 \t\t {hdr.pCO2}\n";
            //tb.Text += $"BE \t\t {hdr.BE}\n";
            //tb.Text += $"Apgar 1 \t\t {hdr.Aggar1}\n";
            //tb.Text += $"Apgar 5 \t\t {hdr.Aggar2}\n";
            //tb.Text += $"Gest. Weeks \t\t {hdr.GestWeeks}\n";
            //tb.Text += $"Weight \t\t {hdr.Weight} g\n";
            //tb.Text += $"Sex \t\t {hdr.Sex}\n";
            //tb.Text += $"Age \t\t {hdr.Age}\n";
            //tb.Text += $"Gravidity \t\t {hdr.Gravidity}\n";
            //tb.Text += $"Parity \t\t {hdr.Parity}\n";
            List<Header> h = new List<Header>();
            h.Add(saveFile.Header);

            dataGridView1.DataSource = h;
        }

        void DownloadHeader(int ID)
        {
            SqlCommand cmd = new SqlCommand("SELECT * FROM tblHeaders WHERE [id] = @id", DbController.con);

            cmd.Parameters.AddWithValue("@id", ID);

            SqlDataReader dr = cmd.ExecuteReader();
            dr.Read();

            int i = 1;

            saveFile.Header.Id = ID;
            saveFile.Header.pH = dr.GetDouble(i); i++;
            saveFile.Header.BDecf = dr.GetDouble(i); i++;
            saveFile.Header.pCO2 = dr.GetDouble(i); i++;
            saveFile.Header.BE = dr.GetDouble(i); i++;
            saveFile.Header.Apgar1 = dr.GetInt32(i); i++;
            saveFile.Header.Apgar5 = dr.GetInt32(i); i++;
            saveFile.Header.GestWeeks = dr.GetInt32(i); i++;
            saveFile.Header.Weight = dr.GetInt32(i); i++;
            saveFile.Header.Sex = dr.GetInt32(i); i++;
            saveFile.Header.Age = dr.GetInt32(i); i++;
            saveFile.Header.Gravidity = dr.GetInt32(i); i++;
            saveFile.Header.Parity = dr.GetInt32(i); i++;
            saveFile.Header.Diabetes = dr.GetInt32(i); i++;
            saveFile.Header.Hypertension = dr.GetInt32(i); i++;
            saveFile.Header.Preeclampsia = dr.GetInt32(i); i++;
            saveFile.Header.Liqpraecox = dr.GetInt32(i); i++;
            saveFile.Header.Pyrexia = dr.GetInt32(i); i++;
            saveFile.Header.Meconium = dr.GetInt32(i); i++;
            saveFile.Header.Presentation = dr.GetInt32(i); i++;
            saveFile.Header.Induced = dr.GetInt32(i); i++;
            saveFile.Header.Istage = dr.GetInt32(i); i++;
            saveFile.Header.NoProgress = dr.GetInt32(i); i++;
            saveFile.Header.CKKP = dr.GetInt32(i); i++;
            saveFile.Header.Rectype = dr.GetInt32(i); i++;
            saveFile.Header.Pos2st = dr.GetInt32(i); i++;
            saveFile.Header.Sig2Birth = dr.GetInt32(i); i++;

            dr.Close();
        }

        void DownloadData(int ID)
        {
            SqlCommand cmd = new SqlCommand("SELECT * FROM tblData WHERE id = @id", DbController.con);
            cmd.Parameters.AddWithValue("@id", ID);

            SqlDataReader dr = cmd.ExecuteReader();
            dr.Read();

            byte[] rd = dr.GetSqlBinary(1).Value;

            List<short> FHR = new List<short>();
            List<short> UC = new List<short>();

            TimeAxis.Add(new DateTime());

            switch (saveFile.SavingAlgorithm)
            {
                case SaveFile.Algorithm.DumbDB:
                    GenerateDataList_AlgoDumbDB(ref FHR, ref UC, rd);
                    break;
                default:
                    break;
            }

            saveFile.FHR_Data = FHR.ToArray();
            saveFile.UC_Data = UC.ToArray();

            dr.Close();
        }

        void DownloadMarkings(int ID)
        {
            SqlCommand cmd = new SqlCommand("SELECT * FROM tblMarkings WHERE id = @id", DbController.con);

            cmd.Parameters.AddWithValue("@id", ID);

            SqlDataReader dr = cmd.ExecuteReader();
            dr.Read();

            if (dr.HasRows)
                if (!dr.IsDBNull(1))
                    saveFile.Markings = Marking.GetMarkings(dr.GetString(1));

            dr.Close();

            Text = "NeoMonitor Viewer - ";
            Text += ID.ToString();
        }

        void CreateChart()
        {
            Loading ldg = new Loading();

            ldg.Show();

            //MessageBox.Show("Generating graphics. This may take some time depending on the data size.");

            chart1.Series.Clear();
            chart2.Series.Clear();

            Series FHRSeries = new Series("FetalHeartRate");
            Series UCSeries = new Series("UterineContractions");

            // FHRSeries.AxisLabel = "Time (sec)";
            FHRSeries.Color = Color.Black;
            FHRSeries.BorderWidth = 2;
            FHRSeries.ChartType = SeriesChartType.Line;

            //UCSeries.AxisLabel = "Time (sec}";
            UCSeries.Color = Color.Blue;
            UCSeries.BorderWidth = 2;
            UCSeries.ChartType = SeriesChartType.Line;

            // Generat the series using the DumbDB algorith (alternating 16-bit datapoints, with LSB to MSB (byte 2 + byte 1).
            GenerateSeries(ref FHRSeries, ref UCSeries);

            chart1.Series.Add(FHRSeries);
            chart2.Series.Add(UCSeries);

            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = false;

            chart2.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart2.ChartAreas[0].CursorY.IsUserSelectionEnabled = false;

            chart1.Series[0].XValueType = ChartValueType.Time;
            chart2.Series[0].XValueType = ChartValueType.Time;

            chart1.ChartAreas[0].AxisX.Interval = 0;
            chart1.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chart1.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Milliseconds;
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss";
            chart1.ChartAreas[0].AxisX.Title = "Fetal Heart Rate";
            chart1.Series[0].IsXValueIndexed = true;

            chart2.ChartAreas[0].AxisX.Interval = 0;
            chart2.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chart2.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Milliseconds;
            chart2.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss";
            chart2.ChartAreas[0].AxisX.Title = "Uterine Contractions";
            chart2.Series[0].IsXValueIndexed = true;

            chart1.Legends.Clear();
            chart2.Legends.Clear();

            //MessageBox.Show("Graphics processing complete. You may proceed now.");
            ldg.Close();
        }

        // All GenerateDataList functions must write to a List<short> by ref with a given form of data.
        private void GenerateDataList_AlgoDumbDB(ref List<short> FHR_Data, ref List<short> UC_Data, byte[] RawData)
        {
            bool f = true; // used to switch between FHR and UC
            //const int baseline = 0;
            int timeIndex = 0;

            for (int i = 0; i < RawData.Length; i += 2)
            {
                short word = (short)(RawData[i + 1] << 8 | RawData[i] << 16); // LSByte first and MSByte second.

                if (f)
                {
                    //if (word == 0)
                    //    word = baseline;

                    FHR_Data.Add(word);
                }
                else
                    UC_Data.Add(word);

                f = !f; // alternating the data.

                TimeAxis.Add(TimeAxis[timeIndex].AddMilliseconds(sampleInterval));
                timeIndex++;
            }
        }

        // Must take data from the saveFile and write it to two or more given Series.
        private void GenerateSeries(ref Series FHRSeries, ref Series UCSeries)
        {
            int x = 0;

            int FHRminX = short.MaxValue;
            int UCminX = short.MaxValue;
            int FHRmin = short.MaxValue;
            int UCmin = short.MaxValue;

            for (int i = 0; i < saveFile.FHR_Data.Length; i++)
            {
                if (saveFile.FHR_Data[i] < FHRmin && saveFile.FHR_Data[i] > 0)
                {
                    FHRminX = i;
                    FHRmin = saveFile.FHR_Data[i];
                }

                if (saveFile.UC_Data[i] < UCmin && saveFile.UC_Data[i] > 0)
                {
                    UCminX = i;
                    UCmin = saveFile.UC_Data[i];
                }
            }

            foreach (short fhr in saveFile.FHR_Data)
            {
                if (autoGain)
                {
                    if (fhr == 0)
                        FHRSeries.Points.AddXY(TimeAxis[x], saveFile.FHR_Data[FHRminX]);
                    else
                        FHRSeries.Points.AddXY(TimeAxis[x], fhr);

                    if (saveFile.UC_Data[x] == 0)
                        UCSeries.Points.AddXY(TimeAxis[x], saveFile.UC_Data[UCminX]);
                    else
                        UCSeries.Points.AddXY(TimeAxis[x], saveFile.UC_Data[x]);

                    chart1.ChartAreas[0].AxisY.Minimum = FHRmin;
                    chart2.ChartAreas[0].AxisY.Minimum = UCmin;
                }
                else
                {
                    if (fhr == 0)
                        FHRSeries.Points.AddXY(TimeAxis[x], saveFile.FHR_Data[x]);
                    else
                        FHRSeries.Points.AddXY(TimeAxis[x], fhr);

                    if (saveFile.UC_Data[x] == 0)
                        UCSeries.Points.AddXY(TimeAxis[x], saveFile.UC_Data[x]);
                    else
                        UCSeries.Points.AddXY(TimeAxis[x], saveFile.UC_Data[x]);

                    chart1.ChartAreas[0].AxisY.Minimum = 0;
                    chart2.ChartAreas[0].AxisY.Minimum = 0;
                }
                x++;
            }
        }

        void AddMarker()
        {
            MarkingForm mf = new MarkingForm();

            var r = mf.ShowDialog();

            if (r == DialogResult.OK)
            {
                saveFile.Markings.Add(new Marking(mf.Code, mf.Comment, SelectionStart, SelectionEnd));

                AddStrip(SelectionStart, SelectionEnd, mf.Comment, mf.Code);

                RefreshDGV();
            }
        }

        private void AddStrip(double selectionStart, double selectionEnd, string Comment, int Code)
        {
            StripLine limit_lower_strip = new StripLine();

            limit_lower_strip.Interval = 0;
            limit_lower_strip.IntervalOffset = Math.Min(selectionStart, selectionEnd);

            double width = 0;

            if (selectionEnd - selectionStart < 0)
                width = selectionStart - selectionEnd;
            else
                width = selectionEnd - selectionStart;

            limit_lower_strip.StripWidth = width;

            if (Code == 0)
            {
                limit_lower_strip.BorderColor = Color.FromArgb(230, Color.Blue);
                limit_lower_strip.BackColor = Color.FromArgb(30, Color.Blue);
            }

            if (Code == 1)
            {
                limit_lower_strip.BorderColor = Color.FromArgb(230, Color.Red);
                limit_lower_strip.BackColor = Color.FromArgb(30, Color.Red);
            }

            limit_lower_strip.BorderDashStyle = ChartDashStyle.Solid;
            limit_lower_strip.BorderWidth = 3;

            limit_lower_strip.Text = Comment;

            if (Comment.Length * 100 < width)
                limit_lower_strip.TextOrientation = TextOrientation.Horizontal;

            chart1.ChartAreas[0].AxisX.StripLines.Add(limit_lower_strip);
            chart2.ChartAreas[0].AxisX.StripLines.Add(limit_lower_strip);
        }

        void RemoveMarker()
        {
            if (dgvM.SelectedCells.Count != 0)
                saveFile.Markings.RemoveAt(dgvM.SelectedCells[0].RowIndex);

            CreateChart();

            RefreshDGV();

            RenderMarkers();
        }

        private void chart1_AxisViewChanged(object sender, ViewEventArgs e)
        {
            chart2.ChartAreas[0].AxisX.ScaleView = chart1.ChartAreas[0].AxisX.ScaleView;
        }

        private void chart2_AxisViewChanged(object sender, ViewEventArgs e)
        {
            chart1.ChartAreas[0].AxisX.ScaleView = chart2.ChartAreas[0].AxisX.ScaleView;
        }

        private void tsbResetView_Click(object sender, EventArgs e)
        {
            ResetView();
        }

        private void ResetView()
        {
            chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset();
            chart2.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            chart2.ChartAreas[0].AxisY.ScaleView.ZoomReset();
        }

        private void chart1_MosueMove(object sender, MouseEventArgs e)
        {
            // todo: add location at the status strip
        }

        private void chart2_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void chart1_SelectionChange(object sender, CursorEventArgs e)
        {
            lblSelection.Text = $"Selection: {SelectionStart} - {SelectionEnd} ({FormatTime(SelectionStart, SelectionEnd)})";
        }

        private object FormatTime(double selectionStart, double selectionEnd)
        {
            return new TimeSpan(0, 0, 0,0, (int)SelectionStart / 4 * 1000).ToString(@"hh\:mm\:ss\.fff") + " - " + new TimeSpan(0, 0, 0,0, (int)SelectionEnd / 4 * 1000).ToString(@"hh\:mm\:ss\.fff");
        }

        private void chart2_SelectionChange(object sender, CursorEventArgs e)
        {
            lblSelection.Text = $"Selection: {SelectionStart} - {SelectionEnd} ({FormatTime(SelectionStart, SelectionEnd)})";
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = false;
            chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = false;

            if (IsShiftPressed)
            {
                chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
                chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = false;
            }
        }

        private void SelectionChange(object sender, CursorEventArgs e)
        {
            SelectionStart = e.NewSelectionStart;
            SelectionEnd = e.NewSelectionEnd;

            lblSelection.Text = $"Selection: {SelectionStart} - {SelectionEnd} ({FormatTime(SelectionStart, SelectionEnd)})";
        }

        private void chart2_MosueDown(object sender, MouseEventArgs e)
        {
            if (IsShiftPressed)
            {
                chart2.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
                chart2.ChartAreas[0].AxisY.ScaleView.Zoomable = false;
            } else
            {
                chart2.ChartAreas[0].AxisX.ScaleView.Zoomable = false;
                chart2.ChartAreas[0].AxisY.ScaleView.Zoomable = false;
            }
        }

        private void SaveChanges(DataMode? dataMode)
        {
            try
            {
                if (dataMode == DataMode.DB)
                {
                    SqlCommand cmd = new SqlCommand(@"
                    IF EXISTS (SELECT * FROM tblMarkings WHERE [id]=@id)
                        UPDATE tblMarkings SET markingData = @m WHERE id = @id 
                    ELSE
                        INSERT INTO tblMarkings (id, markingData) VALUES (@id, @m)", DbController.con);

                    cmd.Parameters.AddWithValue("@id", saveFile.Header.Id);
                    cmd.Parameters.AddWithValue("@m", Marking.GetXML(saveFile.Markings));

                    if (cmd.ExecuteNonQuery() != 0)
                        MessageBox.Show("Save successful.");
                }

                if (dataMode == DataMode.File)
                {
                    if (string.IsNullOrEmpty(Properties.Settings.Default.UserDisplayName))
                    {
                        MessageBox.Show("You must enter your name in the Settings dialog.");
                        return;
                    }

                    saveFile.Metadata.AuthorName = Properties.Settings.Default.UserDisplayName;

                    SaveFileDialog sf = new SaveFileDialog();

                    sf.Filter = "XML File|*.xml";
                    sf.Title = "Save...";

                    var res = sf.ShowDialog();

                    if (res == DialogResult.OK)
                    {
                        string fileName = sf.FileName;
                        File.WriteAllText(fileName, saveFile.GetXML());
                        MessageBox.Show("Save successful.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed: {ex.Message}");
            }    
        }

        void ShowLoading()
        {
            pb.Visible = true;
        }

        void HideLoading()
        {
            pb.Visible = false;
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void fromDataBaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenDBDialog();
        }

        private void fromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog();
        }

        private void reloadDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadData();
        }

        void ReloadData()
        {
            if (id != -1)
                LoadData(id);
        }
        private void resetViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetView();
        }

        private void toDataBaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveChanges(DataMode.DB);
        }

        private void toFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveChanges(DataMode.File);
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Print();
        }

        private new void Closing(object sender, FormClosingEventArgs e)
        {
            var r = MessageBox.Show("Really quit?", "Quit?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if (r == DialogResult.Yes)
                Application.Exit();
            else
                e.Cancel = true;
        }

        private void eCGEKGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (chart1.Series[0].Points.Count < 60*60) // 3600 samples = 60 seconds at 60 Hz.
            {
                chart1.ChartAreas[0].AxisX.MajorGrid.Interval = 0.2; // big box every 0.2 seconds.
                chart1.ChartAreas[0].AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Seconds;
                chart1.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.Gray;

                chart1.ChartAreas[0].AxisX.MinorGrid.Interval =  0.04; // small box every 0.04 seconds.
                chart1.ChartAreas[0].AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Seconds;
                chart1.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.LightGray;

                chart2.ChartAreas[0].AxisX.MajorGrid.Interval = 0.2; // big box every 0.2 seconds.
                chart2.ChartAreas[0].AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Seconds;
                chart2.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.Gray;

                chart2.ChartAreas[0].AxisX.MinorGrid.Interval = 0.04; // small box every 0.04 seconds.
                chart2.ChartAreas[0].AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Seconds;
                chart2.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.LightGray;
            } else
                MessageBox.Show(this, $"Too much data. There must be at most a total of 3600 samples, now there are {chart1.Series[0].Points.Count}.", "Too much data");
        }

        private void fetalMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chart1.ChartAreas[0].AxisX.MajorGrid.Interval = 30; // big box every 30 seconds.
            chart1.ChartAreas[0].AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.Gray;

            chart1.ChartAreas[0].AxisX.MinorGrid.Interval = 120; // small box every 10 seconds.
            chart1.ChartAreas[0].AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.LightGray;

            chart2.ChartAreas[0].AxisX.MajorGrid.Interval = 30; // big box every 30 seconds.
            chart2.ChartAreas[0].AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Seconds;
            chart2.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.Gray;

            chart2.ChartAreas[0].AxisX.MinorGrid.Interval = 120; // small box every 10 seconds.
            chart2.ChartAreas[0].AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Seconds;
            chart2.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.LightGray;
        }

        void ZoomSelection()
        {
            if (SelectionStart != -1 && SelectionEnd != -1)
                chart1.ChartAreas[0].AxisX.ScaleView.Zoom(SelectionStart, SelectionEnd);

            else MessageBox.Show("No selection is made.");
        }

        private void zoomSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomSelection();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmSettings fs = new frmSettings();
            fs.Show();
        }

        private PrintPreviewDialog printPreviewDialog1 = new PrintPreviewDialog();
        
        void Print()
        {
            PrintDialog printDialog = new PrintDialog();

            printDialog.AllowPrintToFile = true;
            printDialog.AllowSomePages = true;

            var res = printDialog.ShowDialog();

            if (res == DialogResult.OK)
            {
                PrintDocument pd = new PrintDocument();

                pd.PrinterSettings.PrinterName = printDialog.PrinterSettings.PrinterName;
                pd.PrintPage += new PrintPageEventHandler(pd_PrintPage);
                pd.DefaultPageSettings.Landscape = true;

                printPreviewDialog1.Document = pd;
                printPreviewDialog1.ShowDialog();
            }
        }

        private void pd_PrintPage(object sender, PrintPageEventArgs ev)
        {
            ResetView();

            // Create and initialize print font 
            Font printFont = new Font("Arial", 10);

            // Create Rectangle structure, used to set the position of the chart Rectangle 
            Rectangle rec1 = new Rectangle(0, 50, 1100, 260);
            Rectangle rec2 = new Rectangle(0, 280, 1100, 260);

            // Draw a line of text, followed by the chart, and then another line of text 

            //chart1.Printing.PrintPaint(ev.Graphics, rec1);
            //chart2.Printing.PrintPaint(ev.Graphics, rec2);

            Bitmap chartPanel1 = new Bitmap(splitContainer2.Panel1.Width, splitContainer2.Panel1.Height);
            Bitmap chartPanel2 = new Bitmap(splitContainer2.Panel2.Width, splitContainer2.Panel2.Height);

            splitContainer2.Panel1.DrawToBitmap(chartPanel1, new Rectangle(0, 0, splitContainer2.Panel1.Width, splitContainer2.Panel1.Height));
            splitContainer2.Panel2.DrawToBitmap(chartPanel2, new Rectangle(0, 0, splitContainer2.Panel2.Width, splitContainer2.Panel2.Height));

            ev.Graphics.DrawImage(chartPanel1, ev.MarginBounds.X - 120, 50, 1100, splitContainer2.Panel1.Height);
            ev.Graphics.DrawImage(chartPanel2, ev.MarginBounds.X - 120, 450, 1100, splitContainer2.Panel2.Height);
            
            ev.Graphics.DrawString(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), printFont, Brushes.Black, 10, 20);
            ev.Graphics.DrawString("Author: " + Properties.Settings.Default.UserDisplayName, printFont, Brushes.Black, 380, 20);
            ev.Graphics.DrawString("Generated & Printed by NeoMonitor Viewer" + Properties.Settings.Default.UserDisplayName, printFont, Brushes.Black, 800, 20);
        }

        private void autoGainToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            autoGain = !autoGain;
            ReloadData();
        }
    }
}
