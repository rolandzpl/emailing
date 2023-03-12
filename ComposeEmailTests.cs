using System.Text;
using MimeKit;
using MimeKit.Utils;
using static lab_myemail.MessageComposer;

namespace lab_myemail;

public class ComposeEmailTests
{
    [TestCaseSource(nameof(TestMails))]
    public void Test1(TestMail mail)
    {
        var message = Compose(mail.From, mail.To, null, null, mail.Attachments, mail.Subject, mail.Content);
        message.WriteTo("test-mail.eml");
        Assert.Pass();
    }

    private static IEnumerable<TestMail> TestMails()
    {
        yield return new TestMail
        {
            From = "rolandz@poczta.fm",
            To = new[]
            {
            "test1@test.com"
        },
            Subject = "Mail Subject",
            Attachments = new[]
            {
            new AttachmentDto{ FileName = "1.jpg", Content = File.ReadAllBytes("1.jpg"), IsInline = true },
            new AttachmentDto { FileName = "1.xlsx", Content = File.ReadAllBytes("1.xlsx"), IsInline = false },
            new AttachmentDto { FileName = "data.txt", Content = Encoding.Default.GetBytes("This is a test file"), IsInline = false }
        },
            Content = "This is a test <img src=\"1.jpg\">."
        };
    }

    public class TestMail
    {
        internal string From;
        internal string[] To;
        internal string Subject;
        internal AttachmentDto[] Attachments;
        internal string Content;
    }
}

public class MessageComposer
{
    public static MimeMessage Compose(string from, string[] to, string[] cc, string[] bcc, AttachmentDto[] attachments, string subject, string content)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.AddRange(to.Select(_ => MailboxAddress.Parse(_)));
        if (cc != null)
        {
            message.Cc.AddRange(cc.Select(_ => MailboxAddress.Parse(_)));
        }
        if (bcc != null)
        {
            message.Bcc.AddRange(bcc.Select(_ => MailboxAddress.Parse(_)));
        }
        message.Body = GetMessageBody(attachments, content);
        return message;
    }

    private static MimeEntity GetMessageBody(AttachmentDto[] attachments, string content)
    {
        var builder = new BodyBuilder();
        foreach (var a in attachments.Where(_ => _.IsInline))
        {
            var contentId = MimeUtils.GenerateMessageId();
            if (TryReplaceFileNameWithContentId(ref content, a.FileName, contentId))
            {
                var res = builder.LinkedResources.Add(a.FileName, a.Content);
                res.ContentId = contentId;
            }
        }
        foreach (var a in attachments.Where(_ => !_.IsInline))
        {
            builder.Attachments.Add(a.FileName, a.Content);
        }
        builder.HtmlBody = content;
        builder.TextBody = "This message contains HTML content";
        return builder.ToMessageBody();
    }

    private static bool TryReplaceFileNameWithContentId(ref string content, string fileName, string contentId)
    {
        content = content.Replace($"src=\"{fileName}\"", $"src=\"cid:{contentId}\"");
        return true;
    }

    public record AttachmentDto
    {
        internal string FileName;
        internal string ContentType;
        internal byte[] Content;
        internal bool IsInline;
    }
}
