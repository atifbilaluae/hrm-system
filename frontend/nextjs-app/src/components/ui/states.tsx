import { AlertCircle, Inbox, Loader2 } from 'lucide-react';
import { Button } from './button';

export function LoadingState({ label = 'Loading' }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-16 text-sm text-ink-400">
      <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
      {label}
    </div>
  );
}

export function EmptyState({
  title, description, action,
}: { title: string; description: string; action?: React.ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-16 text-center">
      <Inbox className="h-8 w-8 text-ink-200" aria-hidden />
      <p className="text-sm font-medium text-ink">{title}</p>
      <p className="max-w-sm text-sm text-ink-400">{description}</p>
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-16 text-center">
      <AlertCircle className="h-8 w-8 text-red-400" aria-hidden />
      <p className="text-sm font-medium text-ink">Could not load this data</p>
      <p className="max-w-sm text-sm text-ink-400">{message}</p>
      {onRetry && (
        <Button variant="outline" size="sm" className="mt-2" onClick={onRetry}>
          Try again
        </Button>
      )}
    </div>
  );
}

export function TableSkeleton({ rows = 6, columns = 5 }: { rows?: number; columns?: number }) {
  return (
    <div className="divide-y divide-line">
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="flex gap-4 px-5 py-3.5">
          {Array.from({ length: columns }).map((__, colIndex) => (
            <div key={colIndex} className="h-4 flex-1 animate-pulse rounded bg-ink-50" />
          ))}
        </div>
      ))}
    </div>
  );
}
