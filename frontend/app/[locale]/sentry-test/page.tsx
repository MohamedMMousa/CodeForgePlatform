import { notFound } from "next/navigation";
import { defaultLocale, getDictionary, isLocale, type Locale } from "@/lib/i18n";
import SentryTestActions from "./SentryTestActions";

// Forces per-request rendering so the SENTRY_TEST_ENABLED check below runs at request
// time, not build time — without this, Next could statically render the page once
// around whatever value was present during `npm run build` and cache that output for
// every future request, ignoring the runtime env var entirely. SENTRY_TEST_ENABLED is
// deliberately NOT NEXT_PUBLIC_-prefixed: NEXT_PUBLIC_ vars get inlined as literal
// values into the bundle at build time (verified — building with it set and then
// starting that same build with it unset still served the page), which would defeat
// the gate the same way regardless of force-dynamic. A plain server-only var is read
// live from process.env on every request instead.
export const dynamic = "force-dynamic";

export default async function SentryTestPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  if (!process.env.SENTRY_TEST_ENABLED) {
    notFound();
  }

  const { locale: rawLocale } = await params;
  const locale: Locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).sentryTest;

  return (
    <main className="cf-container">
      <div className="card" style={{ maxWidth: 520, margin: "0 auto" }}>
        <h1>{t.title}</h1>
        <p className="muted">{t.description}</p>
        <SentryTestActions
          clientButtonLabel={t.clientButton}
          clientSentLabel={t.clientSent}
          serverButtonLabel={t.serverButton}
          serverSentLabel={t.serverSent}
        />
      </div>
    </main>
  );
}
