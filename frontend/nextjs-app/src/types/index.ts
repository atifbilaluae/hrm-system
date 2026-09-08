export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  employeeId: string | null;
  isActive: boolean;
  roles: string[];
  permissions: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserProfile;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export const EmploymentStatus = {
  Active: 1, Probation: 2, OnLeave: 3, Suspended: 4, Resigned: 5, Terminated: 6,
} as const;
export type EmploymentStatusValue = (typeof EmploymentStatus)[keyof typeof EmploymentStatus];

export const EmploymentType = {
  FullTime: 1, PartTime: 2, Contract: 3, Intern: 4, Consultant: 5,
} as const;

export const Gender = { Male: 1, Female: 2, Other: 3 } as const;

export interface EmployeeListItem {
  id: string;
  employeeCode: string;
  fullName: string;
  email: string;
  phone: string | null;
  departmentName: string | null;
  designationName: string | null;
  employmentStatus: EmploymentStatusValue;
  joiningDate: string;
  profilePhotoUrl: string | null;
}

export interface EmployeeDetail extends Omit<EmployeeListItem, 'fullName'> {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: number;
  address: string | null;
  nationality: string | null;
  departmentId: string | null;
  designationId: string | null;
  managerId: string | null;
  terminationDate: string | null;
  employmentType: number;
  basicSalary: number;
  bankName: string | null;
  bankAccountNumber: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  emergencyContactRelation: string | null;
  createdAtUtc: string;
}

export interface EmployeeStats {
  totalEmployees: number;
  activeEmployees: number;
  newThisMonth: number;
  onLeave: number;
  probation: number;
  headcountByDepartment: { department: string; count: number }[];
  upcomingBirthdays: { employeeId: string; fullName: string; dateOfBirth: string; daysAway: number }[];
}

export interface AppNotification {
  id: string;
  title: string;
  message: string;
  type: number;
  link: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

export interface ApiErrorResponse {
  title: string;
  status: number;
  detail?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}
