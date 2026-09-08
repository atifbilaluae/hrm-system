'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { ArrowLeft, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/card';
import { StatusBadge } from '@/components/ui/badge';
import { ConfirmDialog } from '@/components/ui/dialog';
import { ErrorState, LoadingState } from '@/components/ui/states';
import { PermissionGate } from '@/components/permission-gate';
import { useDeleteEmployee, useEmployee } from '@/hooks/use-employees';
import { getErrorMessage } from '@/lib/api-client';
import { PERMISSIONS, employmentTypeLabels, genderLabels } from '@/lib/constants';
import { cn, formatCurrency, formatDate, initials } from '@/lib/utils';

const tabs = ['Overview', 'Personal', 'Employment', 'Attendance', 'Leaves', 'Payroll', 'Documents', 'Performance'] as const;
type Tab = (typeof tabs)[number];

function Detail({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs text-ink-400">{label}</dt>
      <dd className="mt-0.5 text-sm text-ink">{value || '—'}</dd>
    </div>
  );
}

export default function EmployeeDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [tab, setTab] = useState<Tab>('Overview');
  const [confirmOpen, setConfirmOpen] = useState(false);

  const { data: employee, isLoading, isError, error, refetch } = useEmployee(id);
  const deleteEmployee = useDeleteEmployee();

  if (isLoading) return <LoadingState label="Loading employee" />;
  if (isError) return <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />;
  if (!employee) return null;

  const fullName = `${employee.firstName} ${employee.lastName}`;

  return (
    <div className="space-y-4">
      <Link href="/employees" className="inline-flex items-center gap-1.5 text-sm text-ink-400 hover:text-ink">
        <ArrowLeft className="h-4 w-4" aria-hidden />
        Back to employees
      </Link>

      <Card>
        <CardBody className="flex flex-wrap items-center gap-4">
          <span className="flex h-14 w-14 items-center justify-center rounded-full bg-ink text-lg font-semibold text-white">
            {initials(fullName)}
          </span>
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-lg font-semibold tracking-tight text-ink">{fullName}</h1>
              <StatusBadge status={employee.employmentStatus} />
            </div>
            <p className="text-sm text-ink-400">
              {employee.designationName ?? 'No designation'} · {employee.departmentName ?? 'Unassigned'} ·{' '}
              <span className="tabular">{employee.employeeCode}</span>
            </p>
          </div>
          <PermissionGate permission={PERMISSIONS.employeeDelete}>
            <Button variant="outline" onClick={() => setConfirmOpen(true)}>
              <Trash2 className="h-4 w-4" aria-hidden />
              Remove
            </Button>
          </PermissionGate>
        </CardBody>
      </Card>

      <div className="flex gap-1 overflow-x-auto border-b border-line">
        {tabs.map((item) => (
          <button
            key={item}
            onClick={() => setTab(item)}
            className={cn(
              'whitespace-nowrap border-b-2 px-3 py-2 text-sm',
              tab === item ? 'border-accent font-medium text-ink' : 'border-transparent text-ink-400 hover:text-ink',
            )}
          >
            {item}
          </button>
        ))}
      </div>

      {tab === 'Overview' && (
        <div className="grid gap-4 lg:grid-cols-2">
          <Card>
            <CardHeader><CardTitle>Contact</CardTitle></CardHeader>
            <CardBody>
              <dl className="grid gap-4 sm:grid-cols-2">
                <Detail label="Email" value={employee.email} />
                <Detail label="Phone" value={employee.phone} />
                <Detail label="Address" value={employee.address} />
                <Detail label="Nationality" value={employee.nationality} />
              </dl>
            </CardBody>
          </Card>
          <Card>
            <CardHeader><CardTitle>Employment</CardTitle></CardHeader>
            <CardBody>
              <dl className="grid gap-4 sm:grid-cols-2">
                <Detail label="Joined" value={formatDate(employee.joiningDate)} />
                <Detail label="Type" value={employmentTypeLabels[employee.employmentType]} />
                <Detail label="Department" value={employee.departmentName} />
                <Detail label="Designation" value={employee.designationName} />
              </dl>
            </CardBody>
          </Card>
        </div>
      )}

      {tab === 'Personal' && (
        <Card>
          <CardBody>
            <dl className="grid gap-4 sm:grid-cols-3">
              <Detail label="Date of birth" value={formatDate(employee.dateOfBirth)} />
              <Detail label="Gender" value={genderLabels[employee.gender]} />
              <Detail label="Nationality" value={employee.nationality} />
              <Detail label="Emergency contact" value={employee.emergencyContactName} />
              <Detail label="Emergency phone" value={employee.emergencyContactPhone} />
              <Detail label="Relationship" value={employee.emergencyContactRelation} />
            </dl>
          </CardBody>
        </Card>
      )}

      {tab === 'Employment' && (
        <Card>
          <CardBody>
            <dl className="grid gap-4 sm:grid-cols-3">
              <Detail label="Employee code" value={<span className="tabular">{employee.employeeCode}</span>} />
              <Detail label="Joined" value={formatDate(employee.joiningDate)} />
              <Detail label="Termination date" value={formatDate(employee.terminationDate)} />
              <PermissionGate permission={PERMISSIONS.payrollView}>
                <Detail label="Basic salary" value={formatCurrency(employee.basicSalary)} />
              </PermissionGate>
              <Detail label="Bank" value={employee.bankName} />
              <Detail label="Account" value={employee.bankAccountNumber} />
            </dl>
          </CardBody>
        </Card>
      )}

      {['Attendance', 'Leaves', 'Payroll', 'Documents', 'Performance'].includes(tab) && (
        <Card>
          <CardBody className="py-14 text-center">
            <p className="text-sm font-medium text-ink">{tab} arrives with its own service</p>
            <p className="mx-auto mt-1 max-w-md text-sm text-ink-400">
              This tab reads from the {tab.toLowerCase()} microservice through the same gateway.
              The service is scheduled in a later phase of the roadmap.
            </p>
          </CardBody>
        </Card>
      )}

      <ConfirmDialog
        open={confirmOpen}
        onClose={() => setConfirmOpen(false)}
        title={`Remove ${fullName}?`}
        description="The record is archived, not erased. HR keeps it for audit and legal retention."
        confirmLabel="Remove employee"
        loading={deleteEmployee.isPending}
        onConfirm={() =>
          deleteEmployee.mutate(employee.id, {
            onSuccess: () => {
              setConfirmOpen(false);
              router.push('/employees');
            },
          })
        }
      />
    </div>
  );
}
