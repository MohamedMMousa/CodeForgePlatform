"use client";

import { Dictionary, format } from "@/lib/i18n";

export function Pagination({
  t,
  page,
  pageSize,
  totalCount,
  onPageChange
}: {
  t: Dictionary;
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  if (totalPages <= 1) {
    return null;
  }

  const start = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);

  return (
    <div className="pagination">
      <span className="muted">
        {format(t.pagination.showingCount, { count: `${start}-${end}`, total: totalCount })}
      </span>
      <div className="pagination-controls">
        <button
          type="button"
          className="btn secondary"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          {t.pagination.previous}
        </button>
        <span className="muted">{format(t.pagination.pageOf, { page, totalPages })}</span>
        <button
          type="button"
          className="btn secondary"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          {t.pagination.next}
        </button>
      </div>
    </div>
  );
}
