'use client';

import { useState } from 'react';
import Link from 'next/link';
import { CheckCheck } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/card';
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/states';
import { useMarkAllRead, useMarkNotificationRead, useNotifications } from '@/hooks/use-notifications';
import { getErrorMessage } from '@/lib/api-client';
import { cn, timeAgo } from '@/lib/utils';

export default function NotificationsPage() {
  const [unreadOnly, setUnreadOnly] = useState(false);
  const { data, isLoading, isError, error, refetch } = useNotifications(unreadOnly);
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllRead();

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <div>
        <h1 className="text-lg font-semibold tracking-tight text-ink">Notifications</h1>
        <p className="text-sm text-ink-400">
          Written by the notification service as events arrive from the rest of the platform.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{unreadOnly ? 'Unread' : 'All notifications'}</CardTitle>
          <div className="flex gap-2">
            <Button variant="ghost" size="sm" onClick={() => setUnreadOnly((value) => !value)}>
              {unreadOnly ? 'Show all' : 'Show unread'}
            </Button>
            <Button variant="outline" size="sm" onClick={() => markAllRead.mutate()} loading={markAllRead.isPending}>
              <CheckCheck className="h-4 w-4" aria-hidden />
              Mark all read
            </Button>
          </div>
        </CardHeader>

        <CardBody className="p-0">
          {isLoading ? (
            <LoadingState />
          ) : isError ? (
            <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />
          ) : !data || data.items.length === 0 ? (
            <EmptyState
              title="Nothing here yet"
              description="Add an employee or submit a leave request and the first notification will land here."
            />
          ) : (
            <ul className="divide-y divide-line">
              {data.items.map((notification) => (
                <li
                  key={notification.id}
                  className={cn('flex gap-3 px-5 py-4', !notification.isRead && 'bg-accent-soft/40')}
                >
                  <span
                    className={cn('mt-1.5 h-2 w-2 shrink-0 rounded-full', notification.isRead ? 'bg-ink-200' : 'bg-accent')}
                    aria-hidden
                  />
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium text-ink">{notification.title}</p>
                    <p className="mt-0.5 text-sm text-ink-600">{notification.message}</p>
                    <p className="mt-1 text-xs text-ink-400">{timeAgo(notification.createdAtUtc)}</p>
                  </div>
                  <div className="flex shrink-0 items-start gap-2">
                    {notification.link && (
                      <Link href={notification.link} className="text-sm text-accent hover:underline">
                        Open
                      </Link>
                    )}
                    {!notification.isRead && (
                      <button
                        onClick={() => markRead.mutate(notification.id)}
                        className="text-sm text-ink-400 hover:text-ink"
                      >
                        Mark read
                      </button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardBody>
      </Card>
    </div>
  );
}
