'use client';

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { Plus, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardBody } from '@/components/ui/card';
import { Input, Select } from '@/components/ui/input';
import { EmptyState, ErrorState, TableSkeleton } from '@/components/ui/states';
import { EmployeeTable } from '@/components/employees/employee-table';
import { PermissionGate } from '@/components/permission-gate';
import { useEmployees } from '@/hooks/use-employees';
import { getErrorMessage } from '@/lib/api-client';
import { PERMISSIONS, employmentStatusLabels } from '@/lib/constants';

export default function EmployeesPage() {
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<string>('');
  const [page, setPage] = useState(1);

  // Debounce so a 12-character search does not fire 12 requests.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput);
      setPage(1);
    }, 350);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const params = useMemo(
    () => ({
      search: search || undefined,
      status: status ? Number(status) : undefined,
      page,
      pageSize: 10,
    }),
    [search, status, page],
  );

  const { data, isLoading, isError, error, refetch, isFetching } = useEmployees(params);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-lg font-semibold tracking-tight text-ink">Employees</h1>
          <p className="text-sm text-ink-400">Everyone on the payroll, past and present.</p>
        </div>
        <PermissionGate permission={PERMISSIONS.employeeCreate}>
          <Link
            href="/employees/new"
            className="inline-flex h-9 items-center gap-2 rounded-md bg-accent px-4 text-sm font-medium text-white hover:bg-accent-strong"
          >
            <Plus className="h-4 w-4" aria-hidden />
            Add employee
          </Link>
        </PermissionGate>
      </div>

      <Card>
        <CardBody className="flex flex-wrap gap-3 border-b border-line">
          <div className="relative min-w-[220px] flex-1">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400" aria-hidden />
            <Input
              className="pl-9"
              placeholder="Search by name, email or code"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              aria-label="Search employees"
            />
          </div>
          <Select
            className="w-44"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value);
              setPage(1);
            }}
            aria-label="Filter by status"
          >
            <option value="">All statuses</option>
            {Object.entries(employmentStatusLabels).map(([value, label]) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </Select>
        </CardBody>

        {isLoading ? (
          <TableSkeleton columns={6} />
        ) : isError ? (
          <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />
        ) : data && data.items.length === 0 ? (
          <EmptyState
            title="No employees match this view"
            description="Clear the filters, or add the first employee to get started."
            action={
              <PermissionGate permission={PERMISSIONS.employeeCreate}>
                <Button variant="accent" onClick={() => { setSearchInput(''); setStatus(''); }}>
                  Clear filters
                </Button>
              </PermissionGate>
            }
          />
        ) : (
          data && (
            <div className={isFetching ? 'opacity-70 transition-opacity' : undefined}>
              <EmployeeTable data={data} onPageChange={setPage} />
            </div>
          )
        )}
      </Card>
    </div>
  );
}
