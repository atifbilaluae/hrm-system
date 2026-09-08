'use client';

import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowLeft } from 'lucide-react';
import { EmployeeForm } from '@/components/employees/employee-form';
import { useCreateEmployee } from '@/hooks/use-employees';

export default function NewEmployeePage() {
  const router = useRouter();
  const createEmployee = useCreateEmployee();

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <Link href="/employees" className="inline-flex items-center gap-1.5 text-sm text-ink-400 hover:text-ink">
        <ArrowLeft className="h-4 w-4" aria-hidden />
        Back to employees
      </Link>

      <div>
        <h1 className="text-lg font-semibold tracking-tight text-ink">Add employee</h1>
        <p className="text-sm text-ink-400">
          Saving publishes an <span className="font-medium text-ink-600">employee.created</span> event, so
          leave balances, payroll setup and manager notifications follow automatically.
        </p>
      </div>

      <EmployeeForm
        submitting={createEmployee.isPending}
        onSubmit={(values) =>
          createEmployee.mutate(values, {
            onSuccess: (employee) => router.push(`/employees/${employee.id}`),
          })
        }
      />
    </div>
  );
}
