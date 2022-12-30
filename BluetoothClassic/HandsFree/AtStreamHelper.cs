using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;

namespace HandsFree
{
    public static class AtStreamHelper
    {
        /// <summary>
        /// Writes a correctly formatted response to the Handsfree device.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="command"></param>
        public static void WriteString(this Stream stream, string command)
        {
            Debug.WriteLine($"-> {command}");
            byte[] converted = Encoding.ASCII.GetBytes($"\r\n{command}\r\n");
            stream.Write(converted, 0, converted.Length);
            stream.Flush();
        }

        /// <summary>
        /// Extracts the dialled number from an ATD command
        /// </summary>
        /// <param name="atdString"></param>
        /// <returns></returns>
        public static string ParseAtdString(string atdString)
        {
            return atdString.Replace("ATD", "").Replace(";", "");
        }

        public static void ParseAppleBatteryStatus(string iphoneAccev)
        {
            var parts = iphoneAccev.Split('=')[1].Split(',');
            for (int i = 0; i < int.Parse(parts[0]); i++)
            {
                var key = int.Parse(parts[i * 2 + 1]);
                var value = int.Parse(parts[i * 2 + 2]);
                switch (key)
                {
                    case 1:
                        Debug.WriteLine($"Battery level is {AppleBatteryLevelToPercentage(value)}%");
                        break;
                }
            }
        }

        /// <summary>
        /// Convert battery level returned from +IPHONEACCEV to a percentage
        /// </summary>
        /// <param name="rawValue"></param>
        /// <returns></returns>
        public static int AppleBatteryLevelToPercentage(int rawValue)
        {
            // rawValue is integer from 0 to 9. Round these up to the next full 10%
            return (rawValue + 1) * 10;
        }
    }
}
