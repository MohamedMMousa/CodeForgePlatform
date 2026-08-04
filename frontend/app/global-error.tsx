"use client";

// Sentry's required catch-all: replaces the ENTIRE root, including app/[locale]/layout.tsx,
// so it must define its own <html>/<body> and can't use the locale dictionary or RTL
// direction — by the time this renders, the normal render tree (including locale
// detection) has already failed. Sibling to app/[locale]/, not inside it, since it
// must also catch errors the locale layout itself throws.
import * as Sentry from "@sentry/nextjs";
import NextError from "next/error";
import { useEffect } from "react";

export default function GlobalError({
  error
}: {
  error: Error & { digest?: string };
}) {
  useEffect(() => {
    Sentry.captureException(error);
  }, [error]);

  return (
    <html>
      <body>
        <NextError statusCode={0} />
      </body>
    </html>
  );
}
