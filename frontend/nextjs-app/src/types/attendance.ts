export const AttendanceStatus = {
  Present: 1, Late: 2, Absent: 3, HalfDay: 4, OnLeave: 5, Holiday: 6, WeekOff: 7,
} as const;

export const CorrectionStatus = { Pending: 1, Approved: 2, Rejected: 3 } as const;

export interface AttendanceRecord {
  id: string;
  employeeId: string;
  workDate: string;
  checkInUtc: string | null;
  checkOutUtc: string | null;
  status: number;
  workedMinutes: number;
  lateMinutes: number;
  earlyDepartureMinutes: number;
  overtimeMinutes: number;
  isManualEntry: boolean;
  notes: string | null;
}

export interface TodayAttendance {
  hasCheckedIn: boolean;
  hasCheckedOut: boolean;
  record: AttendanceRecord | null;
}

export interface AttendanceSummary {
  year: number;
  month: number;
  employeeId: string;
  presentDays: number;
  lateDays: number;
  halfDays: number;
  absentDays: number;
  totalWorkedMinutes: number;
  totalOvertimeMinutes: number;
}

export interface DailyAttendanceSummary {
  date: string;
  present: number;
  late: number;
  halfDay: number;
  absent: number;
  totalRecords: number;
}

export interface CorrectionRequest {
  id: string;
  employeeId: string;
  workDate: string;
  requestedCheckInUtc: string | null;
  requestedCheckOutUtc: string | null;
  reason: string;
  status: number;
  reviewComments: string | null;
  createdAtUtc: string;
}
