import { z } from "zod";
import { apiRequest } from "@/lib/api/base-api";
import type { LoanApplicationFormData } from "@/lib/validation";

const submitLoanApplicationResponseSchema = z.object({
  status: z.enum(["Approved", "Denied"]),
  applicationId: z.string().nullable(),
  reason: z.string().nullable(),
});

export type SubmitLoanApplicationResponse = z.infer<typeof submitLoanApplicationResponseSchema>;

export function submitLoanApplication(
  data: LoanApplicationFormData,
): Promise<SubmitLoanApplicationResponse> {
  return apiRequest("/api/applications", submitLoanApplicationResponseSchema, {
    method: "POST",
    // The form displays the SSN with dashes; the API receives digits only.
    body: JSON.stringify({ ...data, ssn: data.ssn.replace(/\D/g, "") }),
  });
}
