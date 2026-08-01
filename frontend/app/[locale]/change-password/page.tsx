"use client";

import { use, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth";
import { ApiRequestError, changePassword } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

function landingPathFor(role: string, locale: string): string {
  if (role === "admin") return `/${locale}/admin/courses`;
  if (role === "instructor") return `/${locale}/instructor`;
  return `/${locale}/dashboard`;
}

export default function ChangePasswordPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).changePassword;

  const { session, refreshSession } = useAuth();
  const router = useRouter();

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  // middleware redirects a signed-out visitor away from this route before it
  // renders; this is just a type-narrowing safety net for the rare case where the
  // server-resolved session and the cookie briefly disagree.
  if (!session) {
    return null;
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (newPassword !== confirmPassword) {
      setMessage({ ok: false, text: t.mismatch });
      return;
    }

    setBusy(true);
    setMessage(null);
    try {
      const auth = await changePassword(currentPassword, newPassword, locale);
      await refreshSession();
      setMessage({ ok: true, text: t.success });
      router.replace(landingPathFor(auth.role, locale));
    } catch (error) {
      const text =
        error instanceof ApiRequestError && error.info.status === 401
          ? t.wrongCurrent
          : t.failed;
      setMessage({ ok: false, text });
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="container">
      <div className="card" style={{ maxWidth: 420, margin: "0 auto" }}>
        <h1>{t.title}</h1>
        {session.mustChangePassword && <p className="notice">{t.forcedNotice}</p>}
        <form onSubmit={onSubmit}>
          <div className="field">
            <label htmlFor="currentPassword">{t.currentPassword}</label>
            <input
              id="currentPassword"
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>
          <div className="field">
            <label htmlFor="newPassword">{t.newPassword}</label>
            <input
              id="newPassword"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              autoComplete="new-password"
              minLength={8}
              required
            />
          </div>
          <div className="field">
            <label htmlFor="confirmPassword">{t.confirmPassword}</label>
            <input
              id="confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              autoComplete="new-password"
              minLength={8}
              required
            />
          </div>
          <button className="btn" type="submit" disabled={busy}>
            {busy ? t.submitting : t.submit}
          </button>
        </form>
        {message && (
          <div className={`notice ${message.ok ? "ok" : "err"}`}>{message.text}</div>
        )}
      </div>
    </main>
  );
}
