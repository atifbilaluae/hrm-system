import { apiClient } from '@/lib/api-client';
import type { LeaveBalance, LeaveRequest, LeaveType } from '@/types/leave';
import type { PagedResult } from '@/types';

export const leaveService = {
  async types(): Promise<LeaveType[]> {
    const { data } = await apiClient.get<LeaveType[]>('/api/leave-types');
    return data;
  },
  async myBalances(year?: number): Promise<LeaveBalance[]> {
    const { data } = await apiClient.get<LeaveBalance[]>('/api/leave/balances', { params: { year } });
    return data;
  },
  async myRequests(page = 1, pageSize = 20): Promise<PagedResult<LeaveRequest>> {
    const { data } = await apiClient.get<PagedResult<LeaveRequest>>('/api/leave/requests/me', {
      params: { page, pageSize },
    });
    return data;
  },
  async pending(page = 1, pageSize = 20): Promise<PagedResult<LeaveRequest>> {
    const { data } = await apiClient.get<PagedResult<LeaveRequest>>('/api/leave/requests/pending', {
      params: { page, pageSize },
    });
    return data;
  },
  async submit(payload: { leaveTypeId: string; fromDate: string; toDate: string; reason: string }): Promise<LeaveRequest> {
    const { data } = await apiClient.post<LeaveRequest>('/api/leave/requests', payload);
    return data;
  },
  async decide(id: string, approve: boolean, comments?: string): Promise<LeaveRequest> {
    const { data } = await apiClient.post<LeaveRequest>(`/api/leave/requests/${id}/decide`, { approve, comments });
    return data;
  },
  async cancel(id: string): Promise<LeaveRequest> {
    const { data } = await apiClient.post<LeaveRequest>(`/api/leave/requests/${id}/cancel`);
    return data;
  },
};
