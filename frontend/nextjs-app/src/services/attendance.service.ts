import { apiClient } from '@/lib/api-client';
import type {
  AttendanceRecord, AttendanceSummary, CorrectionRequest,
  DailyAttendanceSummary, TodayAttendance,
} from '@/types/attendance';
import type { PagedResult } from '@/types';

export const attendanceService = {
  async today(): Promise<TodayAttendance> {
    const { data } = await apiClient.get<TodayAttendance>('/api/attendance/today');
    return data;
  },
  async checkIn(): Promise<AttendanceRecord> {
    const { data } = await apiClient.post<AttendanceRecord>('/api/attendance/check-in');
    return data;
  },
  async checkOut(): Promise<AttendanceRecord> {
    const { data } = await apiClient.post<AttendanceRecord>('/api/attendance/check-out');
    return data;
  },
  async daily(date: string, page = 1, pageSize = 20): Promise<PagedResult<AttendanceRecord>> {
    const { data } = await apiClient.get<PagedResult<AttendanceRecord>>('/api/attendance', {
      params: { date, page, pageSize },
    });
    return data;
  },
  async dailySummary(date: string): Promise<DailyAttendanceSummary> {
    const { data } = await apiClient.get<DailyAttendanceSummary>('/api/attendance/summary/daily', {
      params: { date },
    });
    return data;
  },
  async myMonth(year: number, month: number): Promise<AttendanceRecord[]> {
    const { data } = await apiClient.get<AttendanceRecord[]>('/api/attendance/me/monthly', {
      params: { year, month },
    });
    return data;
  },
  async mySummary(year: number, month: number): Promise<AttendanceSummary> {
    const { data } = await apiClient.get<AttendanceSummary>('/api/attendance/me/summary', {
      params: { year, month },
    });
    return data;
  },
  async requestCorrection(payload: {
    workDate: string;
    requestedCheckInUtc?: string | null;
    requestedCheckOutUtc?: string | null;
    reason: string;
  }): Promise<CorrectionRequest> {
    const { data } = await apiClient.post<CorrectionRequest>('/api/attendance/corrections', payload);
    return data;
  },
  async pendingCorrections(): Promise<CorrectionRequest[]> {
    const { data } = await apiClient.get<CorrectionRequest[]>('/api/attendance/corrections/pending');
    return data;
  },
  async decideCorrection(id: string, approve: boolean, comments?: string): Promise<CorrectionRequest> {
    const { data } = await apiClient.post<CorrectionRequest>(
      `/api/attendance/corrections/${id}/decide`, { approve, comments });
    return data;
  },
};
