"use client";

import { useLoanApplicationForm } from "@/hooks/use-loan-application-form";
import { FormField, inputClass } from "@/components/form-field";
import { formatSSN } from "@/lib/format";
import { US_STATES } from "@/lib/us-states";
import { translations } from "@/lib/i18n/translations";

export function LoanApplicationForm() {
  const { formData, errors, submitError, isSubmitting, updateField, handleSubmit } =
    useLoanApplicationForm();
  const t = translations.loanApplicationForm;

  return (
    <>
      <h1 className="text-2xl font-semibold mb-1">{t.title}</h1>
      <p className="text-sm text-neutral-500 mb-6">{t.subtitle}</p>

      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <div className="grid grid-cols-2 gap-4">
          <FormField label={t.fields.firstName} error={errors.firstName}>
            <input
              className={inputClass(errors.firstName)}
              value={formData.firstName}
              onChange={(e) => updateField("firstName", e.target.value)}
            />
          </FormField>
          <FormField label={t.fields.lastName} error={errors.lastName}>
            <input
              className={inputClass(errors.lastName)}
              value={formData.lastName}
              onChange={(e) => updateField("lastName", e.target.value)}
            />
          </FormField>
        </div>

        <FormField label={t.fields.street} error={errors.street}>
          <input
            className={inputClass(errors.street)}
            value={formData.street}
            onChange={(e) => updateField("street", e.target.value)}
          />
        </FormField>

        <div className="grid grid-cols-3 gap-4">
          <FormField label={t.fields.city} error={errors.city}>
            <input
              className={inputClass(errors.city)}
              value={formData.city}
              onChange={(e) => updateField("city", e.target.value)}
            />
          </FormField>
          <FormField label={t.fields.state} error={errors.state}>
            <select
              className={inputClass(errors.state)}
              value={formData.state}
              onChange={(e) => updateField("state", e.target.value)}
            >
              <option value="">{t.statePlaceholder}</option>
              {US_STATES.map((state) => (
                <option key={state.code} value={state.code}>
                  {state.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label={t.fields.zip} error={errors.zip}>
            <input
              className={inputClass(errors.zip)}
              placeholder="12345"
              value={formData.zip}
              onChange={(e) => updateField("zip", e.target.value)}
            />
          </FormField>
        </div>

        <FormField label={t.fields.companyName} error={errors.companyName}>
          <input
            className={inputClass(errors.companyName)}
            value={formData.companyName}
            onChange={(e) => updateField("companyName", e.target.value)}
          />
        </FormField>

        <div className="grid grid-cols-2 gap-4">
          <FormField label={t.fields.requestedAmount} error={errors.requestedAmount}>
            <input
              type="number"
              min="0"
              step="0.01"
              className={inputClass(errors.requestedAmount)}
              value={formData.requestedAmount || ""}
              onChange={(e) => updateField("requestedAmount", Number(e.target.value))}
            />
          </FormField>
          <FormField label={t.fields.ssn} error={errors.ssn}>
            <input
              className={inputClass(errors.ssn)}
              placeholder="123-45-6789"
              maxLength={11}
              value={formData.ssn}
              onChange={(e) => updateField("ssn", formatSSN(e.target.value))}
            />
          </FormField>
        </div>

        {submitError && (
          <p className="text-sm text-red-700 dark:text-red-300 bg-red-50 dark:bg-red-950/60 border border-red-200 dark:border-red-900 rounded-md p-3">
            {submitError}
          </p>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-neutral-900 dark:bg-neutral-100 text-white dark:text-neutral-900 py-2.5 font-medium disabled:opacity-60 disabled:cursor-not-allowed transition-opacity"
        >
          {isSubmitting ? t.submitting : t.submit}
        </button>
      </form>
    </>
  );
}
