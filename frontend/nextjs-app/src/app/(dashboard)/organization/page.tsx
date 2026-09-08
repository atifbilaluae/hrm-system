'use client';

import { useState } from 'react';
import { ChevronRight, Plus } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';
import { Field, Input } from '@/components/ui/input';
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/states';
import { PermissionGate } from '@/components/permission-gate';
import {
  useBranches, useCreateDepartment, useDepartmentTree, useDepartments,
  useDesignations, useHolidays,
} from '@/hooks/use-organization';
import { getErrorMessage } from '@/lib/api-client';
import { PERMISSIONS } from '@/lib/constants';
import { cn, formatDate } from '@/lib/utils';
import type { DepartmentTreeNode } from '@/types/organization';

const tabs = ['Departments', 'Org chart', 'Designations', 'Branches', 'Holidays'] as const;
type Tab = (typeof tabs)[number];

function TreeBranch({ node, depth = 0 }: { node: DepartmentTreeNode; depth?: number }) {
  return (
    <li>
      <div
        className="flex items-center gap-2 border-l border-line py-1.5 pl-3 text-sm"
        style={{ marginLeft: depth * 16 }}
      >
        {node.children.length > 0 && <ChevronRight className="h-3.5 w-3.5 text-ink-400" aria-hidden />}
        <span className="font-medium text-ink">{node.name}</span>
        <span className="tabular text-xs text-ink-400">{node.code}</span>
      </div>
      {node.children.length > 0 && (
        <ul>
          {node.children.map((child) => (
            <TreeBranch key={child.id} node={child} depth={depth + 1} />
          ))}
        </ul>
      )}
    </li>
  );
}

export default function OrganizationPage() {
  const [tab, setTab] = useState<Tab>('Departments');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState({ name: '', code: '', parentDepartmentId: '', branchId: '' });

  const departments = useDepartments();
  const tree = useDepartmentTree();
  const designations = useDesignations();
  const branches = useBranches();
  const holidays = useHolidays();
  const createDepartment = useCreateDepartment();

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-lg font-semibold tracking-tight text-ink">Organization</h1>
          <p className="text-sm text-ink-400">
            Departments, job titles, offices and the company calendar.
          </p>
        </div>
        <PermissionGate permission={PERMISSIONS.organizationManage}>
          <Button variant="accent" onClick={() => setDialogOpen(true)}>
            <Plus className="h-4 w-4" aria-hidden />
            New department
          </Button>
        </PermissionGate>
      </div>

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

      {tab === 'Departments' && (
        <Card>
          {departments.isLoading ? (
            <LoadingState />
          ) : departments.isError ? (
            <ErrorState message={getErrorMessage(departments.error)} onRetry={() => departments.refetch()} />
          ) : departments.data?.length === 0 ? (
            <EmptyState title="No departments yet" description="Create the first department to start the hierarchy." />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-line bg-ink-50/60 text-xs font-medium text-ink-600">
                  <tr>
                    <th scope="col" className="px-5 py-3">Department</th>
                    <th scope="col" className="px-5 py-3">Code</th>
                    <th scope="col" className="px-5 py-3">Reports into</th>
                    <th scope="col" className="px-5 py-3">Branch</th>
                    <th scope="col" className="px-5 py-3">Job titles</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line">
                  {departments.data?.map((department) => (
                    <tr key={department.id} className="hover:bg-ink-50/50">
                      <td className="px-5 py-3 font-medium text-ink">{department.name}</td>
                      <td className="tabular px-5 py-3 text-ink-600">{department.code}</td>
                      <td className="px-5 py-3 text-ink-600">{department.parentDepartmentName ?? '—'}</td>
                      <td className="px-5 py-3 text-ink-600">{department.branchName ?? '—'}</td>
                      <td className="tabular px-5 py-3 text-ink-600">{department.designationCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      )}

      {tab === 'Org chart' && (
        <Card>
          <CardHeader><CardTitle>Reporting hierarchy</CardTitle></CardHeader>
          <CardBody>
            {tree.isLoading ? (
              <LoadingState />
            ) : tree.data?.length === 0 ? (
              <EmptyState title="Nothing to chart yet" description="Departments with a parent appear here as a tree." />
            ) : (
              <ul className="space-y-0.5">
                {tree.data?.map((node) => <TreeBranch key={node.id} node={node} />)}
              </ul>
            )}
          </CardBody>
        </Card>
      )}

      {tab === 'Designations' && (
        <Card>
          {designations.isLoading ? (
            <LoadingState />
          ) : (
            <ul className="divide-y divide-line">
              {designations.data?.map((designation) => (
                <li key={designation.id} className="flex items-center justify-between px-5 py-3">
                  <div>
                    <p className="text-sm font-medium text-ink">{designation.title}</p>
                    <p className="text-xs text-ink-400">{designation.departmentName ?? 'Company wide'}</p>
                  </div>
                  <Badge>Level {designation.level}</Badge>
                </li>
              ))}
            </ul>
          )}
        </Card>
      )}

      {tab === 'Branches' && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {branches.data?.map((branch) => (
            <Card key={branch.id}>
              <CardBody>
                <div className="flex items-start justify-between">
                  <p className="text-sm font-medium text-ink">{branch.name}</p>
                  {branch.isHeadOffice && <Badge className="bg-accent-soft text-accent-strong">Head office</Badge>}
                </div>
                <p className="mt-1 text-sm text-ink-400">{branch.city}, {branch.country}</p>
                <p className="mt-3 text-xs text-ink-400">
                  {branch.departmentCount} department(s) · {branch.timeZoneId}
                </p>
              </CardBody>
            </Card>
          ))}
        </div>
      )}

      {tab === 'Holidays' && (
        <Card>
          <CardHeader><CardTitle>Company calendar {new Date().getFullYear()}</CardTitle></CardHeader>
          <CardBody className="p-0">
            <ul className="divide-y divide-line">
              {holidays.data?.map((holiday) => (
                <li key={holiday.id} className="flex items-center justify-between px-5 py-3">
                  <span className="text-sm text-ink">{holiday.name}</span>
                  <span className="tabular text-sm text-ink-400">{formatDate(holiday.date)}</span>
                </li>
              ))}
            </ul>
          </CardBody>
        </Card>
      )}

      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title="New department"
        description="Leave the parent empty for a top-level department."
        footer={
          <>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button
              variant="accent"
              loading={createDepartment.isPending}
              onClick={() =>
                createDepartment.mutate(
                  {
                    name: form.name,
                    code: form.code.toUpperCase(),
                    parentDepartmentId: form.parentDepartmentId || null,
                    branchId: form.branchId || null,
                    isActive: true,
                  },
                  {
                    onSuccess: () => {
                      setDialogOpen(false);
                      setForm({ name: '', code: '', parentDepartmentId: '', branchId: '' });
                    },
                  },
                )
              }
            >
              Create department
            </Button>
          </>
        }
      >
        <div className="space-y-3">
          <Field label="Name" required>
            <Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </Field>
          <Field label="Code" required hint="Uppercase, no spaces — for example ENG or FIN">
            <Input value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} />
          </Field>
        </div>
      </Dialog>
    </div>
  );
}
