using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportGenerator
{
    class InvalidImageFileException: Exception
    {
        public InvalidImageFileException(string path)
            : base(String.Format("Invalid Image File: {0}", path))
        {

        }
    }

    class attendance
    {
        public DateTime timeIn;
        public DateTime timeOut;
        public string timeInImage;
        public string timeOutImage;
        public int workedHours;

        public void setWorkedHours(int hours)
        {
            workedHours = hours;
        }
    }

    class worker
    {
        public string m_name;
        public List<attendance> m_attendanceList = new List<attendance>();

        private static Regex r = new Regex(":");

        public worker(string workerDir)
        {
            // Get name from folder path
            string[] tempPath = workerDir.Split('\\');
            string[] workerName = tempPath[tempPath.Count() - 1].Split('_');
            m_name = workerName[workerName.Count() - 1];
        }

        public void analyzeImageReport(string imagePath)
        {
            DateTime currentDateTime = GetDateTakenFromImage(imagePath);
            bool isExist = false;

            for(int idx = 0; idx < m_attendanceList.Count(); idx ++)
            {
                if (m_attendanceList[idx].timeIn.Date == currentDateTime.Date)
                {
                    if (currentDateTime > m_attendanceList[idx].timeIn)
                    {
                        m_attendanceList[idx].timeOut = currentDateTime;
                        m_attendanceList[idx].timeOutImage = imagePath;
                    }
                    else
                    {
                        DateTime tmpDateTime = m_attendanceList[idx].timeIn;
                        string tmpImagePath = m_attendanceList[idx].timeInImage;

                        m_attendanceList[idx].timeIn = currentDateTime;
                        m_attendanceList[idx].timeInImage = imagePath;

                        m_attendanceList[idx].timeOut = tmpDateTime;
                        m_attendanceList[idx].timeOutImage = tmpImagePath;
                    }
                    isExist = true;
                }
            }

            if (!isExist)
            {
                attendance tmpAtt = new attendance();
                tmpAtt.timeIn = currentDateTime;
                tmpAtt.timeInImage = imagePath;

                m_attendanceList.Add(tmpAtt);
            }
        }

        private DateTime GetDateTakenFromImage(string path)
        {
            // Get DateTime from dateTaken metadata.
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (Image myImage = Image.FromStream(fs, false, false))
                {
                    PropertyItem propItem = myImage.GetPropertyItem(36867);
                    string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                    return DateTime.Parse(dateTaken);
                }
            } catch
            {
                // Do nothing.
            }

            // Get DateTime from filename.
            try
            {
                string[] dateString = path.Split('\\');
                string dateTakenRaw = Path.GetFileNameWithoutExtension(dateString[dateString.Length - 1]);
                // Expected filename format 20210323_160314.jpg
                string[] dayAndTime = dateTakenRaw.Split('_');
                
                string year = dayAndTime[0].Substring(0, 4);
                string month = dayAndTime[0].Substring(4, 2);
                string day = dayAndTime[0].Substring(6, 2);

                string hour = dayAndTime[1].Substring(0, 2);
                string minute = dayAndTime[1].Substring(2, 2);
                string second = dayAndTime[1].Substring(4, 2);

                // Expected string format for date parsing '08/18/2018 07:22:16'-- > 8 / 18 / 2018 7:22:16 AM
                string dateTaken = String.Format("{0}/{1}/{2} {3}:{4}:{5}", month, day, year, hour, minute, second);

                return DateTime.ParseExact(dateTaken, "MM/dd/yyyy HH:mm:ss", null);
            } catch
            {
                throw new InvalidImageFileException(path);
            }
        }
    }
}
