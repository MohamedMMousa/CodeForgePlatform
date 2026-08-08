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

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
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
    setCreating(true);
    setError(null);
    try {
      await createTrack(
        { title, slug, description: description || null, thumbnailUrl: thumbnailUrl || null, price: Number(price), currency },
      );
      setTitle("");
      setSlug("");
      setDescription("");
      setThumbnailUrl("");
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
