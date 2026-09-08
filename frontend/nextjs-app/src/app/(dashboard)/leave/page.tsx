'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';
import { Field, Input, Select, Textarea } from '@/components/ui/input';
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/states';
import { PermissionGate } from '@/components/permission-gate';
import {
  useCancelLeave, useDecideLeave, useLeaveTypes, useMyLeaveBalances,
  useMyLeaveRequests, usePendingLeaveRequests,
} from '@/hooks/use-leave';
import { useSubmitLeave } from '@/hooks/use-leave';
import { getErrorMessage } from '@/lib/api-client';
import { PERMISSIONS, leaveStatusLabels, leaveStatusStyles } from '@/lib/constants';
import { cn, formatDate } from '@/lib/utils';
import { leaveRequestSchema, type LeaveRequestInput } from '@/schemas/leave.schema';
import { LeaveRequestStatus } from '@/types/leave';

function LeaveBadge({ status }: { status: number }) {
  return (
    <span className={cn('inline-flex rounded px-2 py-0.5 text-xs font-medium',
      leaveStatusStyles[status] ?? 'bg-ink-50 text-ink-600')}>
      {leaveStatusLabels[status] ?? 'Unknown'}
    </span>
  );
}

export default function LeavePage() {
  const [dialogOpen, setDialogOpen] = useState(false);

  const balances = useMyLeaveBalances();
  const myRequests = useMyLeaveRequests();
  const pending = usePendingLeaveRequests();
  const types = useLeaveTypes();
  const submit = useSubmitLeave();
  const decide = useDecideLeave();
  const cancel = useCancelLeave();

  const {
    register, handleSubmit, reset, formState: { errors },
  } = useForm<LeaveRequestInput>({ resolver: zodResolver(leaveRequestSchema) });

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-lg font-semibold tracking-tight text-ink">Leave</h1>
          <p className="text-sm text-ink-400">
            Weekends are excluded automatically, and requested days are held against your balance
            until your manager decides.
          </p>
        </div>
        <PermissionGate permission={PERMISSIONS.leaveRequest}>
          <Button variant="accent" onClick={() => setDialogOpen(true)}>
            <Plus className="h-4 w-4" aria-hidden />
            Request leave
          </Button>
        </PermissionGate>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {balances.isLoading && <LoadingState label="Loading balances" />}
        {balances.data?.map((balance) => (
          <Card key={balance.leaveTypeId}>
            <CardBody>
              <p className="text-sm text-ink-400">{balance.leaveTypeName}</p>
              <p className="tabular mt-2 text-data-lg font-semibold text-ink">
                {balance.availableDays}
                <span className="ml-1 text-sm font-normal text-ink-400">/ {balance.entitledDays} days</span>
              </p>
              <p className="mt-1 text-xs text-ink-400">
                {balance.usedDays} used · {balance.pendingDays} pending
              </p>
            </CardBody>
          </Card>
        ))}
      </div>

      <PermissionGate permission={PERMISSIONS.leaveApprove}>
        <Card>
          <CardHeader><CardTitle>Waiting for your decision</CardTitle></CardHeader>
          <CardBody className="p-0">
            {pending.isLoading ? (
              <LoadingState />
            ) : pending.data && pending.data.items.length === 0 ? (
              <EmptyState title="Nothing to approve" description="New requests appear here as soon as they are submitted." />
            ) : (
              <ul className="divide-y divide-line">
                {pending.data?.items.map((request) => (
                  <li key={request.id} className="flex flex-wrap items-center gap-3 px-5 py-4">
                    <div className="min-w-0 flex-1">
                      <p className="text-sm font-medium text-ink">
                        {request.leaveTypeName} · {request.totalDays} day(s)
                      </p>
                      <p className="tabular text-xs text-ink-400">
                        {formatDate(request.fromDate)} → {formatDate(request.toDate)}
                      </p>
                      <p className="mt-1 text-sm text-ink-600">{request.reason}</p>
                    </div>
                    <div className="flex gap-2">
                      <Button
                        variant="accent" size="sm" loading={decide.isPending}
                        onClick={() => decide.mutate({ id: request.id, approve: true })}
                      >
                        Approve
                      </Button>
                      <Button
                        variant="outline" size="sm"
                        onClick={() => decide.mutate({ id: request.id, approve: false, comments: 'Not approved' })}
                      >
                        Reject
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </PermissionGate>

      <Card>
        <CardHeader><CardTitle>My requests</CardTitle></CardHeader>
        <CardBody className="p-0">
          {myRequests.isLoading ? (
            <LoadingState />
          ) : myRequests.isError ? (
            <ErrorState message={getErrorMessage(myRequests.error)} onRetry={() => myRequests.refetch()} />
          ) : myRequests.data && myRequests.data.items.length === 0 ? (
            <EmptyState
              title="No leave requested yet"
              description="Your submitted requests and their decisions will be listed here."
            />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-line bg-ink-50/60 text-xs font-medium text-ink-600">
                  <tr>
                    <th scope="col" className="px-5 py-3">Type</th>
                    <th scope="col" className="px-5 py-3">Dates</th>
                    <th scope="col" className="px-5 py-3">Days</th>
                    <th scope="col" className="px-5 py-3">Status</th>
                    <th scope="col" className="px-5 py-3" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-line">
                  {myRequests.data?.items.map((request) => (
                    <tr key={request.id} className="hover:bg-ink-50/50">
                      <td className="px-5 py-3 font-medium text-ink">{request.leaveTypeName}</td>
                      <td className="tabular px-5 py-3 text-ink-600">
                        {formatDate(request.fromDate)} → {formatDate(request.toDate)}
                      </td>
                      <td className="tabular px-5 py-3 text-ink-600">{request.totalDays}</td>
                      <td className="px-5 py-3"><LeaveBadge status={request.status} /></td>
                      <td className="px-5 py-3 text-right">
                        {request.status === LeaveRequestStatus.Pending && (
                          <button
                            className="text-sm text-ink-400 hover:text-ink"
                            onClick={() => cancel.mutate(request.id)}
                          >
                            Cancel
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardBody>
      </Card>

      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title="Request leave"
        description="Your balance updates the moment this is submitted."
        footer={
          <>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button
              variant="accent"
              loading={submit.isPending}
              onClick={handleSubmit((values) =>
                submit.mutate(values, {
                  onSuccess: () => {
                    setDialogOpen(false);
                    reset();
                  },
                }),
              )}
            >
              Submit request
            </Button>
          </>
        }
      >
        <div className="space-y-3">
          <Field label="Leave type" required error={errors.leaveTypeId?.message}>
            <Select {...register('leaveTypeId')} defaultValue="">
              <option value="" disabled>Choose a type</option>
              {types.data?.map((type) => (
                <option key={type.id} value={type.id}>
                  {type.name} — {type.annualQuotaDays} days a year
                </option>
              ))}
            </Select>
          </Field>
          <div className="grid gap-3 sm:grid-cols-2">
            <Field label="From" required error={errors.fromDate?.message}>
              <Input type="date" {...register('fromDate')} />
            </Field>
            <Field label="To" required error={errors.toDate?.message}>
              <Input type="date" {...register('toDate')} />
            </Field>
          </div>
          <Field label="Reason" required error={errors.reason?.message}>
            <Textarea rows={3} {...register('reason')} placeholder="Family commitment, medical appointment, travel" />
          </Field>
        </div>
      </Dialog>
    </div>
  );
}
