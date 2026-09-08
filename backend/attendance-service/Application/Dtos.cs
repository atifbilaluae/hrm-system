using HRM.AttendanceService.Domain;

namespace HRM.AttendanceService.Application;

public record AttendanceRecordDto(Guid Id, Guid EmployeeId, DateTime WorkDate,
    DateTime? CheckInUtc, DateTime? CheckOutUtc, AttendanceStatus Status,
    int WorkedMinutes, int LateMinutes, int EarlyDepartureMinutes, int OvertimeMinutes,
    bool IsManualEntry, string? Notes);

public record TodayAttendanceDto(bool HasCheckedIn, bool HasCheckedOut, AttendanceRecordDto? Record);

public record ManualAttendanceRequest(Guid EmployeeId, DateTime WorkDate,
    DateTime? CheckInUtc, DateTime? CheckOutUtc, string? Notes);

public record CorrectionRequestDto(Guid Id, Guid EmployeeId, DateTime WorkDate,
    DateTime? RequestedCheckInUtc, DateTime? RequestedCheckOutUtc, string Reason,
    CorrectionStatus Status, string? ReviewComments, DateTime CreatedAtUtc);

public record CreateCorrectionRequest(DateTime WorkDate, DateTime? RequestedCheckInUtc,
    DateTime? RequestedCheckOutUtc, string Reason);

public record DecideCorrectionRequest(bool Approve, string? Comments);

public record AttendanceSummaryDto(int Year, int Month, Guid EmployeeId,
    int PresentDays, int LateDays, int HalfDays, int AbsentDays,
    int TotalWorkedMinutes, int TotalOvertimeMinutes);

public record DailyAttendanceSummaryDto(DateTime Date, int Present, int Late,
    int HalfDay, int Absent, int TotalRecords);
