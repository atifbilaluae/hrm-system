'use client';

import Link from 'next/link';
import { StatusBadge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { formatDate, initials } from '@/lib/utils';
import type { EmployeeListItem, PagedResult } from '@/types';

export function EmployeeTable({
  data, onPageChange,
}: { data: PagedResult<EmployeeListItem>; onPageChange: (page: number) => void }) {
  const start = (data.page - 1) * data.pageSize + 1;
  const end = Math.min(data.page * data.pageSize, data.totalCount);

  return (
    <>
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-line bg-ink-50/60 text-xs font-medium text-ink-600">
            <tr>
              <th scope="col" className="px-5 py-3">Employee</th>
              <th scope="col" className="px-5 py-3">Code</th>
              <th scope="col" className="px-5 py-3">Department</th>
              <th scope="col" className="px-5 py-3">Designation</th>
              <th scope="col" className="px-5 py-3">Joined</th>
              <th scope="col" className="px-5 py-3">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {data.items.map((employee) => (
              <tr key={employee.id} className="hover:bg-ink-50/50">
                <td className="px-5 py-3">
                  <Link href={`/employees/${employee.id}`} className="flex items-center gap-3">
                    <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-ink-100 text-xs font-semibold text-ink-600">
                      {initials(employee.fullName)}
                    </span>
                    <span>
                      <span className="block font-medium text-ink hover:text-accent">{employee.fullName}</span>
                      <span className="block text-xs text-ink-400">{employee.email}</span>
                    </span>
                  </Link>
                </td>
                <td className="tabular px-5 py-3 text-ink-600">{employee.employeeCode}</td>
                <td className="px-5 py-3 text-ink-600">{employee.departmentName ?? '—'}</td>
                <td className="px-5 py-3 text-ink-600">{employee.designationName ?? '—'}</td>
                <td className="tabular px-5 py-3 text-ink-600">{formatDate(employee.joiningDate)}</td>
                <td className="px-5 py-3"><StatusBadge status={employee.employmentStatus} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between border-t border-line px-5 py-3">
        <p className="tabular text-xs text-ink-400">
          {data.totalCount === 0 ? 'No results' : `${start}–${end} of ${data.totalCount}`}
        </p>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" disabled={data.page <= 1} onClick={() => onPageChange(data.page - 1)}>
            Previous
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={data.page >= data.totalPages}
            onClick={() => onPageChange(data.page + 1)}
          >
            Next
          </Button>
        </div>
      </div>
    </>
  );
}
