using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using log4net;

namespace WebAPIForGrafana.Utility
{
    public class Utils
    {
        private static ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static long GetStartingPos(BufferedStream bs, DateTime startTime, string pattern, string datePart, string dateFormat, bool isFirstFile)
        {
            if (!isFirstFile)
                return 0;
            long bufferIncrementedBy = Math.Max(1, bs.Length / 100);
            long decrementBy = Math.Max(1, bufferIncrementedBy / 10);
            long startPos = bufferIncrementedBy;
            bs.Position = bs.Length < startPos ? 0 : startPos;
          
            if (bs.Length < startPos && bs.Length > 2048)
            {
                startPos = bs.Length - 2048;
            }

            bool isTimePassed = false;
            int i = 0;
            int j = 0;
            bool isDateParsed = false;
            while (j < 1000)//if not able to parse date than retry 1000 times only
            {
                string s = ReadLine(bs);
                //byte[] bufferBytes = new byte[10024];
                //bs.Read(bufferBytes, 0, 10024);
                //var s1 = Encoding.UTF8.GetString(bufferBytes, 0, 10024);
                var date = GetLogDate(s, pattern, datePart, dateFormat);
                isDateParsed = date != DateTime.MaxValue || isDateParsed;
                j++;
                if (date != DateTime.MaxValue && (!isTimePassed && date < startTime || ((date.Hour == 23 && date.Minute == 59) && bs.Position < bs.Length / 10)))
                {
                    j = 0;
                    startPos += bufferIncrementedBy;
                    if (bs.Length < startPos)
                    {
                        startPos = bs.Position;
                        break;
                    }

                    bs.Position = startPos;
                }
                else if (isTimePassed && date < startTime || startPos < decrementBy || i > 41)
                {
                    if (startPos < decrementBy)
                        startPos = 0;
                    break;
                }
                else if (date != DateTime.MaxValue || s== "")
                {
                    i++;
                    isTimePassed = true;
                    startPos -= startPos >= decrementBy ? decrementBy : 0;
                    bs.Position = startPos;
                }
            }

            if (!isDateParsed)
            {
                logger.Error("Not able to parse date");
                return bs.Length;
            }

            logger.Info($"position:{(startPos < 0 ? 0 : startPos)}, stream Length: {bs.Length}");
            return startPos < 10000 ? 0 : startPos;
        }

        public static String ReadLine(BufferedStream bs)
        {
            StringBuilder sb = new StringBuilder();
            int buffer = 5120;
            byte[] bufferBytes = new byte[buffer];
            int readByte = 0;
            do
            {
                readByte = bs.Read(bufferBytes, 0, buffer);
                if (bufferBytes.Any(x => x == 10))
                {
                    sb.Append(Encoding.UTF8.GetString(bufferBytes, 0, buffer));
                    bs.Read(bufferBytes, 0, buffer);
                    sb.Append(Encoding.UTF8.GetString(bufferBytes, 0, buffer));
                    readByte = 0;
                }

            } while(readByte > 0);

            return sb.ToString();
        }
        public static DateTime GetLogDate(string text, string pattern, string datePart, string dateFormat)
        {
            var match = Regex.Match(text, pattern, RegexOptions.CultureInvariant);
            DateTime dt;
            datePart = datePart == null ? "" : datePart + " ";
            if (match.Success && DateTime.TryParseExact($"{datePart}{match.Value.TrimEnd('t').Trim()}", dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                return dt;
            }

            if (!match.Success)
            {
                match = Regex.Match(text, @"\d{4,4}-\d{1,2}-\d{1,2}\s\d{1,2}:\d{1,2}:\d{1,2}.\d{3,3}", RegexOptions.CultureInvariant);
                if (match.Success && DateTime.TryParseExact($"{match.Value.TrimEnd('t').Trim()}", "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    return dt;
                }
            }


            return DateTime.MaxValue;
        }       
    }
}
