export function formatCopCurrency(value: number | string | null | undefined): string {
  const numericValue = Number(value ?? 0);
  if (!Number.isFinite(numericValue)) {
    return '$ 0';
  }

  const formatted = new Intl.NumberFormat('es-CO', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(Math.round(numericValue));

  return `$ ${formatted}`;
}
