using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IMeetingSummaryMutationService
{
    /// <summary>
    /// Verilen meetingId'ye ait aktif summary'yi bulur ve kullanıcının erişim, analiz durumu vs. yetkilerini doğrular.
    /// ExpectedVersion ve ExpectedManualRevisionNumber kontrolünü yapar, uymazsa ConcurrencyException fırlatır.
    /// </summary>
    Task<(Meeting Meeting, MeetingSummary Summary)> GetActiveSummaryForMutationAsync(
        Guid meetingId, 
        int? expectedVersion = null, 
        int? expectedManualRevisionNumber = null,
        bool requireOwnership = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Özet üzerinde değişiklik (Topic/Decision/Action/Summary) olduğunda çağrılır.
    /// ManualRevisionNumber'ı artırır, onay varsa iptal eder, LastEdited alanlarını günceller.
    /// Transaction içinde kullanılmalıdır.
    /// </summary>
    void MutateSummary(Meeting meeting, MeetingSummary summary, string actionName, string? changedFields = null);

    /// <summary>
    /// Değişiklik durumunda audit log kaydı oluşturur. DB'ye ekler ancak SaveChanges yapmaz.
    /// </summary>
    void LogAudit(string action, string entityName, string entityId, string newValues);
}
