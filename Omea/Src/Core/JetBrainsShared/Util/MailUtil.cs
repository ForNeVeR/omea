// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text;
using System.Web.Mail;
using System.Xml;
using SMTP;

namespace JetBrains.Util
{
  public sealed class MailUtil
  {
    public const string EMAIL_CONFIG_TAG = "EMailConfig";
    public const string FROM_ADDRESS_TAG = "From";
    public const string TO_ADDRESS_TAG = "To";
    public const string MAIL_SERVER_TAG = "MailServer";
    private const string SUCCESS_SUBJECT_TAG = "SuccessSubj";
    private const string FAILED_SUBJECT_TAG = "FailSubj";
    public const string RESULTS_URL_TAG = "ResultsUrl";

    private MailUtil()
    {
    }

    public static void SendEMail(XmlNode emailConfigNode, string body, MailFormat format, bool success)
    {
      SendEMail(emailConfigNode, body, format, success, new MailAttachment[0]);
    }

    public static void SendEMail(XmlNode emailConfigNode, string body, MailFormat format, bool success, MailAttachment[] attachments)
    {
      string subject = emailConfigNode.SelectSingleNode(!success ? FAILED_SUBJECT_TAG : SUCCESS_SUBJECT_TAG).InnerText;
      SendEMail(emailConfigNode, body, format, subject, !success, attachments);
    }

    public static void SendEMail(XmlNode emailConfigNode, string body, MailFormat format, string subject, bool highPriority)
    {
      SendEMail(emailConfigNode, body, format, subject, highPriority, new MailAttachment[0]);
    }

    public static void SendEMail(XmlNode emailConfigNode, string body, MailFormat format, string subject, bool highPriority, MailAttachment[] attachments)
    {
      try
      {
        string from = emailConfigNode.SelectSingleNode(FROM_ADDRESS_TAG).InnerText;
        string to = emailConfigNode.SelectSingleNode(TO_ADDRESS_TAG).InnerText;
        string smtpServer = emailConfigNode.SelectSingleNode(MAIL_SERVER_TAG).InnerText;
        SendEMail(from, to, smtpServer, body, format, subject, highPriority, attachments);
      }
      catch (Exception e)
      {
        Console.WriteLine(e.ToString());
      }
    }
    public static void SendEMail(string from, string to, string smtpServer, string body, MailFormat format, string subject, bool highPriority, MailAttachment[] attachments, bool throghSmtpDirect )
    {
        try
        {
            MailMessage message = new MailMessage();
            message.From = from;
            message.To = to;
            message.Subject = subject;
            if (highPriority)
            {
                message.Priority = MailPriority.High;
            }
            message.BodyEncoding = Encoding.ASCII;
            message.BodyFormat = format;
            message.Body = body;

            foreach (MailAttachment attachment in attachments)
                message.Attachments.Add(attachment);

            if ( throghSmtpDirect )
            {
                SmtpDirect.SmtpServer = smtpServer;
                SmtpDirect.Send(message);
            }
            else
            {
                SmtpMail.SmtpServer = smtpServer;
                SmtpMail.Send(message);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static void SendEMail(string from, string to, string smtpServer, string body, MailFormat format, string subject, bool highPriority, MailAttachment[] attachments)
    {
        SendEMail( from, to, smtpServer, body, format, subject, highPriority, attachments, true );
    }
  }
}
