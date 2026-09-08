import { apiClient } from '@/lib/api-client';
import type { Branch, Department, DepartmentTreeNode, Designation, Holiday } from '@/types/organization';

export interface SaveDepartmentPayload {
  name: string;
  code: string;
  description?: string;
  parentDepartmentId?: string | null;
  headEmployeeId?: string | null;
  branchId?: string | null;
  isActive: boolean;
}

export const organizationService = {
  async departments(): Promise<Department[]> {
    const { data } = await apiClient.get<Department[]>('/api/departments');
    return data;
  },
  async departmentTree(): Promise<DepartmentTreeNode[]> {
    const { data } = await apiClient.get<DepartmentTreeNode[]>('/api/departments/tree');
    return data;
  },
  async createDepartment(payload: SaveDepartmentPayload): Promise<Department> {
    const { data } = await apiClient.post<Department>('/api/departments', payload);
    return data;
  },
  async updateDepartment(id: string, payload: SaveDepartmentPayload): Promise<Department> {
    const { data } = await apiClient.put<Department>(`/api/departments/${id}`, payload);
    return data;
  },
  async deleteDepartment(id: string): Promise<void> {
    await apiClient.delete(`/api/departments/${id}`);
  },
  async designations(departmentId?: string): Promise<Designation[]> {
    const { data } = await apiClient.get<Designation[]>('/api/designations', { params: { departmentId } });
    return data;
  },
  async branches(): Promise<Branch[]> {
    const { data } = await apiClient.get<Branch[]>('/api/branches');
    return data;
  },
  async holidays(year?: number): Promise<Holiday[]> {
    const { data } = await apiClient.get<Holiday[]>('/api/holidays', { params: { year } });
    return data;
  },
};
