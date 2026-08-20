import type { CertificateVerification } from "@/lib/api";
import type { Dictionary } from "@/lib/i18n";

// Single source of truth for how a certificate's tier and a verify result render as
// badges — shared by app/[locale]/my-certificates/page.tsx and app/[locale]/verify/page.tsx
// (mirrors the sessionState.ts / attendanceStatus.ts pattern) so the two surfaces can
// never disagree about what a tier or a verify outcome means. Pure data logic, no JSX.

type BadgeVariant = "success" | "warning" | "danger" | "neutral";

export function tierDisplay(
  tier: string,
  t: Dictionary["certificates"]
): { variant: BadgeVariant; label: string } {
  switch (tier) {
    case "completion":
      // The earned achievement.
      return { variant: "success", label: t.completion };
    case "participation":
      // Honest "attended" acknowledgment — still a real certificate, never a
      // danger/failure treatment even though it's the lower tier.
      return { variant: "neutral", label: t.participation };
    default:
      // Defensive only: Tier is a plain string field (no C# enum) on the backend.
      return { variant: "neutral", label: tier };
  }
}

export type VerificationDisplay =
  | { kind: "valid"; variant: "success" }
  | { kind: "revoked"; variant: "danger" } // DESIGN_LANGUAGE.md §3 lists "revoked" under danger.
  | { kind: "not-found"; variant: "neutral" };

export function verificationDisplay(result: CertificateVerification): VerificationDisplay {
  if (!result.found) return { kind: "not-found", variant: "neutral" };
  if (!result.isValid) return { kind: "revoked", variant: "danger" };
  return { kind: "valid", variant: "success" };
}
