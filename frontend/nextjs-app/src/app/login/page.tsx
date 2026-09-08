'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/input';
import { useLogin } from '@/hooks/use-auth';
import { getErrorMessage } from '@/lib/api-client';
import { loginSchema, type LoginInput } from '@/schemas/auth.schema';

export default function LoginPage() {
  const login = useLogin();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginInput>({ resolver: zodResolver(loginSchema) });

  return (
    <main className="grid min-h-screen lg:grid-cols-2">
      <section className="hidden flex-col justify-between bg-ink p-12 text-white lg:flex">
        <span className="flex items-center gap-2">
          <span className="flex h-7 w-7 items-center justify-center rounded bg-accent text-xs font-bold">HR</span>
          <span className="text-sm font-semibold">Workforce</span>
        </span>
        <div className="max-w-md">
          <p className="text-3xl font-semibold leading-tight tracking-tight">
            Every hire, hour and payslip in one place.
          </p>
          <p className="mt-4 text-sm leading-relaxed text-ink-200">
            Attendance, leave, payroll and recruitment stay in sync because each
            module publishes what it changes and the rest of the system listens.
          </p>
        </div>
        <p className="text-xs text-ink-400">© {new Date().getFullYear()} Workforce HRM</p>
      </section>

      <section className="flex items-center justify-center px-6 py-16">
        <div className="w-full max-w-sm">
          <h1 className="text-xl font-semibold tracking-tight text-ink">Sign in</h1>
          <p className="mt-1 text-sm text-ink-400">Use the work account your HR team issued.</p>

          <form
            className="mt-8 space-y-4"
            onSubmit={handleSubmit((values) => login.mutate(values))}
            noValidate
          >
            <Field label="Work email" required error={errors.email?.message}>
              <Input type="email" autoComplete="email" placeholder="you@company.com" {...register('email')} />
            </Field>

            <Field label="Password" required error={errors.password?.message}>
              <Input type="password" autoComplete="current-password" {...register('password')} />
            </Field>

            {login.isError && (
              <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
                {getErrorMessage(login.error, 'Sign in failed. Check your email and password.')}
              </p>
            )}

            <Button type="submit" className="w-full" loading={login.isPending}>
              Sign in
            </Button>
          </form>

          <p className="mt-8 rounded-md border border-line bg-white px-3 py-2 text-xs text-ink-400">
            Demo account: <span className="font-medium text-ink-600">admin@hrm.local</span> / Admin@12345
          </p>
        </div>
      </section>
    </main>
  );
}
