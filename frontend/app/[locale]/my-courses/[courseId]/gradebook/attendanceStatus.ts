import type { Dictionary } from "@/lib/i18n";

// Single source of truth for how a per-session attendance status renders in the
// gradebook — mirrors surface #5's sessionState.ts so nothing badges attendance
// two different ways. §3 badge semantics, read honestly:
//   present → success (green)  — attended
//   late    → warning (amber)  — attended but noteworthy. The rate calculator
//             (AttendanceRateCalculator) still CREDITS late as present, so this
//             badge flags lateness without implying a miss.
//   excused → neutral (gray)   — excluded from the rate's denominator entirely,
//             never counts against the student.
//   absent  → danger (red)     — a real miss.
//
// A null status is "session held but not marked yet" and is deliberately NOT a
// badge: it renders as muted text via `notMarked` below, so it can never read as
// an absence (the one hard requirement — an unmarked session must not look like
// a missed one).

type BadgeVariant = "success" | "warning" | "danger" | "neutral";

export type AttendanceStatusDisplay =
  | { kind: "badge"; variant: BadgeVariant; label: string }
  | { kind: "muted"; label: string };

export function attendanceStatusDisplay(
  status: string | null | undefined,
  t: Dictionary["gradebook"]
): AttendanceStatusDisplay {
  switch (status) {
    case "present":
      return { kind: "badge", variant: "success", label: t.statusPresent };
    case "late":
      return { kind: "badge", variant: "warning", label: t.statusLate };
    case "excused":
      return { kind: "badge", variant: "neutral", label: t.statusExcused };
    case "absent":
      return { kind: "badge", variant: "danger", label: t.statusAbsent };
    default:
      // null / unmarked / any unexpected value → muted, never an absence badge.
      return { kind: "muted", label: t.statusNotMarked };
  }
}
