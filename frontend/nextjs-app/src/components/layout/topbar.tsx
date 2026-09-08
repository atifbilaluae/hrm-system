'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Bell, LogOut, Menu, Search } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { useCurrentUser, useLogout } from '@/hooks/use-auth';
import { useUnreadCount } from '@/hooks/use-notifications';
import { initials } from '@/lib/utils';

export function Topbar({ onOpenSidebar }: { onOpenSidebar: () => void }) {
  const { data: user } = useCurrentUser();
  const { data: unread = 0 } = useUnreadCount();
  const logout = useLogout();
  const [menuOpen, setMenuOpen] = useState(false);

  return (
    <header className="flex h-14 items-center gap-3 border-b border-line bg-white px-4 lg:px-6">
      <Button variant="ghost" size="icon" className="lg:hidden" onClick={onOpenSidebar} aria-label="Open menu">
        <Menu className="h-5 w-5" />
      </Button>

      <div className="relative hidden max-w-sm flex-1 sm:block">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400" aria-hidden />
        <Input placeholder="Search employees, requests, documents" className="pl-9" aria-label="Global search" />
      </div>

      <div className="ml-auto flex items-center gap-1">
        <Link
          href="/notifications"
          className="relative rounded-md p-2 text-ink-600 hover:bg-ink-50"
          aria-label={`Notifications${unread ? `, ${unread} unread` : ''}`}
        >
          <Bell className="h-5 w-5" />
          {unread > 0 && (
            <span className="absolute right-1 top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-accent px-1 text-[10px] font-semibold text-white">
              {unread > 9 ? '9+' : unread}
            </span>
          )}
        </Link>

        <div className="relative">
          <button
            onClick={() => setMenuOpen((open) => !open)}
            className="flex items-center gap-2 rounded-md px-2 py-1.5 hover:bg-ink-50"
            aria-haspopup="menu"
            aria-expanded={menuOpen}
          >
            <span className="flex h-8 w-8 items-center justify-center rounded-full bg-ink text-xs font-semibold text-white">
              {initials(user?.fullName ?? '')}
            </span>
            <span className="hidden text-left sm:block">
              <span className="block text-sm font-medium leading-tight text-ink">{user?.fullName}</span>
              <span className="block text-xs leading-tight text-ink-400">{user?.roles?.[0]}</span>
            </span>
          </button>

          {menuOpen && (
            <div role="menu" className="absolute right-0 mt-1 w-52 rounded-md border border-line bg-white py-1 shadow-lg">
              <div className="border-b border-line px-3 py-2">
                <p className="truncate text-sm font-medium text-ink">{user?.fullName}</p>
                <p className="truncate text-xs text-ink-400">{user?.email}</p>
              </div>
              <Link href="/settings" role="menuitem" className="block px-3 py-2 text-sm text-ink-600 hover:bg-ink-50">
                Settings
              </Link>
              <button
                role="menuitem"
                onClick={() => logout.mutate()}
                className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-ink-600 hover:bg-ink-50"
              >
                <LogOut className="h-4 w-4" aria-hidden />
                Sign out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
