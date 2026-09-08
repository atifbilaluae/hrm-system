'use client';

import { useState } from 'react';
import { LogIn, LogOut } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { EmptyState, ErrorState, TableSkeleton } from '@/components/ui/states';
import { PermissionGate } from '@/components/permission-gate';
import {
  useCheckIn, useCheckOut, useDailyAttendance, useDailySummary,
  useMyAttendanceSummary, useTodayAttendance,
} from '@/hooks/use-attendance';
import { getErrorMessage } from '@/lib/api-client';
import { PERMISSIONS, attendanceStatusLabels, attendanceStatusStyles } from '@/lib/constants';
import { cn, formatMinutes, formatTime, todayIso } from '@/lib/utils';

function AttendanceBadge({ status }: { status: number }) {
  return (
    <span className={cn('inline-flex rounded px-2 py-0.5 text-xs font-medium',
      attendanceStatusStyles[status] ?? 'bg-ink-50 text-ink-600')}>
      {attendanceStatusLabels[status] ?? 'Unknown'}
    </span>
  );
}

export default function AttendancePage() {
  const [date, setDate] = useState(todayIso());
  const now = new Date();

  const today = useTodayAttendance();
  const checkIn = useCheckIn();
  const checkOut = useCheckOut();
  const summary = useMyAttendanceSummary(now.getFullYear(), now.getMonth() + 1);
  const daily = useDailyAttendance(date);
  const dailySummary = useDailySummary(date);

  const record = today.data?.record;

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-lg font-semibold tracking-tight text-ink">Attendance</h1>
        <p className="text-sm text-ink-400">
          Check in once a day. Late arrivals, worked hours and overtime are calculated for you.
        </p>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-1">
          <CardHeader><CardTitle>Today</CardTitle></CardHeader>
          <CardBody className="space-y-4">
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <p className="text-xs text-ink-400">Checked in</p>
                <p className="tabular mt-0.5 font-medium text-ink">{formatTime(record?.checkInUtc)}</p>
              </div>
              <div>
                <p className="text-xs text-ink-400">Checked out</p>
                <p className="tabular mt-0.5 font-medium text-ink">{formatTime(record?.checkOutUtc)}</p>
              </div>
              <div>
                <p className="text-xs text-ink-400">Worked</p>
                <p className="tabular mt-0.5 font-medium text-ink">{formatMinutes(record?.workedMinutes ?? 0)}</p>
              </div>
              <div>
                <p className="text-xs text-ink-400">Status</p>
                <p className="mt-0.5">{record ? <AttendanceBadge status={record.status} /> : '—'}</p>
              </div>
            </div>

            {record && record.lateMinutes > 0 && (
              <p className="rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-700">
                Marked late by {formatMinutes(record.lateMinutes)} past the grace period.
              </p>
            )}
            {record && record.overtimeMinutes > 0 && (
              <p className="rounded-md bg-accent-soft px-3 py-2 text-xs text-accent-strong">
                {formatMinutes(record.overtimeMinutes)} of overtime recorded.
              </p>
            )}

            <div className="flex gap-2">
              <Button
                variant="accent"
                className="flex-1"
                disabled={today.data?.hasCheckedIn}
                loading={checkIn.isPending}
                onClick={() => checkIn.mutate()}
              >
                <LogIn className="h-4 w-4" aria-hidden />
                Check in
              </Button>
              <Button
                variant="outline"
                className="flex-1"
                disabled={!today.data?.hasCheckedIn || today.data?.hasCheckedOut}
                loading={checkOut.isPending}
                onClick={() => checkOut.mutate()}
              >
                <LogOut className="h-4 w-4" aria-hidden />
                Check out
              </Button>
            </div>
          </CardBody>
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader><CardTitle>My month so far</CardTitle></CardHeader>
          <CardBody className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
            {[
              ['Present', summary.data?.presentDays],
              ['Late', summary.data?.lateDays],
              ['Half days', summary.data?.halfDays],
              ['Absent', summary.data?.absentDays],
            ].map(([label, value]) => (
              <div key={label as string}>
                <p className="text-xs text-ink-400">{label}</p>
                <p className="tabular mt-1 text-xl font-semibold text-ink">{(value as number) ?? 0}</p>
              </div>
            ))}
            <div>
              <p className="text-xs text-ink-400">Worked</p>
              <p className="tabular mt-1 text-xl font-semibold text-ink">
                {formatMinutes(summary.data?.totalWorkedMinutes ?? 0)}
              </p>
            </div>
            <div>
              <p className="text-xs text-ink-400">Overtime</p>
              <p className="tabular mt-1 text-xl font-semibold text-ink">
                {formatMinutes(summary.data?.totalOvertimeMinutes ?? 0)}
              </p>
            </div>
          </CardBody>
        </Card>
      </div>

      <PermissionGate permission={PERMISSIONS.attendanceView}>
        <Card>
          <CardHeader>
            <CardTitle>
              Company attendance
              {dailySummary.data && (
                <span className="ml-2 font-normal text-ink-400">
                  {dailySummary.data.present} present · {dailySummary.data.late} late ·{' '}
                  {dailySummary.data.halfDay} half day
                </span>
              )}
            </CardTitle>
            <Input
              type="date"
              className="w-44"
              value={date}
              onChange={(event) => setDate(event.target.value)}
              aria-label="Attendance date"
            />
          </CardHeader>

          {daily.isLoading ? (
            <TableSkeleton columns={6} />
          ) : daily.isError ? (
            <ErrorState message={getErrorMessage(daily.error)} onRetry={() => daily.refetch()} />
          ) : daily.data && daily.data.items.length === 0 ? (
            <EmptyState
              title="Nobody has checked in on this date"
              description="Pick another date, or add a manual entry from the employee's profile."
            />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-line bg-ink-50/60 text-xs font-medium text-ink-600">
                  <tr>
                    <th scope="col" className="px-5 py-3">Employee</th>
                    <th scope="col" className="px-5 py-3">In</th>
                    <th scope="col" className="px-5 py-3">Out</th>
                    <th scope="col" className="px-5 py-3">Worked</th>
                    <th scope="col" className="px-5 py-3">Overtime</th>
                    <th scope="col" className="px-5 py-3">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line">
                  {daily.data?.items.map((item) => (
                    <tr key={item.id} className="hover:bg-ink-50/50">
                      <td className="tabular px-5 py-3 text-ink-600">{item.employeeId.slice(0, 8)}</td>
                      <td className="tabular px-5 py-3 text-ink-600">{formatTime(item.checkInUtc)}</td>
                      <td className="tabular px-5 py-3 text-ink-600">{formatTime(item.checkOutUtc)}</td>
                      <td className="tabular px-5 py-3 text-ink-600">{formatMinutes(item.workedMinutes)}</td>
                      <td className="tabular px-5 py-3 text-ink-600">{formatMinutes(item.overtimeMinutes)}</td>
                      <td className="px-5 py-3"><AttendanceBadge status={item.status} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </PermissionGate>
    </div>
  );
}
