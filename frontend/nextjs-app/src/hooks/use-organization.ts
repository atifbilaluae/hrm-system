'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { organizationService, type SaveDepartmentPayload } from '@/services/organization.service';
import { getErrorMessage } from '@/lib/api-client';

export const organizationKeys = {
  all: ['organization'] as const,
  departments: ['organization', 'departments'] as const,
  tree: ['organization', 'departments', 'tree'] as const,
  designations: (departmentId?: string) => ['organization', 'designations', departmentId ?? 'all'] as const,
  branches: ['organization', 'branches'] as const,
  holidays: (year: number) => ['organization', 'holidays', year] as const,
};

export function useDepartments() {
  return useQuery({ queryKey: organizationKeys.departments, queryFn: organizationService.departments });
}

export function useDepartmentTree() {
  return useQuery({ queryKey: organizationKeys.tree, queryFn: organizationService.departmentTree });
}

export function useDesignations(departmentId?: string) {
  return useQuery({
    queryKey: organizationKeys.designations(departmentId),
    queryFn: () => organizationService.designations(departmentId),
  });
}

export function useBranches() {
  return useQuery({ queryKey: organizationKeys.branches, queryFn: organizationService.branches });
}

export function useHolidays(year = new Date().getFullYear()) {
  return useQuery({
    queryKey: organizationKeys.holidays(year),
    queryFn: () => organizationService.holidays(year),
  });
}

export function useCreateDepartment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveDepartmentPayload) => organizationService.createDepartment(payload),
    onSuccess: (department) => {
      queryClient.invalidateQueries({ queryKey: organizationKeys.all });
      toast.success(`${department.name} added`);
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not save the department.')),
  });
}

export function useDeleteDepartment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => organizationService.deleteDepartment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: organizationKeys.all });
      toast.success('Department removed');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'Could not remove the department.')),
  });
}
