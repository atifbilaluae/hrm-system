import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';
import { format, formatDistanceToNow, parseISO } from 'date-fns';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatDate(value: string | Date | null | undefined, pattern = 'dd MMM yyyy') {
  if (!value) return '—';
  const date = typeof value === 'string' ? parseISO(value) : value;
  return Number.isNaN(date.getTime()) ? '—' : format(date, pattern);
}

export function timeAgo(value: string) {
  try {
    return formatDistanceToNow(parseISO(value), { addSuffix: true });
  } catch {
    return '';
  }
}

export function formatCurrency(amount: number, currency = 'PKR') {
  return new Intl.NumberFormat('en-PK', {
    style: 'currency', currency, maximumFractionDigits: 0,
  }).format(amount);
}

export function initials(name: string) {
  return name.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase()).join('');
}

/** 495 -> "8h 15m". Used everywhere attendance minutes are shown. */
export function formatMinutes(minutes: number) {
  if (!minutes) return '0h';
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return rest === 0 ? `${hours}h` : `${hours}h ${rest}m`;
}

export function formatTime(value: string | null | undefined) {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '—'
    : date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
}

export function todayIso() {
  return new Date().toISOString().slice(0, 10);
}
