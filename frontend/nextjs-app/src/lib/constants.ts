import { EmploymentStatus } from '@/types';

export const PERMISSIONS = {
  employeeView: 'employee.view',
  employeeCreate: 'employee.create',
  employeeUpdate: 'employee.update',
  employeeDelete: 'employee.delete',
  attendanceView: 'attendance.view',
  attendanceManage: 'attendance.manage',
  leaveRequest: 'leave.request',
  leaveApprove: 'leave.approve',
  payrollView: 'payroll.view',
  payrollProcess: 'payroll.process',
  recruitmentManage: 'recruitment.manage',
  performanceManage: 'performance.manage',
  userManage: 'user.manage',
  organizationManage: 'organization.manage',
} as const;

export const ACCESS_TOKEN_COOKIE = 'hrm_access_token';
export const REFRESH_TOKEN_COOKIE = 'hrm_refresh_token';

export const employmentStatusLabels: Record<number, string> = {
  [EmploymentStatus.Active]: 'Active',
  [EmploymentStatus.Probation]: 'Probation',
  [EmploymentStatus.OnLeave]: 'On leave',
  [EmploymentStatus.Suspended]: 'Suspended',
  [EmploymentStatus.Resigned]: 'Resigned',
  [EmploymentStatus.Terminated]: 'Terminated',
};

export const employmentTypeLabels: Record<number, string> = {
  1: 'Full time', 2: 'Part time', 3: 'Contract', 4: 'Intern', 5: 'Consultant',
};

export const genderLabels: Record<number, string> = { 1: 'Male', 2: 'Female', 3: 'Other' };

export const attendanceStatusLabels: Record<number, string> = {
  1: 'Present', 2: 'Late', 3: 'Absent', 4: 'Half day', 5: 'On leave', 6: 'Holiday', 7: 'Week off',
};

export const attendanceStatusStyles: Record<number, string> = {
  1: 'bg-accent-soft text-accent-strong',
  2: 'bg-amber-50 text-amber-700',
  3: 'bg-red-50 text-red-700',
  4: 'bg-orange-50 text-orange-700',
  5: 'bg-blue-50 text-blue-700',
  6: 'bg-ink-50 text-ink-600',
  7: 'bg-ink-50 text-ink-600',
};

export const leaveStatusLabels: Record<number, string> = {
  1: 'Pending', 2: 'Approved', 3: 'Rejected', 4: 'Cancelled',
};

export const leaveStatusStyles: Record<number, string> = {
  1: 'bg-amber-50 text-amber-700',
  2: 'bg-accent-soft text-accent-strong',
  3: 'bg-red-50 text-red-700',
  4: 'bg-ink-50 text-ink-600',
};
