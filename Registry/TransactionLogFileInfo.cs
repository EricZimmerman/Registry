namespace Registry;

public class TransactionLogFileInfo
{
    public TransactionLogFileInfo(string fileName, byte[] fileBytes)
    {
        FileName = fileName;
        FileBytes = fileBytes;
    }

    public string FileName { get; }
    public byte[] FileBytes { get; }
}