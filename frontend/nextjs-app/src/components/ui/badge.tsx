import { cn } from '@/lib/utils';
import { EmploymentStatus } from '@/types';
import { employmentStatusLabels } from '@/lib/constants';

const statusStyles: Record<number, string> = {
  [EmploymentStatus.Active]: 'bg-accent-soft text-accent-strong',
  [EmploymentStatus.Probation]: 'bg-amber-50 text-amber-700',
  [EmploymentStatus.OnLeave]: 'bg-blue-50 text-blue-700',
  [EmploymentStatus.Suspended]: 'bg-orange-50 text-orange-700',
  [EmploymentStatus.Resigned]: 'bg-ink-50 text-ink-600',
  [EmploymentStatus.Terminated]: 'bg-red-50 text-red-700',
};

export function StatusBadge({ status }: { status: number }) {
  return (
    <span className={cn('inline-flex rounded px-2 py-0.5 text-xs font-medium', statusStyles[status] ?? 'bg-ink-50 text-ink-600')}>
      {employmentStatusLabels[status] ?? 'Unknown'}
    </span>
  );
}

export function Badge({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <span className={cn('inline-flex rounded px-2 py-0.5 text-xs font-medium bg-ink-50 text-ink-600', className)}>
      {children}
    </span>
  );
}
