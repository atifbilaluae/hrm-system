export interface Department {
  id: string;
  name: string;
  code: string;
  description: string | null;
  parentDepartmentId: string | null;
  parentDepartmentName: string | null;
  headEmployeeId: string | null;
  branchId: string | null;
  branchName: string | null;
  isActive: boolean;
  designationCount: number;
}

export interface DepartmentTreeNode {
  id: string;
  name: string;
  code: string;
  headEmployeeId: string | null;
  children: DepartmentTreeNode[];
}

export interface Designation {
  id: string;
  title: string;
  level: number;
  departmentId: string | null;
  departmentName: string | null;
  isActive: boolean;
}

export interface Branch {
  id: string;
  name: string;
  code: string | null;
  address: string | null;
  city: string;
  country: string;
  timeZoneId: string;
  isHeadOffice: boolean;
  departmentCount: number;
}

export interface Holiday {
  id: string;
  name: string;
  date: string;
  isRecurring: boolean;
  description: string | null;
}
