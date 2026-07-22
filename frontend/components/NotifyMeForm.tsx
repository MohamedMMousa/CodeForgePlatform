"use client";

import { useState } from "react";
import { submitLead } from "@/lib/api";
import { getDictionary, Locale } from "@/lib/i18n";

export default function NotifyMeForm({
  locale,
  courseId
}: {
  locale: Locale;
  courseId: string;
}) {
  const t = getDictionary(locale);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await submitLead({ name, email, courseId, locale });
      setDone(true);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  if (done) {
    return <div className="notice ok">{t.courseDetail.notifyMeSuccess}</div>;
  }

  return (
    <form onSubmit={onSubmit}>
      <div className="field">
        <label htmlFor="notify-name">{t.lead.name}</label>
        <input
          id="notify-name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
        />
      </div>
      <div className="field">
        <label htmlFor="notify-email">{t.lead.email}</label>
        <input
          id="notify-email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
      </div>
      <button className="btn" type="submit" disabled={busy}>
        {busy ? t.lead.submitting : t.courseDetail.notifyMe}
      </button>
      {error && <div className="notice err">{error}</div>}
    </form>
  );
}
