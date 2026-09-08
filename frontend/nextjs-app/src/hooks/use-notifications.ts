'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationService } from '@/services/notification.service';

export const notificationKeys = {
  all: ['notifications'] as const,
  list: (unreadOnly: boolean) => ['notifications', 'list', unreadOnly] as const,
  unreadCount: ['notifications', 'unread-count'] as const,
};

export function useNotifications(unreadOnly = false) {
  return useQuery({
    queryKey: notificationKeys.list(unreadOnly),
    queryFn: () => notificationService.list(unreadOnly),
  });
}

export function useUnreadCount() {
  return useQuery({
    queryKey: notificationKeys.unreadCount,
    queryFn: notificationService.unreadCount,
    // Events arrive asynchronously, so poll. Phase 2 replaces this with SignalR.
    refetchInterval: 30_000,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationService.markRead(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notificationKeys.all }),
  });
}

export function useMarkAllRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: notificationService.markAllRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notificationKeys.all }),
  });
}
