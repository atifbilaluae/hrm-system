'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { leaveService } from '@/services/leave.service';
import { getErrorMessage } from '@/lib/api-client';
import type { LeaveRequestInput } from '@/schemas/leave.schema';

export const leaveKeys = {
  all: ['leave'] as const,
  types: ['leave', 'types'] as const,
  balances: (year: number) => ['leave', 'balances', year] as const,
  myRequests: ['leave', 'requests', 'me'] as const,
  pending: ['leave', 'requests', 'pending'] as const,
};

export function useLeaveTypes() {
  return useQuery({ queryKey: leaveKeys.types, queryFn: leaveService.types, staleTime: 10 * 60 * 1000 });
}

export function useMyLeaveBalances(year = new Date().getFullYear()) {
  return useQuery({ queryKey: leaveKeys.balances(year), queryFn: () => leaveService.myBalances(year) });
}

export function useMyLeaveRequests() {
  return useQuery({ queryKey: leaveKeys.myRequests, queryFn: () => leaveService.myRequests() });
}

export function usePendingLeaveRequests() {
  return useQuery({ queryKey: leaveKeys.pending, queryFn: () => leaveService.pending() });
}

export function useSubmitLeave() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: LeaveRequestInput) => leaveService.submit(payload),
    onSuccess: (request) => {
      queryClient.invalidateQueries({ queryKey: leaveKeys.all });
      toast.success(`${request.totalDays} day(s) requested — your manager has been notified`);
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not submit the request.')),
  });
}

export function useDecideLeave() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, approve, comments }: { id: string; approve: boolean; comments?: string }) =>
      leaveService.decide(id, approve, comments),
    onSuccess: (request) => {
      queryClient.invalidateQueries({ queryKey: leaveKeys.all });
      toast.success(request.status === 2 ? 'Leave approved' : 'Leave rejected');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not record the decision.')),
  });
}

export function useCancelLeave() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => leaveService.cancel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: leaveKeys.all });
      toast.success('Request cancelled');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not cancel the request.')),
  });
}
