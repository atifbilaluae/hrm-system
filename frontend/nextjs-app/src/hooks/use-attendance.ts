'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { attendanceService } from '@/services/attendance.service';
import { getErrorMessage } from '@/lib/api-client';

export const attendanceKeys = {
  all: ['attendance'] as const,
  today: ['attendance', 'today'] as const,
  daily: (date: string) => ['attendance', 'daily', date] as const,
  dailySummary: (date: string) => ['attendance', 'daily-summary', date] as const,
  myMonth: (year: number, month: number) => ['attendance', 'me', year, month] as const,
  mySummary: (year: number, month: number) => ['attendance', 'me', 'summary', year, month] as const,
  corrections: ['attendance', 'corrections', 'pending'] as const,
};

export function useTodayAttendance() {
  return useQuery({ queryKey: attendanceKeys.today, queryFn: attendanceService.today });
}

export function useDailyAttendance(date: string) {
  return useQuery({ queryKey: attendanceKeys.daily(date), queryFn: () => attendanceService.daily(date) });
}

export function useDailySummary(date: string) {
  return useQuery({
    queryKey: attendanceKeys.dailySummary(date),
    queryFn: () => attendanceService.dailySummary(date),
  });
}

export function useMyMonthlyAttendance(year: number, month: number) {
  return useQuery({
    queryKey: attendanceKeys.myMonth(year, month),
    queryFn: () => attendanceService.myMonth(year, month),
  });
}

export function useMyAttendanceSummary(year: number, month: number) {
  return useQuery({
    queryKey: attendanceKeys.mySummary(year, month),
    queryFn: () => attendanceService.mySummary(year, month),
  });
}

export function useCheckIn() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: attendanceService.checkIn,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: attendanceKeys.all });
      toast.success('Checked in');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not check you in.')),
  });
}

export function useCheckOut() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: attendanceService.checkOut,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: attendanceKeys.all });
      toast.success('Checked out');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not check you out.')),
  });
}

export function useRequestCorrection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: attendanceService.requestCorrection,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: attendanceKeys.all });
      toast.success('Correction sent for review');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not send the correction.')),
  });
}
