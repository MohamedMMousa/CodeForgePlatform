"use client";

import { use, useState } from "react";
import { useAuth } from "@/lib/auth";
import { ApiRequestError } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function LoginPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).login;

  const { signIn } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setMessage(null);
    try {
      const auth = await signIn(email, password, locale);
      const text = t.success
        .replace("{name}", auth.fullName)
        .replace("{role}", auth.role);
      setMessage({
        ok: true,
        text: auth.mustChangePassword ? `${text} ${t.mustChange}` : text
      });
    } catch (error) {
      const text =
        error instanceof ApiRequestError && error.info.status === 401
          ? t.failed
          : (error as Error).message;
      setMessage({ ok: false, text });
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="container">
      <div className="card" style={{ maxWidth: 420, margin: "0 auto" }}>
        <h1>{t.title}</h1>
        <form onSubmit={onSubmit}>
          <div className="field">
            <label htmlFor="email">{t.email}</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
              required
            />
          </div>
          <div className="field">
            <label htmlFor="password">{t.password}</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>
          <button className="btn" type="submit" disabled={busy}>
            {busy ? t.signingIn : t.submit}
          </button>
        </form>
        {message && (
          <div className={`notice ${message.ok ? "ok" : "err"}`}>{message.text}</div>
        )}
      </div>
    </main>
  );
}
