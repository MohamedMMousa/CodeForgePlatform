"use client";

import * as React from "react";

import { cn } from "@/lib/utils";

// DESIGN_LANGUAGE.md §3 — uppercase `meta` column headers in --text-muted,
// hairline row separators, generous row padding, and numbers end-aligned per
// direction. Zebra striping in --surface-2 is optional, so it's opt-in.
function Table({
  className,
  zebra = false,
  ...props
}: React.ComponentProps<"table"> & { zebra?: boolean }) {
  return (
    <div data-slot="table-container" className="relative w-full overflow-x-auto">
      <table
        data-slot="table"
        data-zebra={zebra}
        className={cn(
          "w-full caption-bottom border-collapse text-body",
          "data-[zebra=true]:[&_tbody_tr:nth-child(even)]:bg-surface-2",
          className
        )}
        {...props}
      />
    </div>
  );
}

function TableHeader({ className, ...props }: React.ComponentProps<"thead">) {
  return (
    <thead
      data-slot="table-header"
      className={cn("[&_tr]:border-b [&_tr]:border-border", className)}
      {...props}
    />
  );
}

function TableBody({ className, ...props }: React.ComponentProps<"tbody">) {
  return (
    <tbody
      data-slot="table-body"
      className={cn("[&_tr:last-child]:border-0", className)}
      {...props}
    />
  );
}

function TableFooter({ className, ...props }: React.ComponentProps<"tfoot">) {
  return (
    <tfoot
      data-slot="table-footer"
      className={cn(
        "border-t border-border bg-surface-2 font-semibold [&>tr]:last:border-b-0",
        className
      )}
      {...props}
    />
  );
}

function TableRow({ className, ...props }: React.ComponentProps<"tr">) {
  return (
    <tr
      data-slot="table-row"
      className={cn(
        "border-b border-border transition-colors hover:bg-surface-2 data-[state=selected]:bg-surface-2",
        className
      )}
      {...props}
    />
  );
}

// `numeric` end-aligns and tabular-figures the column. `text-end` is logical, so
// a grade column sits against the right edge in English and the left in Arabic —
// which is what "end-aligned per direction" means in §3. Digits stay Western in
// both (§2.1).
function TableHead({
  className,
  numeric = false,
  ...props
}: React.ComponentProps<"th"> & { numeric?: boolean }) {
  return (
    <th
      data-slot="table-head"
      className={cn(
        // `eyebrow` carries the uppercase + letter-spacing treatment and drops
        // it under lang="ar", where letter case doesn't exist (§2.1).
        "eyebrow h-11 px-4 align-middle whitespace-nowrap text-text-muted",
        numeric ? "text-end" : "text-start",
        className
      )}
      {...props}
    />
  );
}

function TableCell({
  className,
  numeric = false,
  ...props
}: React.ComponentProps<"td"> & { numeric?: boolean }) {
  return (
    <td
      data-slot="table-cell"
      className={cn(
        "px-4 py-3 align-middle text-text-secondary",
        numeric ? "text-end tabular-nums" : "text-start",
        className
      )}
      {...props}
    />
  );
}

function TableCaption({ className, ...props }: React.ComponentProps<"caption">) {
  return (
    <caption
      data-slot="table-caption"
      className={cn("mt-4 text-meta text-text-muted", className)}
      {...props}
    />
  );
}

export {
  Table,
  TableHeader,
  TableBody,
  TableFooter,
  TableHead,
  TableRow,
  TableCell,
  TableCaption
};
