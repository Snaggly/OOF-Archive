namespace OOF_Packer
{
    public class FileData
    {
        public FileData(string fileName, long length) : this(fileName, length, 0, 0) { }
        public FileData(string fileName, long length, long position) : this(fileName, length, position, 0) { }
        public FileData(string fileName, long length, long position, int index)
        {
            FileName = fileName;
            Length = length;
            Position = position;
            Index = index;
        }

        public int Index { get; }
        public string FileName { get; }
        public long Length { get; }
        public long Position { get; }

        public string FileSize {
            get
            {
                if (Length > 1099511627776)
                    return ((double)Length / 1099511627776).ToString("0.00") + " TB";
                else if (Length > 1073741824)
                    return ((double)Length / 1073741824).ToString("0.00") + " GB";
                else if (Length > 1048576)
                    return ((double)Length / 1048576).ToString("0.00") + " MB";
                else if (Length > 1024)
                    return ((double)Length / 1024).ToString("0.00") + " KB";
                else
                    return Length + " B";
            }
        }
    }
}
