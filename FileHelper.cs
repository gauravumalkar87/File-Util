using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using WebAPIForGrafana.Models;

namespace WebAPIForGrafana.Utility
{
    public class FileHelper
    {
        public static void ReadFile(string filePath,Func<string, long, bool> text , Func<BufferedStream, long> SetStartPosition)
        {
            
            long lineCount = 0;
            if (File.Exists(filePath))
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (BufferedStream bs = new BufferedStream(fs))
                    {
                       var pos = SetStartPosition?.Invoke(bs);
                       if (pos != null)
                       {
                           bs.Position = pos ?? 0;   
                       }                      
                      
                        using (StreamReader sr = new StreamReader(bs))
                        {
                            string line;
                            long length = bs.Position;
                            while ((line = sr.ReadLine()) != null)
                            {

                                length += (line.Length + 2);                                
                                if (!(text?.Invoke(line, length) ?? false))
                                {                                    
                                    break;
                                }                              
                            }                           
                        }
                    }
                }
            }           
        }        
    }
}
