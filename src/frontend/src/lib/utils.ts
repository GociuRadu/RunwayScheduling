export function formatDate(value: string): string {
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString();
}

export function toUtcString(local: string): string | null {
  if (!local) return null;
  return new Date(local).toISOString();
}

export function toLocalDatetime(utcStr: string): string {
  if (!utcStr) return "";
  const d = new Date(utcStr);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
