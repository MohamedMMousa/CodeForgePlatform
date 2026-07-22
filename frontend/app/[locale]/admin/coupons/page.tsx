"use client";

import { use, useEffect, useState } from "react";
import { useAuth } from "@/lib/auth";
import {
  AdminCoupon,
  ApiRequestError,
  createCoupon,
  deactivateCoupon,
  getCoupons,
  updateCoupon
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function AdminCouponsPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).admin;

  const { session } = useAuth();
  const [coupons, setCoupons] = useState<AdminCoupon[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [code, setCode] = useState("");
  const [type, setType] = useState("percent");
  const [value, setValue] = useState("10");
  const [validFrom, setValidFrom] = useState("");
  const [validUntil, setValidUntil] = useState("");
  const [usageLimit, setUsageLimit] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [saving, setSaving] = useState(false);

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  function load() {
    if (!session) return;
    getCoupons(undefined, session.accessToken).then(setCoupons).catch(onError);
  }

  useEffect(load, [session]);

  if (!session || session.role !== "admin") {
    return <p className="notice err">{getDictionary(locale).instructor.signInRequired}</p>;
  }

  function resetForm() {
    setEditingId(null);
    setCode("");
    setType("percent");
    setValue("10");
    setValidFrom("");
    setValidUntil("");
    setUsageLimit("");
    setIsActive(true);
  }

  function onEdit(c: AdminCoupon) {
    setEditingId(c.id);
    setCode(c.code);
    setType(c.type);
    setValue(String(c.value));
    setValidFrom(c.validFrom ? c.validFrom.slice(0, 10) : "");
    setValidUntil(c.validUntil ? c.validUntil.slice(0, 10) : "");
    setUsageLimit(c.usageLimit !== null ? String(c.usageLimit) : "");
    setIsActive(c.isActive);
  }

  async function onSave(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setSaving(true);
    setError(null);
    try {
      const shared = {
        type,
        value: Number(value),
        validFrom: validFrom ? new Date(validFrom).toISOString() : null,
        validUntil: validUntil ? new Date(validUntil).toISOString() : null,
        usageLimit: usageLimit ? Number(usageLimit) : null
      };
      if (editingId) {
        await updateCoupon(editingId, { ...shared, isActive }, session.accessToken);
      } else {
        await createCoupon({ code, ...shared }, session.accessToken);
      }
      resetForm();
      load();
    } catch (err) {
      onError(err);
    } finally {
      setSaving(false);
    }
  }

  async function onDeactivate(id: string) {
    if (!session) return;
    setBusyId(id);
    try {
      await deactivateCoupon(id, session.accessToken);
      load();
    } catch (err) {
      onError(err);
    } finally {
      setBusyId(null);
    }
  }

  return (
    <>
      <h2>{t.couponsTitle}</h2>
      {error && <p className="notice err">{error}</p>}

      {coupons === null && !error && <p className="muted">…</p>}
      {coupons !== null && coupons.length === 0 && <p className="muted">{t.noCoupons}</p>}
      {coupons !== null && coupons.length > 0 && (
        <div style={{ overflowX: "auto", marginBottom: "1.5rem" }}>
          <table className="table">
            <thead>
              <tr>
                <th>{t.code}</th>
                <th>{t.couponType}</th>
                <th>{t.value}</th>
                <th>{t.usedCount}</th>
                <th>{t.status}</th>
                <th>{t.actions}</th>
              </tr>
            </thead>
            <tbody>
              {coupons.map((c) => (
                <tr key={c.id}>
                  <td>{c.code}</td>
                  <td>{c.type === "percent" ? t.typePercent : t.typeFixed}</td>
                  <td>{c.value}{c.type === "percent" ? "%" : ""}</td>
                  <td>{c.usedCount}{c.usageLimit !== null ? ` / ${c.usageLimit}` : ""}</td>
                  <td><span className="badge">{c.isActive ? t.active : t.inactive}</span></td>
                  <td style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
                    <button className="btn secondary" onClick={() => onEdit(c)}>
                      {t.edit}
                    </button>
                    {c.isActive && (
                      <button className="btn secondary" disabled={busyId === c.id} onClick={() => onDeactivate(c.id)}>
                        {t.deactivate}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <h3>{editingId ? t.edit : t.addCoupon}</h3>
      <form onSubmit={onSave} style={{ display: "flex", flexDirection: "column", gap: "0.5rem", maxWidth: "24rem" }}>
        <label>
          {t.code}
          <input value={code} onChange={(e) => setCode(e.target.value)} required disabled={!!editingId} />
        </label>
        <label>
          {t.couponType}
          <select value={type} onChange={(e) => setType(e.target.value)}>
            <option value="percent">{t.typePercent}</option>
            <option value="fixed">{t.typeFixed}</option>
          </select>
        </label>
        <label>
          {t.value}
          <input type="number" min="0" value={value} onChange={(e) => setValue(e.target.value)} required />
        </label>
        <label>
          {t.validFrom}
          <input type="date" value={validFrom} onChange={(e) => setValidFrom(e.target.value)} />
        </label>
        <label>
          {t.validUntil}
          <input type="date" value={validUntil} onChange={(e) => setValidUntil(e.target.value)} />
        </label>
        <label>
          {t.usageLimit}
          <input type="number" min="0" value={usageLimit} onChange={(e) => setUsageLimit(e.target.value)} />
        </label>
        {editingId && (
          <label>
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />{" "}
            {t.active}
          </label>
        )}
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <button className="btn" type="submit" disabled={saving}>
            {saving ? t.saving : editingId ? t.save : t.create}
          </button>
          {editingId && (
            <button type="button" className="btn secondary" onClick={resetForm}>
              {t.cancel}
            </button>
          )}
        </div>
      </form>
    </>
  );
}
