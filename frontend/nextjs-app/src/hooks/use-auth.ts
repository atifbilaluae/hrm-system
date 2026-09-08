'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { authService } from '@/services/auth.service';
import { tokenStore } from '@/lib/api-client';
import type { UserProfile } from '@/types';

export const authKeys = { me: ['auth', 'me'] as const };

/** Loads the signed-in user once and shares it across the whole app. */
export function useCurrentUser() {
  return useQuery<UserProfile>({
    queryKey: authKeys.me,
    queryFn: authService.me,
    staleTime: 5 * 60 * 1000,
    retry: false,
  });
}

export function useLogin() {
  const router = useRouter();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) =>
      authService.login(email, password),
    onSuccess: (data) => {
      queryClient.setQueryData(authKeys.me, data.user);
      router.replace('/dashboard');
    },
  });
}

export function useLogout() {
  const router = useRouter();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: authService.logout,
    onSettled: () => {
      tokenStore.clear();
      queryClient.clear();
      router.replace('/login');
    },
  });
}

/** Permission check used to hide actions the user cannot perform. */
export function usePermissions() {
  const { data: user } = useCurrentUser();
  return {
    user,
    can: (permission: string) => user?.permissions?.includes(permission) ?? false,
    hasRole: (role: string) => user?.roles?.includes(role) ?? false,
  };
}
