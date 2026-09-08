import { z } from 'zod';

/**
 * Mirrors the FluentValidation rules on the server. Client validation is for fast
 * feedback only - the API stays the source of truth and validates again.
 */
export const createEmployeeSchema = z.object({
  firstName: z.string().min(1, 'First name is required').max(100),
  lastName: z.string().min(1, 'Last name is required').max(100),
  email: z.string().email('Enter a valid email address'),
  phone: z.string().max(30).optional().or(z.literal('')),
  dateOfBirth: z.string().min(1, 'Date of birth is required').refine((value) => {
    const dob = new Date(value);
    const sixteenYearsAgo = new Date();
    sixteenYearsAgo.setFullYear(sixteenYearsAgo.getFullYear() - 16);
    return dob <= sixteenYearsAgo;
  }, 'Employee must be at least 16 years old'),
  gender: z.coerce.number().int().min(1).max(3),
  address: z.string().max(300).optional().or(z.literal('')),
  nationality: z.string().max(100).optional().or(z.literal('')),
  departmentId: z.string().uuid().optional().or(z.literal('')),
  departmentName: z.string().max(150).optional().or(z.literal('')),
  designationId: z.string().uuid().optional().or(z.literal('')),
  designationName: z.string().max(150).optional().or(z.literal('')),
  managerId: z.string().uuid().optional().or(z.literal('')),
  joiningDate: z.string().min(1, 'Joining date is required'),
  employmentType: z.coerce.number().int().min(1).max(5),
  basicSalary: z.coerce.number().positive('Basic salary must be greater than zero'),
  bankName: z.string().max(150).optional().or(z.literal('')),
  bankAccountNumber: z.string().max(50).optional().or(z.literal('')),
  emergencyContactName: z.string().max(150).optional().or(z.literal('')),
  emergencyContactPhone: z.string().max(30).optional().or(z.literal('')),
  emergencyContactRelation: z.string().max(50).optional().or(z.literal('')),
});

export type CreateEmployeeInput = z.infer<typeof createEmployeeSchema>;

export const updateEmployeeSchema = createEmployeeSchema
  .omit({ email: true, joiningDate: true })
  .extend({ employmentStatus: z.coerce.number().int().min(1).max(6) });

export type UpdateEmployeeInput = z.infer<typeof updateEmployeeSchema>;
