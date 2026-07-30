namespace TurkcellMeetingAssistant.Application.Common.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}
