"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
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

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
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

  if (!session || session.role !== "admin") {
    return <p className="notice err">{getDictionary(locale).instructor.signInRequired}</p>;
  }

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setCreating(true);
    setError(null);
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
      load();
    } catch (err) {
      onError(err);
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
          <input value={title} onChange={(e) => setTitle(e.target.value)} required />
        </label>
        <label>
          {t.fieldSlug}
          <input value={slug} onChange={(e) => setSlug(e.target.value)} required />
        </label>
        <label>
          {t.fieldDescription}
          <textarea value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>
        <label>
          {t.fieldThumbnailUrl}
          <input value={thumbnailUrl} onChange={(e) => setThumbnailUrl(e.target.value)} />
        </label>
        <label>
          {t.fieldCategory}
          <input value={category} onChange={(e) => setCategory(e.target.value)} />
        </label>
        <label>
          {t.fieldPrice}
          <input type="number" min="0" value={price} onChange={(e) => setPrice(e.target.value)} required />
        </label>
        <label>
          {t.fieldCurrency}
          <input value={currency} onChange={(e) => setCurrency(e.target.value)} required />
        </label>
        <button className="btn" type="submit" disabled={creating}>
          {creating ? t.creating : t.create}
        </button>
      </form>
    </>
  );
}
