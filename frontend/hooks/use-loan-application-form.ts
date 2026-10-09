import { useState } from "react";
import { useRouter } from "next/navigation";
import { submitLoanApplication } from "@/lib/api/applications/applications-api";
import { SSN_ITIN_MESSAGE, validateForm, type FormErrors, type LoanApplicationFormData } from "@/lib/validation";
import { translations } from "@/lib/i18n/translations";

const initialFormData: LoanApplicationFormData = {
  firstName: "",
  lastName: "",
  street: "",
  city: "",
  state: "",
  zip: "",
  companyName: "",
  requestedAmount: 0,
  ssn: "",
};

export function useLoanApplicationForm() {
  const router = useRouter();
  const [formData, setFormData] = useState<LoanApplicationFormData>(initialFormData);
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function updateField<K extends keyof LoanApplicationFormData>(
    field: K,
    value: LoanApplicationFormData[K],
  ) {
    setFormData((prev) => ({ ...prev, [field]: value }));

    // Flag an SSN starting with 9 (ITIN range) as soon as it's typed, instead of on submit.
    if (field === "ssn") {
      const startsWithNine = String(value).startsWith("9");
      setErrors((prev) => {
        if (startsWithNine) return { ...prev, ssn: SSN_ITIN_MESSAGE };
        if (prev.ssn === SSN_ITIN_MESSAGE) return { ...prev, ssn: undefined };
        return prev;
      });
    }
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    const validationErrors = validateForm(formData);
    setErrors(validationErrors);
    setSubmitError(null);

    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    setIsSubmitting(true);
    try {
      const result = await submitLoanApplication(formData);

      if (result.status === "Approved" && result.applicationId) {
        const params = new URLSearchParams({
          applicationId: result.applicationId,
          firstName: formData.firstName,
          lastName: formData.lastName,
          requestedAmount: String(formData.requestedAmount),
        });
        router.push(`/success?${params.toString()}`);
        return;
      }

      const params = new URLSearchParams({ reason: result.reason ?? "" });
      router.push(`/denied?${params.toString()}`);
    } catch {
      setSubmitError(translations.loanApplicationForm.submitError);
      setIsSubmitting(false);
    }
  }

  return { formData, errors, submitError, isSubmitting, updateField, handleSubmit };
}
