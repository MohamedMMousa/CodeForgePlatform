"use client";

import { use, useState } from "react";
import { submitLead } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function ContactPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).lead;

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<{ ok: boolean; text: string } | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setResult(null);
    try {
      await submitLead({ name, email, phone: phone || undefined, message: message || undefined, locale });
      setResult({ ok: true, text: t.success });
    } catch (error) {
      setResult({ ok: false, text: (error as Error).message || t.error });
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="cf-container">
      <div className="card" style={{ maxWidth: 520, margin: "0 auto" }}>
        <h1>{t.title}</h1>
        <p className="muted">{t.description}</p>

        {result?.ok ? (
          <div className="notice ok">{result.text}</div>
        ) : (
          <form onSubmit={onSubmit}>
            <div className="field">
              <label htmlFor="name">{t.name}</label>
              <input id="name" value={name} onChange={(e) => setName(e.target.value)} required />
            </div>
            <div className="field">
              <label htmlFor="email">{t.email}</label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div className="field">
              <label htmlFor="phone">{t.phone}</label>
              <input id="phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
            </div>
            <div className="field">
              <label htmlFor="message">{t.message}</label>
              <textarea
                id="message"
                rows={4}
                value={message}
                onChange={(e) => setMessage(e.target.value)}
              />
            </div>
            <button className="btn" type="submit" disabled={busy}>
              {busy ? t.submitting : t.submit}
            </button>
            {result && !result.ok && <div className="notice err">{result.text}</div>}
          </form>
        )}
      </div>
    </main>
  );
}
