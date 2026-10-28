// NotificationEngine.Core/Exceptions/CommonExceptions.cs
using System;

namespace NotificationEngine.Core.Exceptions;

public class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string message, Exception inner) : base(message, inner) { }
}

public class ConflictException : BusinessException
{
    public ConflictException(string message) : base(message) { }
    public ConflictException(string message, Exception inner) : base(message, inner) { }
}

public class ValidationException : BusinessException
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception inner) : base(message, inner) { }
}

public class UnauthorizedException : BusinessException
{
    public UnauthorizedException(string message = "Yetkisiz işlem") : base(message) { }
}

public class InvalidInputException : BusinessException
{
        public InvalidInputException(string message) : base(message) { }
        public InvalidInputException(string message, Exception inner) : base(message, inner)  {}
}

public class ForbiddenException : BusinessException
{
    public ForbiddenException(string message) : base(message) { }
}
