"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  CourseListItem,
  archiveCourse,
  createCourse,
  deleteCourse,
  getAdminCourses,
  publishCourse
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { useFormErrors } from "@/lib/formErrors";
import { isValidSlug, slugify } from "@/lib/validation";
import { FieldError, fieldErrorProps } from "@/components/FieldError";
import { Pagination } from "@/components/Pagination";

const PAGE_SIZE = 20;

export default function AdminCoursesPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.admin;

  const { session } = useAuth();
  const [courses, setCourses] = useState<CourseListItem[] | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const [title, setTitle] = useState("");
  const [slug, setSlug] = useState("");
  const [description, setDescription] = useState("");
  const [thumbnailUrl, setThumbnailUrl] = useState("");
  const [category, setCategory] = useState("");
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
    getAdminCourses({ status: statusFilter || undefined, page, pageSize: PAGE_SIZE })
      .then((result) => {
        setCourses(result.items);
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
      await createCourse(
        {
          title,
          slug,
          description: description || null,
          thumbnailUrl: thumbnailUrl || null,
          category: category || null,
          price: Number(price),
          currency
        },
      );
      setTitle("");
      setSlug("");
      setDescription("");
      setThumbnailUrl("");
      setCategory("");
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
      await publishCourse(id);
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
      await archiveCourse(id);
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
      await deleteCourse(id);
      load();
    } catch (err) {
      onError(err);
    } finally {
      setBusyId(null);
    }
  }

  return (
    <>
      <h2>{t.coursesTitle}</h2>
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

      {courses === null && !error && <p className="muted">…</p>}
      {courses !== null && courses.length === 0 && <p className="muted">{t.noCourses}</p>}
      {courses !== null && courses.length > 0 && (
        <div style={{ overflowX: "auto", marginBottom: "1.5rem" }}>
          <table className="table">
            <thead>
              <tr>
                <th>{t.fieldTitle}</th>
                <th>{t.fieldCategory}</th>
                <th>{t.fieldPrice}</th>
                <th>{t.status}</th>
                <th>{t.actions}</th>
              </tr>
            </thead>
            <tbody>
              {courses.map((c) => (
                <tr key={c.id}>
                  <td>
                    <Link href={`/${locale}/instructor/courses/${c.id}`}>{c.title}</Link>
                  </td>
                  <td>{c.category ?? "—"}</td>
                  <td>{c.price.toLocaleString(locale)} {c.currency}</td>
                  <td><span className="badge">{c.status}</span></td>
                  <td style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
                    <Link className="btn secondary" href={`/${locale}/instructor/courses/${c.id}`}>
                      {t.manageContent}
                    </Link>
                    {c.status !== "published" && (
                      <button
                        className="btn secondary"
                        disabled={busyId === c.id}
                        onClick={() => onPublish(c.id)}
                      >
                        {t.publish}
                      </button>
                    )}
                    {c.status !== "archived" && (
                      <button
                        className="btn secondary"
                        disabled={busyId === c.id}
                        onClick={() => onArchive(c.id)}
                      >
                        {t.archive}
                      </button>
                    )}
                    <button
                      className="btn secondary"
                      disabled={busyId === c.id}
                      onClick={() => onDelete(c.id)}
                    >
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

      <h3>{t.addCourse}</h3>
      <form onSubmit={onCreate} style={{ display: "flex", flexDirection: "column", gap: "0.5rem", maxWidth: "28rem" }}>
        <label>
          {t.fieldTitle}
          <input
            value={title}
            onChange={(e) => onTitleChange(e.target.value)}
            maxLength={255}
            required
            {...fieldErrorProps("course-title", formErrors.messagesFor("Title"))}
          />
          <FieldError id="course-title-error" messages={formErrors.messagesFor("Title")} />
        </label>
        <label>
          {t.fieldSlug}
          <input
            value={slug}
            onChange={(e) => onSlugChange(e.target.value)}
            maxLength={255}
            required
            {...fieldErrorProps("course-slug", formErrors.messagesFor("Slug"), "course-slug-hint")}
          />
          <span className="hint" id="course-slug-hint">
            {t.slugHint}
          </span>
          <FieldError id="course-slug-error" messages={formErrors.messagesFor("Slug")} />
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
            {...fieldErrorProps("course-description", formErrors.messagesFor("Description"))}
          />
          <FieldError
            id="course-description-error"
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
            {...fieldErrorProps("course-thumbnail", formErrors.messagesFor("ThumbnailUrl"))}
          />
          <FieldError
            id="course-thumbnail-error"
            messages={formErrors.messagesFor("ThumbnailUrl")}
          />
        </label>
        <label>
          {t.fieldCategory}
          <input
            value={category}
            onChange={(e) => {
              setCategory(e.target.value);
              formErrors.clearField("Category");
            }}
            maxLength={100}
            {...fieldErrorProps("course-category", formErrors.messagesFor("Category"))}
          />
          <FieldError id="course-category-error" messages={formErrors.messagesFor("Category")} />
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
            {...fieldErrorProps("course-price", formErrors.messagesFor("Price"))}
          />
          <FieldError id="course-price-error" messages={formErrors.messagesFor("Price")} />
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
            {...fieldErrorProps("course-currency", formErrors.messagesFor("Currency"))}
          />
          <FieldError id="course-currency-error" messages={formErrors.messagesFor("Currency")} />
        </label>
        {formErrors.formError && <p className="notice err">{formErrors.formError}</p>}
        <button className="btn" type="submit" disabled={creating}>
          {creating ? t.creating : t.create}
        </button>
      </form>
    </>
  );
}
