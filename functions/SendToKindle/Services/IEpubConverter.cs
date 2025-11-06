namespace SendToKindle.Services;

public interface IEpubConverter
{
    Task<byte[]> ConvertHtmlToEpub(string html, string title, string author, string sourceUrl);
}
