"use client";

import { use, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/lib/auth";
import { ApiRequestError } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale, Locale } from "@/lib/i18n";

// The only existing "redirect after an auth action" precedent in the app
// (change-password/page.tsx) is role-aware, not a single fixed landing page:
// admin/instructor accounts have their own home surfaces, and RoleNav never
// links them to /dashboard. Duplicated here rather than imported — it's a
// tiny pure function, and this page has no other reason to reach into
// change-password's module.
function landingPathFor(role: string, locale: Locale): string {
  if (role === "admin") return `/${locale}/admin/courses`;
  if (role === "instructor") return `/${locale}/instructor`;
  return `/${locale}/dashboard`;
}

export default function LoginPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.login;

  const router = useRouter();
  const { signIn } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const auth = await signIn(email, password, locale);
      // PasswordChangeGate (mounted in the root layout) would also catch this
      // on the next render, but redirecting here directly avoids a visible
      // dashboard-then-change-password flash.
      if (auth.mustChangePassword) {
        router.replace(`/${locale}/change-password`);
      } else {
        router.replace(landingPathFor(auth.role, locale));
      }
    } catch (err) {
      setError(
        err instanceof ApiRequestError && err.info.status === 401
          ? t.failed
          : (err as Error).message
      );
      setBusy(false);
    }
  }

  return (
    <main data-theme="light" className="min-h-screen bg-bg [&_:is(h1,h2,h3,p)]:m-0">
      <div className="mx-auto flex w-full max-w-md flex-col gap-6 ps-5 pe-5 py-16">
        <Card>
          <CardHeader>
            <CardTitle as="h1">{t.title}</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={onSubmit} className="flex flex-col gap-4">
              <div className="flex flex-col gap-1.5">
                <label htmlFor="email" className="text-label text-text">
                  {t.email}
                </label>
                <Input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoComplete="email"
                  disabled={busy}
                  required
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <label htmlFor="password" className="text-label text-text">
                  {t.password}
                </label>
                <Input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="current-password"
                  disabled={busy}
                  required
                />
              </div>
              {error ? (
                <p
                  role="alert"
                  className="rounded-card border border-danger-border bg-danger-soft p-3 text-body text-danger"
                >
                  {error}
                </p>
              ) : null}
              <Button type="submit" disabled={busy} className="w-full">
                {busy ? t.signingIn : t.submit}
              </Button>
            </form>
          </CardContent>
          <CardFooter className="justify-center">
            <Link
              href={`/${locale}/catalog`}
              className="text-body font-semibold text-accent-text hover:underline"
            >
              {dictionary.home.newHereHint}
            </Link>
          </CardFooter>
        </Card>
      </div>
    </main>
  );
}
