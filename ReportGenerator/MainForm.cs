using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ReportGenerator
{
    struct Configuration
    {
        public int gracePeriod;
        public int time_in_hour;
        public int time_in_minutes;
        public int time_out_hour;
        public int time_out_minutes;
        public int lunch_start_hour;
        public int lunch_start_min;
        public int lunch_end_hour;
        public int lunch_end_min;
        public string inputPath;
    }

    public partial class MainForm : Form
    {
        DataTable m_dtReport;
        List<worker> m_listWorkers = new List<worker>();
        FolderBrowserDialog getFolderDir = new FolderBrowserDialog();
        string configFilepath = Path.Combine(Directory.GetCurrentDirectory(), "config.xml");

        public MainForm()
        {
            InitializeComponent();
            // Main Form Size
            //this.Size = new Size(1250, 850);
            this.WindowState = FormWindowState.Normal;
            var defaultVal = this.getDefaultValues();

            // Grace Period
            this.gracePeriod.Value = defaultVal.gracePeriod;

            // Time picker valid time-in and time-out
            this.timePickerTimeIn.Format = DateTimePickerFormat.Time;
            this.timePickerTimeIn.ShowUpDown = true;
            this.timePickerTimeOut.Format = DateTimePickerFormat.Time;
            this.timePickerTimeOut.ShowUpDown = true;

            DateTime current = DateTime.Now;
            TimeSpan tsIn = new TimeSpan(defaultVal.time_in_hour, defaultVal.time_in_minutes, 0);
            this.timePickerTimeIn.Value = current.Date + tsIn;
            TimeSpan tsOut = new TimeSpan(defaultVal.time_out_hour, defaultVal.time_out_minutes, 0);
            this.timePickerTimeOut.Value = current.Date + tsOut;

            // Time picker lunch start and lunch end
            this.timePickerLunchStart.Format = DateTimePickerFormat.Time;
            this.timePickerLunchStart.ShowUpDown = true;
            this.timePickerLunchEnd.Format = DateTimePickerFormat.Time;
            this.timePickerLunchEnd.ShowUpDown = true;

            TimeSpan lunchIn = new TimeSpan(defaultVal.lunch_start_hour, defaultVal.lunch_start_min, 0);
            this.timePickerLunchStart.Value = current.Date + lunchIn;
            TimeSpan lunchOut = new TimeSpan(defaultVal.lunch_end_hour, defaultVal.lunch_end_min, 0);
            this.timePickerLunchEnd.Value = current.Date + lunchOut;

            // Last input folder used
            this.InputPath.Text = defaultVal.inputPath;

            // Set startDate to -7 days from today
            startPeriodPicker.Value = DateTime.Today.AddDays(-6);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)

        {
            XElement config = new XElement("Settings",
                new XElement("Grace_period", this.gracePeriod.Value.ToString()),
                new XElement("Valid_time_in", this.timePickerTimeIn.Value.Hour.ToString() + ":" + this.timePickerTimeIn.Value.Minute.ToString()),
                new XElement("Valid_time_out", this.timePickerTimeOut.Value.Hour.ToString() + ":" + this.timePickerTimeOut.Value.Minute.ToString()),
                new XElement("Lunch_start", this.timePickerLunchStart.Value.Hour.ToString() + ":" + this.timePickerLunchStart.Value.Minute.ToString()),
                new XElement("Lunch_end", this.timePickerLunchEnd.Value.Hour.ToString() + ":" + this.timePickerLunchEnd.Value.Minute.ToString()),
                new XElement("Last_folder_path", this.InputPath.Text)
            );
            config.Save(this.configFilepath);
        }

        private Configuration getDefaultValues()
        {
            Configuration defValues = new Configuration();
            if (File.Exists(this.configFilepath))
            {
                // Get default values from xml
                XElement config = XElement.Load(this.configFilepath);

                var gracePeriod = (int)config.Descendants("Grace_period").First();
                var valid_in = ((string)config.Descendants("Valid_time_in").First()).Split(':');
                var valid_out = ((string)config.Descendants("Valid_time_out").First()).Split(':');
                var lunch_start = ((string)config.Descendants("Lunch_start").First()).Split(':');
                var lunch_end = ((string)config.Descendants("Lunch_end").First()).Split(':');
                var inputFolder = (string)config.Descendants("Last_folder_path").First();

                defValues.gracePeriod = gracePeriod;
                defValues.time_in_hour = Convert.ToInt32(valid_in[0]);
                defValues.time_in_minutes = Convert.ToInt32(valid_in[1]);
                defValues.time_out_hour = Convert.ToInt32(valid_out[0]);
                defValues.time_out_minutes = Convert.ToInt32(valid_out[1]);
                defValues.lunch_start_hour = Convert.ToInt32(lunch_start[0]);
                defValues.lunch_start_min = Convert.ToInt32(lunch_start[1]);
                defValues.lunch_end_hour = Convert.ToInt32(lunch_end[0]);
                defValues.lunch_end_min = Convert.ToInt32(lunch_end[1]);
                defValues.inputPath = inputFolder;
            }
            else
            {
                // Default Values
                defValues.gracePeriod = 15;
                defValues.time_in_hour = 7;
                defValues.time_in_minutes = 0;
                defValues.time_out_hour = 16;
                defValues.time_out_minutes = 0;
                defValues.lunch_start_hour = 12;
                defValues.lunch_start_min = 0;
                defValues.lunch_end_hour = 13;
                defValues.lunch_end_min = 0;
                defValues.inputPath = "";
            }

            return defValues;
        }

        private void browse_Click(object sender, EventArgs e)
        {
            DialogResult result = getFolderDir.ShowDialog();
            if (result == DialogResult.OK)
            {
                InputPath.Text = getFolderDir.SelectedPath;
            }
        }

        private void generate_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(InputPath.Text))
            {
                MessageBox.Show("The folder path does not exist.", "Invalid Path");
            } 
            else
            {
                // For re-executing generate button
                m_listWorkers.Clear();
                m_dtReport = new DataTable();
                dataGridView1.Columns.Clear();

                string[] workerDirs = Directory.GetDirectories(InputPath.Text);

                // Analyze Image Reports for each worker directory
                foreach (string dir in workerDirs)
                {
                    worker tmpWorker = new worker(dir);
                    string[] imageList = Directory.GetFiles(dir, "*.jpg");

                    foreach (string image in imageList)
                    {
                        tmpWorker.analyzeImageReport(image);
                    }

                    m_listWorkers.Add(tmpWorker);
                }
                
                // Create column headers from start and end period
                DateTime startPeriod = startPeriodPicker.Value;
                DateTime endPeriod = endPeriodPicker.Value;

                m_dtReport.Columns.Add("Name", typeof(string));
                m_dtReport.Columns.Add("Total Work Hours", typeof(int));

                for (DateTime date = startPeriod; date.Date <= endPeriod.Date; date = date.AddDays(1.0))
                {
                    if (!(date.ToString("ddd") == "Sun"))
                    {
                        DataColumn tempCol = new DataColumn();
                        tempCol.ColumnName = date.ToString("dddd") + Environment.NewLine + date.ToString("yyyy-MM-dd");
                        tempCol.DataType = typeof(string);
                        tempCol.DefaultValue = "Absent";
                        m_dtReport.Columns.Add(tempCol);
                    }
                }

                // Generate report
                foreach (worker worker in m_listWorkers)
                {
                    int totalWorkHours = 0;
                    DataRow row = m_dtReport.NewRow();
                    row["Name"] = worker.m_name; 


                    foreach (attendance att in worker.m_attendanceList)
                    {
                        if (att.timeIn.Date >= startPeriod.Date && att.timeIn.Date <= endPeriod.Date && att.timeIn.ToString("ddd") != "Sun")
                        {
                            string timeIn = "";
                            string timeout = "";
                            string adjustedTimeIn = "";
                            string adjustedTimeOut = "";
                            int workedHours = 0;

                            // No image for timeout
                            if (att.timeOut == default(DateTime))
                            {
                                timeIn = "Time-in: " + att.timeIn.ToString("hh:mm tt") + Environment.NewLine;
                                timeout = "Time-out: None" + Environment.NewLine + Environment.NewLine;
                                workedHours = 0;
                            }
                            else
                            {
                                // Check if time-in is earlier than valid time-in
                                DateTime validIn = att.timeIn.Date + timePickerTimeIn.Value.TimeOfDay;
                                DateTime workerValIn = att.timeIn < validIn ? validIn : att.timeIn;

                                // Create lunch time-in and time-out
                                DateTime lunchTimeIn = workerValIn.Date + timePickerLunchStart.Value.TimeOfDay;
                                DateTime lunchTimeOut = workerValIn.Date + timePickerLunchEnd.Value.TimeOfDay;

                                TimeSpan tempIn = workerValIn.Subtract(validIn);

                                // Adjust time-in if worker is late
                                if (tempIn.Minutes > (double) gracePeriod.Value)
                                {
                                    double minDiff = 60 - tempIn.Minutes;
                                    workerValIn = validIn.AddMinutes(tempIn.TotalMinutes + minDiff);
                                }
                                workerValIn = new DateTime(workerValIn.Year, workerValIn.Month, workerValIn.Day, workerValIn.Hour, validIn.Minute, 0);

                                // If worker in is within lunch time
                                if (lunchTimeIn < workerValIn && workerValIn < lunchTimeOut)
                                {
                                    workerValIn = lunchTimeOut;
                                }

                                // Check if time-out is later than valid time-out
                                DateTime validOut = att.timeOut.Date + timePickerTimeOut.Value.TimeOfDay;
                                DateTime workerValOut = att.timeOut > validOut ? validOut : att.timeOut;

                                // Compute worked hours for the day (for worker out adjustment)
                                TimeSpan tempOut = workerValOut.Subtract(workerValIn);

                                // Adjust time-out
                                if (tempOut.Minutes > 0 && tempOut.Hours > 0)
                                {
                                    workerValOut = workerValIn.AddMinutes(tempOut.TotalMinutes - tempOut.Minutes);
                                }

                                // If worker out is within lunch time
                                if (lunchTimeIn < workerValOut && workerValOut < lunchTimeOut)
                                {
                                    workerValOut = lunchTimeIn;
                                }

                                // Compute final worked hours
                                double workhours = workerValOut.Subtract(workerValIn).TotalHours;

                                if (workhours > 0)
                                {
                                    // If worker render lunch time
                                    if (workerValIn < lunchTimeOut && workerValOut > lunchTimeOut)
                                    {
                                        workhours -= lunchTimeOut.Subtract(lunchTimeIn).TotalHours;
                                    }
                                    adjustedTimeIn = "In: " + workerValIn.ToString("hh:mm tt") + Environment.NewLine;
                                    adjustedTimeOut = "Out: " + workerValOut.ToString("hh:mm tt") + Environment.NewLine + Environment.NewLine;
                                }
                                else
                                {
                                    workhours = 0;
                                }
                                workedHours = (int)workhours;
                                timeIn = "Time-in: " + att.timeIn.ToString("hh:mm tt") + Environment.NewLine;
                                timeout = "Time-out: " + att.timeOut.ToString("hh:mm tt") + Environment.NewLine + Environment.NewLine;
                            }
                            // Add to total
                            totalWorkHours += workedHours;
                            // Update workedHours of attendance
                            att.setWorkedHours(workedHours);
                            string rowStr = timeIn + timeout + adjustedTimeIn + adjustedTimeOut + $"Hours worked: {workedHours}";
                            // Add to Row
                            row[att.timeIn.Date.ToString("dddd") + Environment.NewLine + att.timeIn.Date.ToString("yyyy-MM-dd")] = rowStr;
                        }
                    }
                    row["Total Work Hours"] = totalWorkHours;
                    m_dtReport.Rows.Add(row);
                }

                // Add dataSource to dataGrid
                dataGridView1.DataSource = m_dtReport;
                dataGridView1.Columns[0].Width = 125;
                dataGridView1.Columns[0].Frozen = true;
                dataGridView1.Columns[1].Width = 45;
                dataGridView1.Columns[1].Frozen = true;
                dataGridView1.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                for (int idx=0; idx <dataGridView1.Columns.Count; idx++)
                {
                    if (idx >= 2)
                    {
                        dataGridView1.Columns[idx].Width = 120;
                    }
                    // Remove sort in all columns
                    dataGridView1.Columns[idx].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                // Enable export button
                exportButton.Enabled = true;
            }
        }

        private void export_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV file (*.csv)|*.csv| All Files (*.*)|*.*";
            saveFileDialog1.Title = "Export to csv";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                exportToCsv(saveFileDialog1.FileName);
            }
        }

        private void exportToCsv(string filePath)
        {
            StringBuilder csv = new StringBuilder();

            foreach (worker worker in m_listWorkers)
            {
                StringBuilder sbWorker = new StringBuilder();

                for (int idx = 2; idx < m_dtReport.Columns.Count; idx++)
                {
                    bool isFound = false;
                    // Find corresponding date worked hour by date
                    foreach (attendance att in worker.m_attendanceList)
                    {
                        if (m_dtReport.Columns[idx].ToString().Contains(att.timeIn.ToString("yyyy-MM-dd")))
                        {
                            sbWorker.Append($"{att.workedHours},");
                            isFound = true;
                        }
                    }
                    if (!isFound)
                    {
                        sbWorker.Append("0,");
                    }
                }
                sbWorker.Append(worker.m_name);
                csv.AppendLine(sbWorker.ToString());
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }
            if (pictureBox2.Image != null)
            {
                pictureBox2.Image.Dispose();
                pictureBox2.Image = null;
            }
            if (dataGridView1.CurrentCell == null)
            {
                return;
            }

            int curColIndex = dataGridView1.CurrentCell.ColumnIndex;
            int curRowIndex = dataGridView1.CurrentCell.RowIndex;

            if (curColIndex > 1 && curRowIndex < m_dtReport.Rows.Count)
            {
                if (dataGridView1.CurrentCell != null && dataGridView1.CurrentCell.Value != null)
                {
                    string workerName = m_dtReport.Rows[curRowIndex][0].ToString();
                    string targetDate = m_dtReport.Columns[curColIndex].ToString();

                    foreach (worker worker in m_listWorkers)
                    {
                        if (worker.m_name == workerName)
                        {
                            foreach (attendance att in worker.m_attendanceList)
                            {
                                if (targetDate == att.timeIn.Date.ToString("dddd") + Environment.NewLine + att.timeIn.Date.ToString("yyyy-MM-dd"))
                                {
                                    // Time-in Image
                                    Image imgTimeIn = Image.FromFile(att.timeInImage);
                                    imgTimeIn.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                    pictureBox1.Image = imgTimeIn;
                                    // Time-out Image
                                    if (att.timeOutImage != null)
                                    {
                                        Image imgTimeOut = Image.FromFile(att.timeOutImage);
                                        imgTimeOut.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                        pictureBox2.Image = imgTimeOut;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void backup_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV file (*.csv)|*.csv| All Files (*.*)|*.*";
            saveFileDialog1.Title = "Export to csv";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                exportBackup(saveFileDialog1.FileName);
            }
        }

        private void exportBackup(string filePath)
        {
            StringBuilder csv = new StringBuilder();
            StringBuilder strCol = new StringBuilder();

            foreach (DataColumn col in m_dtReport.Columns)
            {
                strCol.Append(col.ColumnName + ',');
            }
            csv.AppendLine(strCol.ToString().Remove(strCol.ToString().Length - 1, 1));

            foreach (DataRow worker in m_dtReport.Rows)
            {
                StringBuilder sbWorker = new StringBuilder();

                for (int idx = 0; idx < m_dtReport.Columns.Count; idx++)
                {
                    sbWorker.Append(worker[idx].ToString() + ',');
                }
                csv.AppendLine(sbWorker.ToString().Remove(sbWorker.ToString().Length - 1, 1));
            }

            File.WriteAllText(filePath, csv.ToString());
        }
    }
}
