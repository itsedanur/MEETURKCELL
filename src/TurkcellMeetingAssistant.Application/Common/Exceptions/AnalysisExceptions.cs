using FluentValidation.Results;

namespace TurkcellMeetingAssistant.Application.Common.Exceptions;

public class TranscriptNotFoundException : Exception
{
    public TranscriptNotFoundException(string message) : base(message) { }
}

public class TranscriptTooShortException : Exception
{
    public TranscriptTooShortException(string message) : base(message) { }
}

public class AnalysisAlreadyInProgressException : Exception
{
    public AnalysisAlreadyInProgressException(string message) : base(message) { }
}

public class AiResultValidationException : Exception
{
    public IEnumerable<ValidationFailure> Errors { get; }

    public AiResultValidationException(string message, IEnumerable<ValidationFailure> errors) : base(message)
    {
        Errors = errors;
    }
}
