'use client';

import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { employeeService, type EmployeeQueryParams } from '@/services/employee.service';
import { getErrorMessage } from '@/lib/api-client';
import type { CreateEmployeeInput, UpdateEmployeeInput } from '@/schemas/employee.schema';

export const employeeKeys = {
  all: ['employees'] as const,
  list: (params: EmployeeQueryParams) => ['employees', 'list', params] as const,
  detail: (id: string) => ['employees', 'detail', id] as const,
  stats: ['employees', 'stats'] as const,
};

export function useEmployees(params: EmployeeQueryParams) {
  return useQuery({
    queryKey: employeeKeys.list(params),
    queryFn: () => employeeService.list(params),
    // Keeps the previous page visible while the next one loads - no table flicker.
    placeholderData: keepPreviousData,
  });
}

export function useEmployee(id: string) {
  return useQuery({
    queryKey: employeeKeys.detail(id),
    queryFn: () => employeeService.getById(id),
    enabled: Boolean(id),
  });
}

export function useEmployeeStats() {
  return useQuery({
    queryKey: employeeKeys.stats,
    queryFn: employeeService.stats,
    staleTime: 60 * 1000,
  });
}

export function useCreateEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateEmployeeInput) => employeeService.create(payload),
    onSuccess: (employee) => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all });
      toast.success(`${employee.firstName} ${employee.lastName} added as ${employee.employeeCode}`);
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not add the employee.')),
  });
}

export function useUpdateEmployee(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateEmployeeInput) => employeeService.update(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all });
      toast.success('Employee updated');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not update the employee.')),
  });
}

export function useDeleteEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => employeeService.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all });
      toast.success('Employee removed');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not remove the employee.')),
  });
}
