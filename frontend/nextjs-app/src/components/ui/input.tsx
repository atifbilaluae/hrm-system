'use client';

import * as React from 'react';
import { cn } from '@/lib/utils';

export const Input = React.forwardRef<HTMLInputElement, React.InputHTMLAttributes<HTMLInputElement>>(
  ({ className, ...props }, ref) => (
    <input
      ref={ref}
      className={cn(
        'h-9 w-full rounded-md border border-line bg-white px-3 text-sm text-ink placeholder:text-ink-400',
        'focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent disabled:bg-ink-50',
        className,
      )}
      {...props}
    />
  ),
);
Input.displayName = 'Input';

export const Select = React.forwardRef<HTMLSelectElement, React.SelectHTMLAttributes<HTMLSelectElement>>(
  ({ className, children, ...props }, ref) => (
    <select
      ref={ref}
      className={cn(
        'h-9 w-full rounded-md border border-line bg-white px-3 text-sm text-ink',
        'focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent',
        className,
      )}
      {...props}
    >
      {children}
    </select>
  ),
);
Select.displayName = 'Select';

export const Textarea = React.forwardRef<HTMLTextAreaElement, React.TextareaHTMLAttributes<HTMLTextAreaElement>>(
  ({ className, ...props }, ref) => (
    <textarea
      ref={ref}
      className={cn(
        'w-full rounded-md border border-line bg-white p-3 text-sm text-ink placeholder:text-ink-400',
        'focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent',
        className,
      )}
      {...props}
    />
  ),
);
Textarea.displayName = 'Textarea';

export function Field({
  label, error, hint, required, children,
}: {
  label: string; error?: string; hint?: string; required?: boolean; children: React.ReactNode;
}) {
  return (
    <label className="block space-y-1.5">
      <span className="text-sm font-medium text-ink-700">
        {label}
        {required && <span className="ml-0.5 text-red-600">*</span>}
      </span>
      {children}
      {error ? (
        <span className="block text-xs text-red-600">{error}</span>
      ) : hint ? (
        <span className="block text-xs text-ink-400">{hint}</span>
      ) : null}
    </label>
  );
}
