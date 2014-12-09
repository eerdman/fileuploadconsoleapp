using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TestSalesforceMDF
{
    public class FileUploadSecurityHelper
    {
        private static readonly byte[] _Bmp = { 66, 77 };
        private static readonly byte[] _Mso = { 208, 207, 17, 224, 161, 177, 26, 225 }; //doc, xls, ppt
        private static readonly byte[] _Gif = { 71, 73, 70, 56 };
        private static readonly byte[] _Ico = { 0, 0, 1, 0 };
        private static readonly byte[] _Jpg = { 255, 216, 255 };
        private static readonly byte[] _Pdf = { 37, 80, 68, 70, 45, 49, 46 };
        private static readonly byte[] _Png = { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 };
        private static readonly byte[] _Tiff = { 73, 73, 42, 0 };
        private static readonly byte[] _Ttf = { 0, 1, 0, 0, 0 };
        private static readonly byte[] _Txt = { 116, 101, 115, 116 };
        private static readonly byte[] _Csv = { 34, 70, 105 };
        private static readonly byte[] _Msox = { 80, 75, 3, 4, 20, 0 }; //docx, xlsx, pptx, zip
        private static readonly byte[] _Rtf = { 123, 92, 114, 116, 102 };
        private static readonly byte[] _Xml = { 60, 63, 120, 109 };
        private static readonly byte[] _Rar = { 82, 97, 114, 33, 26 };
        private static readonly byte[] _Log = { 91, 73, 110, 115 };

        [DllImport("urlmon.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false)]
        static extern int FindMimeFromData(IntPtr pBC,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzUrl,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1, SizeParamIndex = 3)] byte[] pBuffer,
            int cbSize,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzMimeProposed,
            int dwMimeFlags, out IntPtr ppwzMimeOut, int dwReserved);

        public static string GetRealMimeFromFile(byte[] fileContent, string fileName, string fileContentType)
        {
            IntPtr mimeout;

            var contentLength = fileContent.Length;
            if (contentLength > 4096) contentLength = 4096;

            var buffer = new byte[contentLength];
            Array.Copy(fileContent, buffer, contentLength);

            var result = FindMimeFromData(IntPtr.Zero, fileName, buffer, contentLength, null, 0, out mimeout, 0);

            if (result != 0)
            {
                Marshal.FreeCoTaskMem(mimeout);
                return "";
            }

            var mime = Marshal.PtrToStringUni(mimeout);
            Marshal.FreeCoTaskMem(mimeout);

            fileContentType = fileContentType.ToLower();

            if (mime != null && mime.ToLower() != fileContentType && !isKnownMimeToFileContentTypeMapping(mime.ToLower(), fileContentType))
            {
                mime = getMimeType(buffer, fileName);
            }

            return mime.ToLower();
        }

        private static string getMimeType(byte[] file, string fileName)
        {
            var mime = string.Empty; // Default: Unknown

            // Ensure that the filename isn't empty or null
            //
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return mime;
            }

            // Get the file extension
            //
            var extension = Path.GetExtension(fileName) == null ? string.Empty : Path.GetExtension(fileName).ToUpper();

            // Get the MIME Type
            //
            if (file.Take(2).SequenceEqual(_Bmp))
            {
                mime = "image/bmp";
            }
            else if (file.Take(8).SequenceEqual(_Mso) && extension == ".XLS")
            {
                mime = "application/vnd.ms-excel";
            }
            else if (file.Take(8).SequenceEqual(_Mso) && extension == ".DOC")
            {
                mime = "application/msword";
            }
            else if (file.Take(8).SequenceEqual(_Mso) && extension == ".PPT")
            {
                mime = "application/vnd.ms-powerpoint";
            }
            else if (file.Take(4).SequenceEqual(_Gif))
            {
                mime = "image/gif";
            }
            else if (file.Take(4).SequenceEqual(_Ico))
            {
                mime = "image/x-icon";
            }
            else if (file.Take(3).SequenceEqual(_Jpg))
            {
                mime = "image/jpeg";
            }
            else if (file.Take(7).SequenceEqual(_Pdf))
            {
                mime = "application/pdf";
            }
            else if (file.Take(16).SequenceEqual(_Png))
            {
                mime = "image/png";
            }
            else if (file.Take(4).SequenceEqual(_Tiff))
            {
                mime = "image/tiff";
            }
            else if (file.Take(5).SequenceEqual(_Ttf))
            {
                mime = "application/x-font-ttf";
            }
            else if (file.Take(4).SequenceEqual(_Txt) || file.Take(4).SequenceEqual(_Log))
            {
                mime = "text/plain";
            }
            else if (file.Take(6).SequenceEqual(_Msox) && extension == ".DOCX")
            {
                mime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }
            else if (file.Take(6).SequenceEqual(_Msox) && extension == ".XLSX")
            {
                mime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }
            else if (file.Take(6).SequenceEqual(_Msox) && extension == ".PPTX")
            {
                mime = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
            }
            else if (file.Take(6).SequenceEqual(_Msox) && extension == ".ZIP")
            {
                mime = "application/zip";
            }
            else if (file.Take(5).SequenceEqual(_Rar))
            {
                mime = "application/x-rar-compressed";
            }
            else if (file.Take(3).SequenceEqual(_Csv))
            {
                mime = "text/csv";
            }
            else if (file.Take(5).SequenceEqual(_Rtf))
            {
                mime = "text/rtf";
            }
            else if (file.Take(4).SequenceEqual(_Xml))
            {
                mime = "application/xml";
            }
            return mime;
        }

        private static bool isKnownMimeToFileContentTypeMapping(string mime, string fileContentType)
        {
            // Add as many known mappings as is necessary. This is because of discrepancies between what the client
            // tells us is the mime type, and what urlmon.dll tells us is the mime type. Also, inspecting the first
            // number of bytes does not always translate to the exact type. For instance, file.Take(6).SequenceEqual(_Msox) && extension == ".ZIP"
            // does not match all zip files.
            //
            if (fileContentType == "text/csv" && (mime == "text/plain" || mime == "application/vnd.ms-excel")) // .csv
            {
                return true;
            }
            if (fileContentType == "application/zip" && mime == "application/x-zip-compressed") // .zip
            {
                return true;
            }
            if (fileContentType == "application/vnd.openxmlformats" &&
                (mime == "application/x-zip-compressed" || mime == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || mime == "application/octet-stream")) // .xlsx
            {
                return true;
            }
            if (fileContentType == "application/vnd.ms-excel" && mime == "application/octet-stream") // .xls
            {
                return true;
            }

            return false;
        }

        public static bool IsValidProofFile(ProofFile file)
        {
            var fileName = file.FileName.Replace("\"", "").ToLower();
            var fileMime = file.MimeType.ToLower();
            if (fileMime == "image/png")
            {
                return fileName.EndsWith(".png");
            }
            if (fileMime == "image/jpeg" || fileMime == "image/pjpeg")
            {
                return fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg");
            }
            if (fileMime == "application/pdf")
            {
                return fileName.EndsWith(".pdf");
            }
            if (fileMime == "application/msword")
            {
                return fileName.EndsWith(".doc") || fileName.EndsWith(".docx");
            }
            if (fileMime == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                return fileName.EndsWith(".docx");
            }
            return false;
        }
    }
}