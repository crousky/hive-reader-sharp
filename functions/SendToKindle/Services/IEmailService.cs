namespace SendToKindle.Services;

public interface IEmailService
{
    Task SendToKindle(byte[] epubData, string fileName, string recipientEmail);
}
