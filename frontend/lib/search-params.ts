export type SearchParams = Promise<{ [key: string]: string | string[] | undefined }>;

export function firstValue(value: string | string[] | undefined): string {
  return Array.isArray(value) ? value[0] ?? "" : value ?? "";
}
