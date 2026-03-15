namespace BakeryFlow.Application.Common.Exceptions;

public sealed class BusinessRuleException(string message) : AppException(message);
