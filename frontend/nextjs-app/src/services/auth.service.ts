import { apiClient, tokenStore } from '@/lib/api-client';
import type { AuthResponse, UserProfile } from '@/types';

/**
 * Service layer: the only place that knows API paths and shapes.
 * Components and hooks talk to this, never to axios directly.
 */
export const authService = {
  async login(email: string, password: string): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/api/auth/login', { email, password });
    tokenStore.save(data);
    return data;
  },

  async me(): Promise<UserProfile> {
    const { data } = await apiClient.get<UserProfile>('/api/auth/me');
    return data;
  },

  async logout(): Promise<void> {
    const refreshToken = tokenStore.refresh;
    try {
      if (refreshToken) await apiClient.post('/api/auth/logout', { refreshToken });
    } finally {
      tokenStore.clear();
    }
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await apiClient.post('/api/auth/change-password', { currentPassword, newPassword });
  },
};
