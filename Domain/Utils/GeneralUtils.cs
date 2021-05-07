using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nyerguds.Util
{
    public class GeneralUtils
    {
        public static Boolean IsNumeric(String str)
        {
            foreach (Char c in str)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }
        
        /// <summary>
        /// Checks if the given value starts with T, J, Y, O (TRUE, JA, YES, OUI) or is 1
        /// If the value is null or the parse fails, the default is False.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>True if the string's first letter matches J, Y, O, 1 or T</returns>
        public static Boolean IsTrueValue(String value)
        {
            return IsTrueValue(value, false);
        }
        /// <summary>
        /// Checks if the given value starts with T, J, Y, O (TRUE, JA, YES, OUI) or is 1
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="defaultVal">Default value to return in case parse fails</param>
        /// <returns>True if the string's first letter matches J, Y, O, 1 or T</returns>
        public static Boolean IsTrueValue(String value, Boolean defaultVal)
        {
            if (String.IsNullOrEmpty(value))
                return defaultVal;
            return Regex.IsMatch(value, "^(([TJYO].*)|(0*1))$", RegexOptions.IgnoreCase);
        }

        public static Boolean IsHexadecimal(String str)
        {
            return Regex.IsMatch(str, "^[0-9A-F]*$", RegexOptions.IgnoreCase);
        }

        public static String GetApplicationPath()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        public static TEnum TryParseEnum<TEnum>(String value, TEnum defaultValue, Boolean ignoreCase) where TEnum : struct
        {
            if (String.IsNullOrEmpty(value))
                return defaultValue;
            try { return (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase); }
            catch (ArgumentException) { return defaultValue; }
        }

        public static String GetAbsolutePath(String relativePath, String basePath)
        {
            if (relativePath == null)
                return null;
            if (basePath == null)
                basePath = Path.GetFullPath("."); // quick way of getting current working directory
            else
                basePath = GetAbsolutePath(basePath, null); // to be REALLY sure ;)
            String path;
            // specific for windows paths starting on \ - they need the drive added to them.
            // I constructed this piece like this for possible Mono support.
            if (!Path.IsPathRooted(relativePath) || "\\".Equals(Path.GetPathRoot(relativePath)))
            {
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    path = Path.Combine(Path.GetPathRoot(basePath), relativePath.TrimStart(Path.DirectorySeparatorChar));
                else
                    path = Path.Combine(basePath, relativePath);
            }
            else
                path = relativePath;
            // resolves any internal "..\" to get the true full path.
            Int32 filenameStart = path.LastIndexOf(Path.DirectorySeparatorChar);
            String dirPart = path.Substring(0, filenameStart + 1);
            String filePart = path.Substring(filenameStart + 1);
            if (filePart.Contains("*") || filePart.Contains("?"))
            {
                dirPart = Path.GetFullPath(dirPart);
                return Path.Combine(dirPart, filePart);
            }
            return Path.GetFullPath(path);
        }

        public static DateTime GetDosDateTime(UInt16 dosTime, UInt16 dosDate)
        {
            Int32 sec = (dosTime & 0x1F) * 2;
            Int32 min = ((dosTime >> 5) & 0x3F);
            Int32 hour = ((dosTime >> 11) & 0x1F);
            if (sec > 59 || min > 59 || hour > 23)
                throw new ArgumentException("Bad time stamp.");
            Int32 day = (dosDate & 0x1F);
            Int32 month = ((dosDate >> 5) & 0x0F);
            Int32 year = 1980 + ((dosDate >> 9) & 0x7F);
            if (day == 0 || month == 0 || month > 12)
                throw new ArgumentException("Bad date stamp.");
            return new DateTime(year, month, day, hour, min, sec);
        }

        public static UInt16 GetDosDateInt(DateTime datestamp)
        {
            Int32 year = Math.Max(Math.Min(0, datestamp.Year - 1980), 127);
            return (UInt16)((datestamp.Day) | (datestamp.Month << 5) | (year << 9));
        }

        public static UInt16 GetDosTimeInt(DateTime datestamp)
        {
            return (UInt16)((datestamp.Second >> 1) | (datestamp.Minute << 5) | (datestamp.Hour << 11));
        }

        /// <summary>
        /// Tool to get date string for ExtraInfo from a dateTime.
        /// </summary>
        /// <param name="datestamp">date stamp</param>
        /// <returns>String for ExtraInfo</returns>
        public static String GetDateString(DateTime datestamp)
        {
            return "Date: " + datestamp.Year.ToString("D4") + "-" + datestamp.Month.ToString("D2") + "-" + datestamp.Day.ToString("D2") + "\n"
                    + "Time: " + datestamp.Hour.ToString("D2") + ":" + datestamp.Minute.ToString("D2") + ":" + datestamp.Second.ToString("D2");
        }

        public static String GetDos83FileName(String file)
        {
            String filename = Path.GetFileNameWithoutExtension(file) ?? String.Empty;
            filename = new String(filename.Replace(' ', '_').Where(x => x > 0x20 && x < 0x7F).ToArray());
            String extension = Path.GetExtension(file) ?? String.Empty;
            extension = new String(extension.Replace(' ', '_').Where(x => x > 0x20 && x < 0x7F).ToArray());
            if (filename.Length > 8)
                filename = filename.Substring(0, 8);
            if (extension.Length > 4)
                extension = extension.Substring(0, 4);
            return (filename + extension).ToUpperInvariant();
        }

        public static String ProgramVersion()
        {
            FileVersionInfo ver = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            //Version v = AssemblyName.GetAssemblyName(Assembly.GetExecutingAssembly().Location).Version;
            String version = String.Format("v{0}.{1}", ver.FileMajorPart, ver.FileMinorPart);
            if (ver.FileBuildPart > 0)
                version += "." + ver.FileBuildPart;
            if (ver.FilePrivatePart > 0)
                version += "." + ver.FilePrivatePart;
            return version;
        }

        public static String DoubleFirstAmpersand(String input)
        {
            if (input == null)
                return null;
            Int32 index = input.IndexOf('&');
            if (index == -1)
                return input;
            return input.Substring(0, index) + '&' + input.Substring(index);
        }
    }
}
