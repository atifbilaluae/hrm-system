'use client';

import * as React from 'react';
import { X } from 'lucide-react';
import { Button } from './button';

/** Minimal accessible modal: focus trap is handled by the browser dialog semantics + Escape key. */
export function Dialog({
  open, onClose, title, description, children, footer,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children?: React.ReactNode;
  footer?: React.ReactNode;
}) {
  React.useEffect(() => {
    if (!open) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-ink-900/40" onClick={onClose} aria-hidden />
      <div role="dialog" aria-modal="true" aria-label={title}
           className="relative w-full max-w-md rounded-lg border border-line bg-white shadow-lg">
        <div className="flex items-start justify-between border-b border-line px-5 py-4">
          <div>
            <h2 className="text-sm font-semibold text-ink">{title}</h2>
            {description && <p className="mt-1 text-sm text-ink-400">{description}</p>}
          </div>
          <button onClick={onClose} aria-label="Close" className="rounded p-1 text-ink-400 hover:bg-ink-50">
            <X className="h-4 w-4" />
          </button>
        </div>
        {children && <div className="px-5 py-4">{children}</div>}
        <div className="flex justify-end gap-2 border-t border-line px-5 py-3">
          {footer ?? <Button variant="outline" onClick={onClose}>Close</Button>}
        </div>
      </div>
    </div>
  );
}

export function ConfirmDialog({
  open, onClose, onConfirm, title, description, confirmLabel = 'Confirm', loading,
}: {
  open: boolean; onClose: () => void; onConfirm: () => void;
  title: string; description: string; confirmLabel?: string; loading?: boolean;
}) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={title}
      description={description}
      footer={
        <>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button variant="danger" loading={loading} onClick={onConfirm}>{confirmLabel}</Button>
        </>
      }
    />
  );
}
