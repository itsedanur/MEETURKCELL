using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Services;

public class MeetingEmailTemplateService : IMeetingEmailTemplateService
{
    private readonly EmailSettings _settings;

    public MeetingEmailTemplateService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public Task<EmailMessage> GeneratePreviewEmailAsync(
        Meeting meeting, 
        MeetingSummary summary, 
        List<ActionItem> actionItems, 
        List<MeetingTopic> topics, 
        List<MeetingDecision> decisions, 
        List<OpenIssue> openIssues, 
        List<MeetingParticipant> participants, 
        string? subjectOverride = null, 
        string? introText = null, 
        string? closingText = null, 
        bool includeParticipants = true, 
        bool includeTopics = true, 
        bool includeDecisions = true, 
        bool includeActionItems = true, 
        bool includeOpenIssues = true, 
        bool includeActionStatus = true, 
        bool includeEvidence = false, 
        CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage();
        
        // Subject
        var title = meeting.Title;
        var dateStr = meeting.MeetingDate.ToString("dd.MM.yyyy");
        message.Subject = subjectOverride ?? $"[Toplantı Özeti] {title} - {dateStr}";

        // Body Generation
        var html = new StringBuilder();
        var text = new StringBuilder();

        // 1. Intro
        if (!string.IsNullOrWhiteSpace(introText))
        {
            var encodedIntro = HtmlEncoder.Default.Encode(introText);
            html.AppendLine($"<p>{encodedIntro.Replace("\n", "<br/>")}</p>");
            text.AppendLine(introText);
            text.AppendLine();
        }

        // 2. Meeting Details
        html.AppendLine("<h2>Toplantı Detayları</h2>");
        html.AppendLine("<ul>");
        html.AppendLine($"<li><strong>Başlık:</strong> {HtmlEncoder.Default.Encode(meeting.Title)}</li>");
        html.AppendLine($"<li><strong>Tarih:</strong> {dateStr}</li>");
        if (meeting.OrganizerUser != null)
        {
            var orgName = $"{meeting.OrganizerUser.FirstName} {meeting.OrganizerUser.LastName}";
            html.AppendLine($"<li><strong>Organizatör:</strong> {HtmlEncoder.Default.Encode(orgName)}</li>");
        }
        html.AppendLine("</ul>");

        text.AppendLine("TOPLANTI DETAYLARI");
        text.AppendLine($"Başlık: {meeting.Title}");
        text.AppendLine($"Tarih: {dateStr}");
        if (meeting.OrganizerUser != null) 
        {
            text.AppendLine($"Organizatör: {meeting.OrganizerUser.FirstName} {meeting.OrganizerUser.LastName}");
        }
        text.AppendLine();

        // 3. Participants
        if (includeParticipants && participants.Any())
        {
            html.AppendLine("<h2>Katılımcılar</h2>");
            html.AppendLine("<ul>");
            text.AppendLine("KATILIMCILAR");
            foreach (var p in participants)
            {
                var pText = $"{p.FullName} ({p.Email})";
                html.AppendLine($"<li>{HtmlEncoder.Default.Encode(pText)}</li>");
                text.AppendLine($"- {pText}");
            }
            html.AppendLine("</ul>");
            text.AppendLine();
        }

        // 4. Executive Summary
        if (!string.IsNullOrWhiteSpace(summary.ExecutiveSummary))
        {
            html.AppendLine("<h2>Yönetici Özeti</h2>");
            html.AppendLine($"<p>{HtmlEncoder.Default.Encode(summary.ExecutiveSummary).Replace("\n", "<br/>")}</p>");
            text.AppendLine("YÖNETİCİ ÖZETİ");
            text.AppendLine(summary.ExecutiveSummary);
            text.AppendLine();
        }

        // 5. Topics
        if (includeTopics && topics.Any())
        {
            html.AppendLine("<h2>Görüşülen Konular</h2>");
            html.AppendLine("<ul>");
            text.AppendLine("GÖRÜŞÜLEN KONULAR");
            foreach (var t in topics.OrderBy(x => x.SortOrder))
            {
                html.AppendLine($"<li>{HtmlEncoder.Default.Encode(t.Title)}</li>");
                text.AppendLine($"- {t.Title}");
            }
            html.AppendLine("</ul>");
            text.AppendLine();
        }

        // 6. Decisions
        if (includeDecisions && decisions.Any())
        {
            html.AppendLine("<h2>Alınan Kararlar</h2>");
            html.AppendLine("<ul>");
            text.AppendLine("ALINAN KARARLAR");
            foreach (var d in decisions.OrderBy(x => x.SortOrder))
            {
                var content = d.Description;
                if (includeEvidence && !string.IsNullOrWhiteSpace(d.Evidence))
                {
                    content += $" (Dayanak: {d.Evidence})";
                }
                html.AppendLine($"<li>{HtmlEncoder.Default.Encode(content)}</li>");
                text.AppendLine($"- {content}");
            }
            html.AppendLine("</ul>");
            text.AppendLine();
        }

        // 7. Action Items
        if (includeActionItems && actionItems.Any())
        {
            html.AppendLine("<h2>Aksiyonlar</h2>");
            html.AppendLine("<table border='1' cellpadding='5' cellspacing='0'>");
            html.AppendLine("<thead><tr><th>Aksiyon</th><th>Sorumlu</th><th>Termin</th><th>Öncelik</th>");
            if (includeActionStatus) html.AppendLine("<th>Durum</th>");
            html.AppendLine("</tr></thead><tbody>");
            
            text.AppendLine("AKSİYONLAR");

            foreach (var a in actionItems)
            {
                var owner = !string.IsNullOrWhiteSpace(a.OwnerName) ? a.OwnerName : "Belirlenmedi";
                var due = a.DueDate.HasValue ? a.DueDate.Value.ToString("dd.MM.yyyy") : "Belirtilmedi";
                
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{HtmlEncoder.Default.Encode(a.Description)}</td>");
                html.AppendLine($"<td>{HtmlEncoder.Default.Encode(owner)}</td>");
                html.AppendLine($"<td>{due}</td>");
                html.AppendLine($"<td>{a.Priority}</td>");
                if (includeActionStatus) html.AppendLine($"<td>{a.Status}</td>");
                html.AppendLine("</tr>");

                var statusStr = includeActionStatus ? $" [{a.Status}]" : "";
                text.AppendLine($"- {a.Description} (Sorumlu: {owner}, Termin: {due}, Öncelik: {a.Priority}){statusStr}");
            }
            html.AppendLine("</tbody></table>");
            text.AppendLine();
        }

        // 8. Open Issues
        if (includeOpenIssues && openIssues.Any())
        {
            html.AppendLine("<h2>Açık Konular</h2>");
            html.AppendLine("<ul>");
            text.AppendLine("AÇIK KONULAR");
            foreach (var o in openIssues.OrderBy(x => x.SortOrder))
            {
                html.AppendLine($"<li>{HtmlEncoder.Default.Encode(o.Description)}</li>");
                text.AppendLine($"- {o.Description}");
            }
            html.AppendLine("</ul>");
            text.AppendLine();
        }

        // 9. Closing
        if (!string.IsNullOrWhiteSpace(closingText))
        {
            var encodedClosing = HtmlEncoder.Default.Encode(closingText);
            html.AppendLine($"<p>{encodedClosing.Replace("\n", "<br/>")}</p>");
            text.AppendLine(closingText);
            text.AppendLine();
        }

        // 10. Footer
        if (!string.IsNullOrWhiteSpace(_settings.EmailFooterText))
        {
            html.AppendLine("<hr/>");
            html.AppendLine($"<p style='font-size: 12px; color: #666;'>{HtmlEncoder.Default.Encode(_settings.EmailFooterText)}</p>");
            text.AppendLine("---");
            text.AppendLine(_settings.EmailFooterText);
        }

        message.HtmlBody = html.ToString();
        message.TextBody = text.ToString();
        
        return Task.FromResult(message);
    }
}
