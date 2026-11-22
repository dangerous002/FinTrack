namespace FinTrack.Domain.Exceptions;

/// <summary>Нарушение бизнес-правила. Сообщение показывается пользователю.</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}