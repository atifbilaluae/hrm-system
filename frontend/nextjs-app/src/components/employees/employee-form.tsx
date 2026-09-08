'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/card';
import { Field, Input, Select, Textarea } from '@/components/ui/input';
import { createEmployeeSchema, type CreateEmployeeInput } from '@/schemas/employee.schema';
import { employmentTypeLabels, genderLabels } from '@/lib/constants';

const departments = [
  { id: '11111111-1111-1111-1111-111111111111', name: 'Engineering' },
  { id: '22222222-2222-2222-2222-222222222222', name: 'Human Resources' },
  { id: '33333333-3333-3333-3333-333333333333', name: 'Finance' },
  { id: '44444444-4444-4444-4444-444444444444', name: 'Sales' },
];

export function EmployeeForm({
  onSubmit, submitting, submitLabel = 'Save employee',
}: {
  onSubmit: (values: CreateEmployeeInput) => void;
  submitting?: boolean;
  submitLabel?: string;
}) {
  const {
    register, handleSubmit, setValue, formState: { errors },
  } = useForm<CreateEmployeeInput>({
    resolver: zodResolver(createEmployeeSchema),
    defaultValues: { gender: 1, employmentType: 1, basicSalary: 0 },
  });

  return (
    <form className="space-y-4" onSubmit={handleSubmit(onSubmit)} noValidate>
      <Card>
        <CardHeader><CardTitle>Personal details</CardTitle></CardHeader>
        <CardBody className="grid gap-4 sm:grid-cols-2">
          <Field label="First name" required error={errors.firstName?.message}>
            <Input {...register('firstName')} />
          </Field>
          <Field label="Last name" required error={errors.lastName?.message}>
            <Input {...register('lastName')} />
          </Field>
          <Field label="Work email" required error={errors.email?.message}>
            <Input type="email" {...register('email')} />
          </Field>
          <Field label="Phone" error={errors.phone?.message}>
            <Input {...register('phone')} placeholder="+92 300 1234567" />
          </Field>
          <Field label="Date of birth" required error={errors.dateOfBirth?.message}>
            <Input type="date" {...register('dateOfBirth')} />
          </Field>
          <Field label="Gender" error={errors.gender?.message}>
            <Select {...register('gender')}>
              {Object.entries(genderLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </Select>
          </Field>
          <Field label="Nationality" error={errors.nationality?.message}>
            <Input {...register('nationality')} />
          </Field>
          <div className="sm:col-span-2">
            <Field label="Address" error={errors.address?.message}>
              <Textarea rows={2} {...register('address')} />
            </Field>
          </div>
        </CardBody>
      </Card>

      <Card>
        <CardHeader><CardTitle>Employment</CardTitle></CardHeader>
        <CardBody className="grid gap-4 sm:grid-cols-2">
          <Field label="Department" error={errors.departmentId?.message}>
            <Select
              onChange={(event) => {
                const dept = departments.find((d) => d.id === event.target.value);
                setValue('departmentId', dept?.id ?? '');
                setValue('departmentName', dept?.name ?? '');
              }}
              defaultValue=""
            >
              <option value="">Unassigned</option>
              {departments.map((dept) => (
                <option key={dept.id} value={dept.id}>{dept.name}</option>
              ))}
            </Select>
          </Field>
          <Field label="Designation" error={errors.designationName?.message}>
            <Input {...register('designationName')} placeholder="Software Engineer" />
          </Field>
          <Field label="Joining date" required error={errors.joiningDate?.message}>
            <Input type="date" {...register('joiningDate')} />
          </Field>
          <Field label="Employment type" error={errors.employmentType?.message}>
            <Select {...register('employmentType')}>
              {Object.entries(employmentTypeLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </Select>
          </Field>
          <Field label="Basic salary" required error={errors.basicSalary?.message} hint="Monthly gross before allowances">
            <Input type="number" step="1000" {...register('basicSalary')} />
          </Field>
        </CardBody>
      </Card>

      <Card>
        <CardHeader><CardTitle>Bank and emergency contact</CardTitle></CardHeader>
        <CardBody className="grid gap-4 sm:grid-cols-2">
          <Field label="Bank name" error={errors.bankName?.message}>
            <Input {...register('bankName')} />
          </Field>
          <Field label="Account number" error={errors.bankAccountNumber?.message}>
            <Input {...register('bankAccountNumber')} />
          </Field>
          <Field label="Emergency contact" error={errors.emergencyContactName?.message}>
            <Input {...register('emergencyContactName')} />
          </Field>
          <Field label="Emergency phone" error={errors.emergencyContactPhone?.message}>
            <Input {...register('emergencyContactPhone')} />
          </Field>
          <Field label="Relationship" error={errors.emergencyContactRelation?.message}>
            <Input {...register('emergencyContactRelation')} placeholder="Spouse, parent, sibling" />
          </Field>
        </CardBody>
      </Card>

      <div className="flex justify-end gap-2">
        <Button type="submit" variant="accent" loading={submitting}>{submitLabel}</Button>
      </div>
    </form>
  );
}
