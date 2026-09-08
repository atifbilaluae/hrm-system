export const LeaveRequestStatus = { Pending: 1, Approved: 2, Rejected: 3, Cancelled: 4 } as const;

export interface LeaveType {
  id: string;
  name: string;
  code: string;
  annualQuotaDays: number;
  isPaid: boolean;
  requiresApproval: boolean;
  maxConsecutiveDays: number;
  minimumNoticeDays: number;
  isActive: boolean;
}

export interface LeaveBalance {
  leaveTypeId: string;
  leaveTypeName: string;
  leaveTypeCode: string;
  year: number;
  entitledDays: number;
  usedDays: number;
  pendingDays: number;
  availableDays: number;
}

export interface LeaveRequest {
  id: string;
  employeeId: string;
  leaveTypeId: string;
  leaveTypeName: string;
  fromDate: string;
  toDate: string;
  totalDays: number;
  reason: string;
  status: number;
  approverEmployeeId: string | null;
  decisionComments: string | null;
  decidedAtUtc: string | null;
  createdAtUtc: string;
}
