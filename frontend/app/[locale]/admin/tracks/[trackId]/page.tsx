"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  CourseListItem,
  TrackDetail,
  addCourseToTrack,
  getAdminCourses,
  getTrackById,
  removeCourseFromTrack,
  updateTrack
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { useFormErrors } from "@/lib/formErrors";
import { isValidSlug } from "@/lib/validation";
import { FieldError, fieldErrorProps } from "@/components/FieldError";

export default function AdminTrackDetailPage({
  params
}: {
  params: Promise<{ locale: string; trackId: string }>;
}) {
  const { locale: rawLocale, trackId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.admin;

  const { session } = useAuth();
  const [track, setTrack] = useState<TrackDetail | null>(null);
  const [courses, setCourses] = useState<CourseListItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [title, setTitle] = useState("");
  const [slug, setSlug] = useState("");
  const [description, setDescription] = useState("");
  const [thumbnailUrl, setThumbnailUrl] = useState("");
  const [price, setPrice] = useState("0");
  const [currency, setCurrency] = useState("EGP");

  const [selectedCourseId, setSelectedCourseId] = useState("");
  const [sortOrder, setSortOrder] = useState("0");
  const [addingCourse, setAddingCourse] = useState(false);

  const formErrors = useFormErrors(dictionary);

  /** Everything except the save form — those failures go to `formErrors`, which can place
   * them on the field that caused them instead of in a banner at the top of the page. */
  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  function load() {
    if (!session) return;
    getTrackById(trackId)
      .then((tr) => {
        setTrack(tr);
        setTitle(tr.title);
        setSlug(tr.slug);
        setDescription(tr.description ?? "");
        setThumbnailUrl(tr.thumbnailUrl ?? "");
        setPrice(String(tr.price));
        setCurrency(tr.currency);
      })
      .catch(onError);
    getAdminCourses({ pageSize: 100 })
      .then((result) => setCourses(result.items))
      .catch(() => {});
  }

  useEffect(load, [session, trackId]); // eslint-disable-line react-hooks/exhaustive-deps

  const gate = useSessionGate({ locale, roles: ["admin"], bare: true });
  if (!gate.ok) return gate.fallback;

  async function onSave(e: React.FormEvent) {
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
    setSaving(true);
    try {
      const updated = await updateTrack(
        trackId,
        { title, slug, description: description || null, thumbnailUrl: thumbnailUrl || null, price: Number(price), currency },
      );
      setTrack(updated);
    } catch (err) {
      formErrors.capture(err);
    } finally {
      setSaving(false);
    }
  }

  async function onAddCourse(e: React.FormEvent) {
    e.preventDefault();
    if (!session || !selectedCourseId) return;
    setAddingCourse(true);
    setError(null);
    try {
      await addCourseToTrack(trackId, selectedCourseId, Number(sortOrder));
      setSelectedCourseId("");
      setSortOrder("0");
      load();
    } catch (err) {
      onError(err);
    } finally {
      setAddingCourse(false);
    }
  }

  async function onRemoveCourse(courseId: string) {
    if (!session) return;
    try {
      await removeCourseFromTrack(trackId, courseId);
      load();
    } catch (err) {
      onError(err);
    }
  }

  const availableCourses = courses.filter(
    (c) => !track?.courses.some((tc) => tc.courseId === c.id)
  );

  return (
    <>
      <p>
        <Link href={`/${locale}/admin/tracks`}>← {t.tracksTitle}</Link>
      </p>
      {error && <p className="notice err">{error}</p>}
      {!track && !error && <p className="muted">…</p>}

      {track && (
        <>
          <h2>{track.title}</h2>
          <form onSubmit={onSave} style={{ display: "flex", flexDirection: "column", gap: "0.5rem", maxWidth: "28rem" }}>
            <label>
              {t.fieldTitle}
              <input
                value={title}
                onChange={(e) => {
                  setTitle(e.target.value);
                  formErrors.clearField("Title");
                }}
                maxLength={255}
                required
                {...fieldErrorProps("track-edit-title", formErrors.messagesFor("Title"))}
              />
              <FieldError
                id="track-edit-title-error"
                messages={formErrors.messagesFor("Title")}
              />
            </label>
            <label>
              {t.fieldSlug}
              {/* No auto-derivation from the title here: silently rewriting a live slug
                  would break every URL already pointing at this track. */}
              <input
                value={slug}
                onChange={(e) => {
                  setSlug(e.target.value);
                  formErrors.clearField("Slug");
                }}
                maxLength={255}
                required
                {...fieldErrorProps(
                  "track-edit-slug",
                  formErrors.messagesFor("Slug"),
                  "track-edit-slug-hint"
                )}
              />
              <span className="hint" id="track-edit-slug-hint">
                {t.slugHint}
              </span>
              <FieldError id="track-edit-slug-error" messages={formErrors.messagesFor("Slug")} />
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
                {...fieldErrorProps("track-edit-description", formErrors.messagesFor("Description"))}
              />
              <FieldError
                id="track-edit-description-error"
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
                {...fieldErrorProps("track-edit-thumbnail", formErrors.messagesFor("ThumbnailUrl"))}
              />
              <FieldError
                id="track-edit-thumbnail-error"
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
                {...fieldErrorProps("track-edit-price", formErrors.messagesFor("Price"))}
              />
              <FieldError
                id="track-edit-price-error"
                messages={formErrors.messagesFor("Price")}
              />
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
                {...fieldErrorProps("track-edit-currency", formErrors.messagesFor("Currency"))}
              />
              <FieldError
                id="track-edit-currency-error"
                messages={formErrors.messagesFor("Currency")}
              />
            </label>
            {formErrors.formError && <p className="notice err">{formErrors.formError}</p>}
            <button className="btn" type="submit" disabled={saving}>
              {saving ? t.saving : t.save}
            </button>
          </form>

          <h3 style={{ marginTop: "1.5rem" }}>{t.coursesInTrack}</h3>
          {track.courses.length === 0 && <p className="muted">{t.noCourses}</p>}
          {track.courses.length > 0 && (
            <table className="table">
              <thead>
                <tr>
                  <th>{t.fieldTitle}</th>
                  <th>{t.fieldPrice}</th>
                  <th>{t.sortOrder}</th>
                  <th>{t.actions}</th>
                </tr>
              </thead>
              <tbody>
                {track.courses.map((c) => (
                  <tr key={c.courseId}>
                    <td>{c.courseTitle}</td>
                    <td>{c.coursePrice.toLocaleString(locale)}</td>
                    <td>{c.sortOrder}</td>
                    <td>
                      <button className="btn secondary" onClick={() => onRemoveCourse(c.courseId)}>
                        {t.removeCourseFromTrack}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          <form
            onSubmit={onAddCourse}
            style={{ display: "flex", gap: "0.5rem", alignItems: "flex-end", marginTop: "0.75rem", flexWrap: "wrap" }}
          >
            <label>
              {t.selectCourse}
              <select value={selectedCourseId} onChange={(e) => setSelectedCourseId(e.target.value)} required>
                <option value="" disabled>
                  {t.selectCourse}
                </option>
                {availableCourses.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.title}
                  </option>
                ))}
              </select>
            </label>
            <label>
              {t.sortOrder}
              <input type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.value)} style={{ width: "5rem" }} />
            </label>
            <button className="btn" type="submit" disabled={addingCourse || !selectedCourseId}>
              {t.addCourseToTrack}
            </button>
          </form>
        </>
      )}
    </>
  );
}
