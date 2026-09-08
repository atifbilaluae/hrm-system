'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  BarChart3, Building2, CalendarDays, ClipboardList, Clock, LayoutDashboard,
  Settings, Target, Users, Wallet, Bell,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { PERMISSIONS } from '@/lib/constants';
import { usePermissions } from '@/hooks/use-auth';

const navigation = [
  { href: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, permission: null },
  { href: '/employees', label: 'Employees', icon: Users, permission: PERMISSIONS.employeeView },
  { href: '/organization', label: 'Organization', icon: Building2, permission: PERMISSIONS.organizationManage },
  { href: '/attendance', label: 'Attendance', icon: Clock, permission: PERMISSIONS.attendanceView },
  { href: '/leave', label: 'Leave', icon: CalendarDays, permission: PERMISSIONS.leaveRequest },
  { href: '/payroll', label: 'Payroll', icon: Wallet, permission: PERMISSIONS.payrollView },
  { href: '/recruitment', label: 'Recruitment', icon: ClipboardList, permission: PERMISSIONS.recruitmentManage },
  { href: '/performance', label: 'Performance', icon: Target, permission: PERMISSIONS.performanceManage },
  { href: '/reports', label: 'Reports', icon: BarChart3, permission: PERMISSIONS.employeeView },
  { href: '/notifications', label: 'Notifications', icon: Bell, permission: null },
  { href: '/settings', label: 'Settings', icon: Settings, permission: null },
];

export function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const pathname = usePathname();
  const { can } = usePermissions();

  return (
    <nav aria-label="Main" className="flex h-full w-60 flex-col bg-ink text-ink-100">
      <div className="flex h-14 items-center gap-2 border-b border-white/10 px-5">
        <span className="flex h-7 w-7 items-center justify-center rounded bg-accent text-xs font-bold text-white">
          HR
        </span>
        <span className="text-sm font-semibold tracking-tight text-white">Workforce</span>
      </div>

      <ul className="flex-1 space-y-0.5 overflow-y-auto p-3">
        {navigation
          .filter((item) => !item.permission || can(item.permission))
          .map(({ href, label, icon: Icon }) => {
            const active = pathname === href || pathname.startsWith(`${href}/`);
            return (
              <li key={href}>
                <Link
                  href={href}
                  onClick={onNavigate}
                  aria-current={active ? 'page' : undefined}
                  className={cn(
                    'flex items-center gap-3 rounded-md px-3 py-2 text-sm',
                    active ? 'bg-white/10 text-white' : 'text-ink-200 hover:bg-white/5 hover:text-white',
                  )}
                >
                  <Icon className="h-4 w-4 shrink-0" aria-hidden />
                  {label}
                </Link>
              </li>
            );
          })}
      </ul>

      <div className="border-t border-white/10 px-5 py-3 text-xs text-ink-400">HRM v1.0</div>
    </nav>
  );
}
