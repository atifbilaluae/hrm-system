import { apiClient } from '@/lib/api-client';
import type { EmployeeDetail, EmployeeListItem, EmployeeStats, PagedResult } from '@/types';
import type { CreateEmployeeInput, UpdateEmployeeInput } from '@/schemas/employee.schema';

export interface EmployeeQueryParams {
  search?: string;
  departmentId?: string;
  status?: number;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  desc?: boolean;
}

export const employeeService = {
  async list(params: EmployeeQueryParams): Promise<PagedResult<EmployeeListItem>> {
    const { data } = await apiClient.get<PagedResult<EmployeeListItem>>('/api/employees', { params });
    return data;
  },

  async getById(id: string): Promise<EmployeeDetail> {
    const { data } = await apiClient.get<EmployeeDetail>(`/api/employees/${id}`);
    return data;
  },

  async stats(): Promise<EmployeeStats> {
    const { data } = await apiClient.get<EmployeeStats>('/api/employees/stats');
    return data;
  },

  async create(payload: CreateEmployeeInput): Promise<EmployeeDetail> {
    const { data } = await apiClient.post<EmployeeDetail>('/api/employees', payload);
    return data;
  },

  async update(id: string, payload: UpdateEmployeeInput): Promise<EmployeeDetail> {
    const { data } = await apiClient.put<EmployeeDetail>(`/api/employees/${id}`, payload);
    return data;
  },

  async terminate(id: string, terminationDate: string, reason?: string): Promise<void> {
    await apiClient.post(`/api/employees/${id}/terminate`, { terminationDate, reason });
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/api/employees/${id}`);
  },
};
