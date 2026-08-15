"use client";

import { useState } from "react";
import { submitLead } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { getDictionary, Locale } from "@/lib/i18n";

// Restyled onto §2 tokens and the components/ui primitives with the course-detail
// rebuild (surface #3). Submit behaviour is unchanged. This component is used only
// by that page, so nothing else re-skins as a side effect.
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
    return (
      <div
        // Announced rather than silently swapped in — the form the user was
        // focused inside disappears when this replaces it.
        role="status"
        className="rounded-card border border-success-border bg-success-soft p-4 text-body text-success"
      >
        {t.courseDetail.notifyMeSuccess}
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      <div className="flex flex-col gap-4 sm:flex-row">
        <div className="flex flex-1 flex-col gap-1.5">
          <label htmlFor="notify-name" className="text-label text-text">
            {t.lead.name}
          </label>
          <Input
            id="notify-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>
        <div className="flex flex-1 flex-col gap-1.5">
          <label htmlFor="notify-email" className="text-label text-text">
            {t.lead.email}
          </label>
          <Input
            id="notify-email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>
      </div>
      {/* Secondary, not primary: when this form is on screen there is nothing
          bookable, and §3 reserves the orange fill for a live purchase action. */}
      <Button type="submit" variant="secondary" disabled={busy} className="self-start">
        {busy ? t.lead.submitting : t.courseDetail.notifyMe}
      </Button>
      {error ? (
        <div
          role="alert"
          className="rounded-card border border-danger-border bg-danger-soft p-4 text-body text-danger"
        >
          {error}
        </div>
      ) : null}
    </form>
  );
}
