import { LoanApplicationForm } from "@/components/loan-application-form";

export default function HomePage() {
  return (
    <main className="flex-1 flex items-center justify-center p-6">
      <div className="w-full max-w-xl bg-white dark:bg-neutral-900 rounded-xl shadow-sm border border-neutral-200 dark:border-neutral-800 p-8">
        <LoanApplicationForm />
      </div>
    </main>
  );
}
