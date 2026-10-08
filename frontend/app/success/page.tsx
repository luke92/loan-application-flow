import Link from "next/link";
import { firstValue, type SearchParams } from "@/lib/search-params";
import { translations } from "@/lib/i18n/translations";

export default async function SuccessPage({
  searchParams,
}: {
  searchParams: SearchParams;
}) {
  const t = translations.successPage;
  const params = await searchParams;
  const applicationId = firstValue(params.applicationId);
  const firstName = firstValue(params.firstName);
  const lastName = firstValue(params.lastName);
  const requestedAmount = firstValue(params.requestedAmount);
  const thanks = t.thanks.replace("{name}", firstName ? `, ${firstName}` : "");

  return (
    <main className="flex-1 flex items-center justify-center p-6">
      <div className="w-full max-w-md bg-white dark:bg-neutral-900 rounded-xl shadow-sm border border-neutral-200 dark:border-neutral-800 p-8 text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-green-100 dark:bg-green-900/40 text-green-600 dark:text-green-400 text-2xl">
          ✓
        </div>
        <h1 className="text-2xl font-semibold mb-2">{t.heading}</h1>
        <p className="text-sm text-neutral-500 mb-6">{thanks}</p>

        <dl className="text-left text-sm space-y-2 mb-6 bg-neutral-50 dark:bg-neutral-950 rounded-md p-4 border border-neutral-200 dark:border-neutral-800">
          {applicationId && (
            <div className="flex justify-between gap-4">
              <dt className="text-neutral-500">{t.applicationIdLabel}</dt>
              <dd className="font-mono text-xs text-right break-all">{applicationId}</dd>
            </div>
          )}
          {(firstName || lastName) && (
            <div className="flex justify-between gap-4">
              <dt className="text-neutral-500">{t.applicantLabel}</dt>
              <dd>{`${firstName} ${lastName}`.trim()}</dd>
            </div>
          )}
          {requestedAmount && (
            <div className="flex justify-between gap-4">
              <dt className="text-neutral-500">{t.requestedAmountLabel}</dt>
              <dd>${Number(requestedAmount).toLocaleString()}</dd>
            </div>
          )}
        </dl>

        <Link
          href="/"
          className="inline-block w-full rounded-md bg-neutral-900 dark:bg-neutral-100 text-white dark:text-neutral-900 py-2.5 font-medium"
        >
          {t.backLink}
        </Link>
      </div>
    </main>
  );
}
