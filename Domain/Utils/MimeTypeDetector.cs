using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nyerguds.Util
{
    public static class MimeTypeDetector
    {
        static readonly Dictionary<string, byte[]> KNOWN_TYPES = new()
            {
                {"bmp", new byte[] { 66, 77 }},
                {"doc", new byte[] { 208, 207, 17, 224, 161, 177, 26, 225 }},
                {"exe", new byte[] { 77, 90 }},
                {"gif", new byte[] { 71, 73, 70, 56 }},
                {"ico", new byte[] { 0, 0, 1, 0 }},
                {"jpg", new byte[] { 255, 216, 255 }},
                {"mp3", new byte[] { 255, 251, 48 }},
                {"pdf", new byte[] { 37, 80, 68, 70, 45, 49, 46 }},
                {"png", new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 }},
                {"rar", new byte[] { 82, 97, 114, 33, 26, 7, 0 }},
                {"swf", new byte[] { 70, 87, 83 }},
                {"tiff", new byte[] { 73, 73, 42, 0 }},
                {"torrent", new byte[] { 100, 56, 58, 97, 110, 110, 111, 117, 110, 99, 101 }},
                {"ttf", new byte[] { 0, 1, 0, 0, 0 }},
                {"zip", new byte[] { 80, 75, 3, 4 }},
                {"pcx", new byte[] { 10 }},
            };

        static readonly Dictionary<string, string> MIME_TYPES = new()
            {
                {"bmp", "image/bmp"},
                {"doc", "application/msword"},
                {"exe", "application/x-msdownload"},
                {"gif", "image/gif"},
                {"ico", "image/x-icon"},
                {"jpg", "image/jpeg"},
                {"jpeg", "image/jpeg"},
                {"mp3", "audio/mpeg"},
                {"pcx", "image/vnd.zbrush.pcx"},
                {"pdf", "application/pdf"},
                {"png", "image/png"},
                {"rar", "application/x-rar-compressed"},
                {"swf", "application/x-shockwave-flash"},
                {"tiff", "image/tiff"},
                {"torrent", "application/x-bittorrent"},
                {"ttf", "application/x-font-ttf"},
                {"zip", "application/x-zip-compressed"},
            };

        static readonly int BYTESTOREAD = KNOWN_TYPES.Values.Max(x => x.Length);

        public static string[] GetMimeTypeFromExtension(string extension)
        {
            if (extension != null && MIME_TYPES.TryGetValue(extension, out var mimetype))
                return new[] { extension, mimetype };
            return new[] { "dat", "application/octet-stream" };
        }

        public static string[] GetMimeType(string inputPath)
        {
            var file = new byte[BYTESTOREAD];
            using (var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            {
                fs.Position = 0;
                var actualRead = 0;
                do actualRead += fs.Read(file, actualRead, BYTESTOREAD - actualRead);
                while (actualRead != BYTESTOREAD && fs.Position < fs.Length);
            }
            return GetMimeType(file);
        }

        public static string[] GetMimeType(byte[] input)
        {
            string type = null;
            foreach (var pair in KNOWN_TYPES)
            {
                var value = pair.Value;
                if (!input.Take(value.Length).SequenceEqual(value))
                    continue;
                type = pair.Key;
                break;
            }
            return GetMimeTypeFromExtension(type);
        }

        public static string[] GetMimeType(Stream input)
        {
            string type = null;
            var origPos = input.Position;
            foreach (var pair in KNOWN_TYPES)
            {
                input.Position = origPos;
                var value = pair.Value;
                var checkLen = value.Length;
                var checkArr = new byte[value.Length];
                if (input.Read(checkArr, 0, checkLen) != checkLen || !checkArr.SequenceEqual(value))
                    continue;
                type = pair.Key;
                break;
            }
            input.Position = origPos;
            return GetMimeTypeFromExtension(type);
        }

    }
}
