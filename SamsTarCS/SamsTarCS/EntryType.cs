namespace SamsTarCS
{
    public enum EntryType : byte
    {
        File = 0,
        FileObsolete = 0x30,
        HardLink = 0x31,
        SymLink = 0x32,
        CharDevice = 0x33,
        BlockDevice = 0x34,
        Directory = 0x35,
        Fifo = 0x36,
    }
}