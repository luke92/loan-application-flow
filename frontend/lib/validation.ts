import { z } from "zod";
import { translations } from "@/lib/i18n/translations";

const { validation: messages } = translations.loanApplicationForm;

const SSN_REGEX = /^\d{3}-\d{2}-\d{4}$/;
const STATE_REGEX = /^[A-Za-z]{2}$/;
const ZIP_REGEX = /^\d{5}(-\d{4})?$/;

export const loanApplicationSchema = z.object({
  firstName: z.string().trim().min(1, messages.firstNameRequired),
  lastName: z.string().trim().min(1, messages.lastNameRequired),
  street: z.string().trim().min(1, messages.streetRequired),
  city: z.string().trim().min(1, messages.cityRequired),
  state: z
    .string()
    .trim()
    .min(1, messages.stateRequired)
    .regex(STATE_REGEX, messages.stateInvalid),
  zip: z
    .string()
    .trim()
    .min(1, messages.zipRequired)
    .regex(ZIP_REGEX, messages.zipInvalid),
  companyName: z.string().trim().min(1, messages.companyNameRequired),
  requestedAmount: z
    .number({ error: messages.requestedAmountInvalid })
    .refine((value) => value > 0, messages.requestedAmountInvalid),
  ssn: z
    .string()
    .trim()
    .min(1, messages.ssnRequired)
    .regex(SSN_REGEX, messages.ssnInvalid),
});

export type LoanApplicationFormData = z.infer<typeof loanApplicationSchema>;

export type FormErrors = Partial<Record<keyof LoanApplicationFormData, string>>;

export function validateForm(data: LoanApplicationFormData): FormErrors {
  const result = loanApplicationSchema.safeParse(data);
  if (result.success) return {};

  const errors: FormErrors = {};
  for (const issue of result.error.issues) {
    const field = issue.path[0] as keyof LoanApplicationFormData | undefined;
    if (field && !errors[field]) {
      errors[field] = issue.message;
    }
  }
  return errors;
}
