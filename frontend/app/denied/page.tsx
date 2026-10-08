import Link from "next/link";
import { firstValue, type SearchParams } from "@/lib/search-params";
import { translations } from "@/lib/i18n/translations";

export default async function DeniedPage({
  searchParams,
}: {
  searchParams: SearchParams;
}) {
  const t = translations.deniedPage;
  const params = await searchParams;
  const reason = firstValue(params.reason) || t.defaultReason;

  return (
    <main className="flex-1 flex items-center justify-center p-6">
      <div className="w-full max-w-md bg-white dark:bg-neutral-900 rounded-xl shadow-sm border border-neutral-200 dark:border-neutral-800 p-8 text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100 dark:bg-red-900/40 text-red-600 dark:text-red-400 text-2xl">
          ✕
        </div>
        <h1 className="text-2xl font-semibold mb-2">{t.heading}</h1>
        <p className="text-sm text-neutral-600 dark:text-neutral-400 mb-6">{reason}</p>

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
