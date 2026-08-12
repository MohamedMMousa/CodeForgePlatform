"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  TrackListItem,
  archiveTrack,
  createTrack,
  deleteTrack,
  getAdminTracks,
  publishTrack
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { useFormErrors } from "@/lib/formErrors";
import { isValidSlug, slugify } from "@/lib/validation";
import { FieldError, fieldErrorProps } from "@/components/FieldError";
import { Pagination } from "@/components/Pagination";

const PAGE_SIZE = 20;

export default function AdminTracksPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.admin;

  const { session } = useAuth();
  const [tracks, setTracks] = useState<TrackListItem[] | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const [title, setTitle] = useState("");
  const [slug, setSlug] = useState("");
  const [description, setDescription] = useState("");
  const [thumbnailUrl, setThumbnailUrl] = useState("");
  const [price, setPrice] = useState("0");
  const [currency, setCurrency] = useState("EGP");
  const [creating, setCreating] = useState(false);
  // Once the admin edits the slug by hand it stops tracking the title.
  const [slugTouched, setSlugTouched] = useState(false);

  const formErrors = useFormErrors(dictionary);

  /** List-load failures only — submit failures go to `formErrors`, which can place them
   * on the field that caused them instead of in a banner at the top of the page. */
  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  function onTitleChange(value: string) {
    setTitle(value);
    formErrors.clearField("Title");
    if (slugTouched) return;
    // An Arabic title has no Latin-only slug; leave whatever is there rather than
    // clearing the field on every keystroke.
    const suggested = slugify(value);
    if (suggested) {
      setSlug(suggested);
      formErrors.clearField("Slug");
    }
  }

  function onSlugChange(value: string) {
    setSlug(value);
    setSlugTouched(true);
    formErrors.clearField("Slug");
  }

  function load() {
    if (!session) return;
    getAdminTracks({ status: statusFilter || undefined, page, pageSize: PAGE_SIZE })
      .then((result) => {
        setTracks(result.items);
        setTotalCount(result.totalCount);
      })
      .catch(onError);
  }

  useEffect(load, [session, statusFilter, page]); // eslint-disable-line react-hooks/exhaustive-deps
  useEffect(() => setPage(1), [statusFilter]);

  const gate = useSessionGate({ locale, roles: ["admin"], bare: true });
  if (!gate.ok) return gate.fallback;

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setError(null);
    formErrors.reset();
    // Checked locally so the admin gets the same inline message without a round-trip;
    // the server re-checks it regardless.
    if (!isValidSlug(slug)) {
      formErrors.setFieldErrors({ Slug: [dictionary.validation.slugFormat] });
      return;
    }
    setCreating(true);
    try {
      await createTrack(
        { title, slug, description: description || null, thumbnailUrl: thumbnailUrl || null, price: Number(price), currency },
      );
      setTitle("");
      setSlug("");
      setDescription("");
      setThumbnailUrl("");
      setPrice("0");
      setSlugTouched(false);
      load();
    } catch (err) {
      formErrors.capture(err);
    } finally {
      setCreating(false);
    }
  }

  async function onPublish(id: string) {
    if (!session) return;
    setBusyId(id);
    try {
      await publishTrack(id);
      load();
    } catch (err) {
      onError(err);
    } finally {
      setBusyId(null);
    }
  }

  async function onArchive(id: string) {
    if (!session) return;
    setBusyId(id);
    try {
      await archiveTrack(id);
      load();
    } catch (err) {
      onError(err);
    } finally {
      setBusyId(null);
    }
  }

  async function onDelete(id: string) {
    if (!session) return;
    if (!confirm(t.confirmDelete)) return;
    setBusyId(id);
    try {
      await deleteTrack(id);
      load();
    } catch (err) {
      onError(err);
    } finally {
      setBusyId(null);
    }
  }

  return (
    <>
      <h2>{t.tracksTitle}</h2>
      {error && <p className="notice err">{error}</p>}

      <div style={{ marginBottom: "1rem" }}>
        <label>
          {t.status}{" "}
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            <option value="">{t.all}</option>
            <option value="draft">{t.statusDraft}</option>
            <option value="published">{t.statusPublished}</option>
            <option value="archived">{t.statusArchived}</option>
          </select>
        </label>
      </div>

      {tracks === null && !error && <p className="muted">…</p>}
      {tracks !== null && tracks.length === 0 && <p className="muted">{t.noTracks}</p>}
      {tracks !== null && tracks.length > 0 && (
        <div style={{ overflowX: "auto", marginBottom: "1.5rem" }}>
          <table className="table">
            <thead>
              <tr>
                <th>{t.fieldTitle}</th>
                <th>{t.coursesInTrack}</th>
                <th>{t.fieldPrice}</th>
                <th>{t.status}</th>
                <th>{t.actions}</th>
              </tr>
            </thead>
            <tbody>
              {tracks.map((tr) => (
                <tr key={tr.id}>
                  <td>
                    <Link href={`/${locale}/admin/tracks/${tr.id}`}>{tr.title}</Link>
                  </td>
                  <td>{tr.courseCount}</td>
                  <td>{tr.price.toLocaleString(locale)} {tr.currency}</td>
                  <td><span className="badge">{tr.status}</span></td>
                  <td style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
                    <Link className="btn secondary" href={`/${locale}/admin/tracks/${tr.id}`}>
                      {t.actions}
                    </Link>
                    {tr.status !== "published" && (
                      <button className="btn secondary" disabled={busyId === tr.id} onClick={() => onPublish(tr.id)}>
                        {t.publish}
                      </button>
                    )}
                    {tr.status !== "archived" && (
                      <button className="btn secondary" disabled={busyId === tr.id} onClick={() => onArchive(tr.id)}>
                        {t.archive}
                      </button>
                    )}
                    <button className="btn secondary" disabled={busyId === tr.id} onClick={() => onDelete(tr.id)}>
                      {t.delete}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination
            t={dictionary}
            page={page}
            pageSize={PAGE_SIZE}
            totalCount={totalCount}
            onPageChange={setPage}
          />
        </div>
      )}

      <h3>{t.addTrack}</h3>
      <form onSubmit={onCreate} style={{ display: "flex", flexDirection: "column", gap: "0.5rem", maxWidth: "28rem" }}>
        <label>
          {t.fieldTitle}
          <input
            value={title}
            onChange={(e) => onTitleChange(e.target.value)}
            maxLength={255}
            required
            {...fieldErrorProps("track-title", formErrors.messagesFor("Title"))}
          />
          <FieldError id="track-title-error" messages={formErrors.messagesFor("Title")} />
        </label>
        <label>
          {t.fieldSlug}
          <input
            value={slug}
            onChange={(e) => onSlugChange(e.target.value)}
            maxLength={255}
            required
            {...fieldErrorProps("track-slug", formErrors.messagesFor("Slug"), "track-slug-hint")}
          />
          <span className="hint" id="track-slug-hint">
            {t.slugHint}
          </span>
          <FieldError id="track-slug-error" messages={formErrors.messagesFor("Slug")} />
        </label>
        <label>
          {t.fieldDescription}
          <textarea
            value={description}
            onChange={(e) => {
              setDescription(e.target.value);
              formErrors.clearField("Description");
            }}
            maxLength={5000}
            {...fieldErrorProps("track-description", formErrors.messagesFor("Description"))}
          />
          <FieldError
            id="track-description-error"
            messages={formErrors.messagesFor("Description")}
          />
        </label>
        <label>
          {t.fieldThumbnailUrl}
          <input
            value={thumbnailUrl}
            onChange={(e) => {
              setThumbnailUrl(e.target.value);
              formErrors.clearField("ThumbnailUrl");
            }}
            maxLength={500}
            {...fieldErrorProps("track-thumbnail", formErrors.messagesFor("ThumbnailUrl"))}
          />
          <FieldError
            id="track-thumbnail-error"
            messages={formErrors.messagesFor("ThumbnailUrl")}
          />
        </label>
        <label>
          {t.fieldPrice}
          <input
            type="number"
            min="0"
            value={price}
            onChange={(e) => {
              setPrice(e.target.value);
              formErrors.clearField("Price");
            }}
            required
            {...fieldErrorProps("track-price", formErrors.messagesFor("Price"))}
          />
          <FieldError id="track-price-error" messages={formErrors.messagesFor("Price")} />
        </label>
        <label>
          {t.fieldCurrency}
          <input
            value={currency}
            onChange={(e) => {
              setCurrency(e.target.value);
              formErrors.clearField("Currency");
            }}
            maxLength={10}
            required
            {...fieldErrorProps("track-currency", formErrors.messagesFor("Currency"))}
          />
          <FieldError id="track-currency-error" messages={formErrors.messagesFor("Currency")} />
        </label>
        {formErrors.formError && <p className="notice err">{formErrors.formError}</p>}
        <button className="btn" type="submit" disabled={creating}>
          {creating ? t.creating : t.create}
        </button>
      </form>
    </>
  );
}
