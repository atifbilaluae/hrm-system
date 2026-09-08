using HRM.LeaveService.Domain;

namespace HRM.LeaveService.Application;

public record LeaveTypeDto(Guid Id, string Name, string Code, int AnnualQuotaDays,
    bool IsPaid, bool RequiresApproval, int MaxConsecutiveDays, int MinimumNoticeDays, bool IsActive);

public record SaveLeaveTypeRequest(string Name, string Code, int AnnualQuotaDays,
    bool IsPaid, bool RequiresApproval, int MaxConsecutiveDays, int MinimumNoticeDays, bool IsActive = true);

public record LeaveBalanceDto(Guid LeaveTypeId, string LeaveTypeName, string LeaveTypeCode,
    int Year, decimal EntitledDays, decimal UsedDays, decimal PendingDays, decimal AvailableDays);

public record LeaveRequestDto(Guid Id, Guid EmployeeId, Guid LeaveTypeId, string LeaveTypeName,
    DateTime FromDate, DateTime ToDate, decimal TotalDays, string Reason,
    LeaveRequestStatus Status, Guid? ApproverEmployeeId, string? DecisionComments,
    DateTime? DecidedAtUtc, DateTime CreatedAtUtc);

public record CreateLeaveRequest(Guid LeaveTypeId, DateTime FromDate, DateTime ToDate, string Reason);

public record DecideLeaveRequest(bool Approve, string? Comments);

public record LeaveCalendarEntryDto(Guid RequestId, Guid EmployeeId, string LeaveTypeName,
    DateTime FromDate, DateTime ToDate, LeaveRequestStatus Status);
