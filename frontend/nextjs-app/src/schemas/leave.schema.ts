import { z } from 'zod';

export const leaveRequestSchema = z
  .object({
    leaveTypeId: z.string().min(1, 'Pick a leave type'),
    fromDate: z.string().min(1, 'Pick a start date'),
    toDate: z.string().min(1, 'Pick an end date'),
    reason: z.string().min(5, 'Give your manager a little context').max(500),
  })
  .refine((data) => new Date(data.toDate) >= new Date(data.fromDate), {
    message: 'The end date must be on or after the start date',
    path: ['toDate'],
  });

export type LeaveRequestInput = z.infer<typeof leaveRequestSchema>;

export const correctionSchema = z.object({
  workDate: z.string().min(1, 'Pick the date to correct'),
  checkIn: z.string().optional().or(z.literal('')),
  checkOut: z.string().optional().or(z.literal('')),
  reason: z.string().min(5, 'Explain what went wrong').max(500),
});

export type CorrectionInput = z.infer<typeof correctionSchema>;
