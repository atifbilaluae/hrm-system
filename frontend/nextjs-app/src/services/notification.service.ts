import { apiClient } from '@/lib/api-client';
import type { AppNotification, PagedResult } from '@/types';

export const notificationService = {
  async list(unreadOnly = false, page = 1, pageSize = 20): Promise<PagedResult<AppNotification>> {
    const { data } = await apiClient.get<PagedResult<AppNotification>>('/api/notifications', {
      params: { unreadOnly, page, pageSize },
    });
    return data;
  },

  async unreadCount(): Promise<number> {
    const { data } = await apiClient.get<number>('/api/notifications/unread-count');
    return data;
  },

  async markRead(id: string): Promise<void> {
    await apiClient.post(`/api/notifications/${id}/read`);
  },

  async markAllRead(): Promise<void> {
    await apiClient.post('/api/notifications/read-all');
  },
};
