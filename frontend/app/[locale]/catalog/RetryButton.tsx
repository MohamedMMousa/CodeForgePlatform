"use client";

import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";

// A server component can't re-run itself from a link — this is the smallest
// client surface that lets the designed error state actually retry the fetch
// that failed, by asking Next.js to re-render the current route segment.
export function RetryButton({ label }: { label: string }) {
  const router = useRouter();
  return (
    <Button variant="secondary" onClick={() => router.refresh()}>
      {label}
    </Button>
  );
}
