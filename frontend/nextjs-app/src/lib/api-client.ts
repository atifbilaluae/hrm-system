'use client';

import axios, { AxiosError, type AxiosRequestConfig } from 'axios';
import Cookies from 'js-cookie';
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from './constants';
import type { ApiErrorResponse, AuthResponse } from '@/types';

/**
 * Single axios instance for the whole app. Every request goes to the API Gateway,
 * never directly to a microservice - the frontend does not know which service
 * owns which route, so services can be split or moved without touching the UI.
 */
export const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080',
  headers: { 'Content-Type': 'application/json' },
  timeout: 20000,
});

export const tokenStore = {
  get access() {
    return Cookies.get(ACCESS_TOKEN_COOKIE);
  },
  get refresh() {
    return Cookies.get(REFRESH_TOKEN_COOKIE);
  },
  save(auth: AuthResponse) {
    // Readable by the Next middleware so protected routes can redirect on the server.
    Cookies.set(ACCESS_TOKEN_COOKIE, auth.accessToken, { sameSite: 'lax', expires: 1 });
    Cookies.set(REFRESH_TOKEN_COOKIE, auth.refreshToken, { sameSite: 'lax', expires: 7 });
  },
  clear() {
    Cookies.remove(ACCESS_TOKEN_COOKIE);
    Cookies.remove(REFRESH_TOKEN_COOKIE);
  },
};

apiClient.interceptors.request.use((config) => {
  const token = tokenStore.access;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// --- Silent refresh -------------------------------------------------------
// When a 401 comes back, try the refresh token exactly once and replay the
// original request. Parallel 401s share one refresh call via this promise.
let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = tokenStore.refresh;
  if (!refreshToken) return null;

  try {
    const { data } = await axios.post<AuthResponse>(
      `${apiClient.defaults.baseURL}/api/auth/refresh`,
      { refreshToken },
      { headers: { 'Content-Type': 'application/json' } },
    );
    tokenStore.save(data);
    return data.accessToken;
  } catch {
    tokenStore.clear();
    return null;
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiErrorResponse>) => {
    const original = error.config as AxiosRequestConfig & { _retried?: boolean };

    if (error.response?.status === 401 && original && !original._retried) {
      original._retried = true;
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null;
      });

      const newToken = await refreshPromise;
      if (newToken) {
        original.headers = { ...original.headers, Authorization: `Bearer ${newToken}` };
        return apiClient(original);
      }

      if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login')) {
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  },
);

/** Turns any axios failure into a message that is safe to show a user. */
export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Try again.') {
  if (axios.isAxiosError<ApiErrorResponse>(error)) {
    const data = error.response?.data;
    if (data?.errors) {
      const first = Object.values(data.errors)[0];
      if (first?.length) return first[0];
    }
    return data?.detail ?? data?.title ?? error.message ?? fallback;
  }
  return fallback;
}
