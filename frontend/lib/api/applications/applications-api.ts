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
    body: JSON.stringify(data),
  });
}
