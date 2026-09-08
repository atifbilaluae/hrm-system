'use client';

import Link from 'next/link';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/card';
import { ErrorState, LoadingState } from '@/components/ui/states';
import { useEmployeeStats } from '@/hooks/use-employees';
import { useCurrentUser } from '@/hooks/use-auth';
import { getErrorMessage } from '@/lib/api-client';
import { formatDate } from '@/lib/utils';

function Metric({ label, value, sublabel }: { label: string; value: number | string; sublabel?: string }) {
  return (
    <Card>
      <CardBody>
        <p className="text-sm text-ink-400">{label}</p>
        <p className="tabular mt-2 text-data-lg font-semibold text-ink">{value}</p>
        {sublabel && <p className="mt-1 text-xs text-ink-400">{sublabel}</p>}
      </CardBody>
    </Card>
  );
}

export default function DashboardPage() {
  const { data: user } = useCurrentUser();
  const { data: stats, isLoading, isError, error, refetch } = useEmployeeStats();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight text-ink">
          {user?.fullName ? `Welcome back, ${user.fullName.split(' ')[0]}` : 'Dashboard'}
        </h1>
        <p className="text-sm text-ink-400">Here is where your organization stands today.</p>
      </div>

      {isLoading && <LoadingState label="Loading dashboard" />}
      {isError && <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />}

      {stats && (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Metric label="Total employees" value={stats.totalEmployees} />
            <Metric label="Active" value={stats.activeEmployees} sublabel={`${stats.probation} on probation`} />
            <Metric label="Joined this month" value={stats.newThisMonth} />
            <Metric label="On leave today" value={stats.onLeave} />
          </div>

          <div className="grid gap-4 lg:grid-cols-3">
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle>Headcount by department</CardTitle>
              </CardHeader>
              <CardBody>
                <div className="h-72">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={stats.headcountByDepartment} margin={{ top: 8, right: 8, bottom: 8, left: -16 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#e4e7ec" vertical={false} />
                      <XAxis dataKey="department" tick={{ fontSize: 12, fill: '#6e7f9d' }} tickLine={false} axisLine={false} />
                      <YAxis allowDecimals={false} tick={{ fontSize: 12, fill: '#6e7f9d' }} tickLine={false} axisLine={false} />
                      <Tooltip cursor={{ fill: '#f4f6fa' }} />
                      <Bar dataKey="count" fill="#0f766e" radius={[4, 4, 0, 0]} maxBarSize={48} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </CardBody>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Birthdays in the next 30 days</CardTitle>
              </CardHeader>
              <CardBody className="p-0">
                {stats.upcomingBirthdays.length === 0 ? (
                  <p className="px-5 py-8 text-center text-sm text-ink-400">No birthdays coming up.</p>
                ) : (
                  <ul className="divide-y divide-line">
                    {stats.upcomingBirthdays.map((person) => (
                      <li key={person.employeeId} className="flex items-center justify-between px-5 py-3">
                        <Link href={`/employees/${person.employeeId}`} className="text-sm text-ink hover:text-accent">
                          {person.fullName}
                        </Link>
                        <span className="text-xs text-ink-400">
                          {person.daysAway === 0 ? 'Today' : `in ${person.daysAway} days`}
                          <span className="ml-2">{formatDate(person.dateOfBirth, 'dd MMM')}</span>
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </CardBody>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}
