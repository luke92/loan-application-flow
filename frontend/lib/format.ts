export function formatSSN(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 9);
  const parts = [digits.slice(0, 3), digits.slice(3, 5), digits.slice(5, 9)];
  return parts.filter(Boolean).join("-");
}

export function formatZip(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 9);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
}
