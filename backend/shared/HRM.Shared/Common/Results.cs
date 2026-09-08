namespace HRM.Shared.Common;

/// <summary>Standard error envelope returned by every service.</summary>
public class ApiErrorResponse
{
    public string Title { get; set; } = "An error occurred";
    public int Status { get; set; } = 500;
    public string? Detail { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>Domain-level exceptions the middleware maps to proper HTTP codes.</summary>
public class NotFoundException(string message) : Exception(message);
public class ConflictException(string message) : Exception(message);
public class DomainRuleException(string message) : Exception(message);
