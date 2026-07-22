"use client";

import { use, useEffect, useState } from "react";
import { useAuth } from "@/lib/auth";
import { AdminUser, ApiRequestError, createInstructor, deactivateUser, getUsers, reactivateUser } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function AdminUsersPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).admin;

  const { session } = useAuth();
  const [users, setUsers] = useState<AdminUser[] | null>(null);
  const [roleFilter, setRoleFilter] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [creating, setCreating] = useState(false);

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  function load() {
    if (!session) return;
    getUsers({ role: roleFilter || undefined }, session.accessToken).then(setUsers).catch(onError);
  }

  useEffect(load, [session, roleFilter]);

  if (!session || session.role !== "admin") {
    return <p className="notice err">{getDictionary(locale).instructor.signInRequired}</p>;
  }

  const roleLabel = (r: string) => (r === "admin" ? t.roleAdmin : r === "instructor" ? t.roleInstructor : t.roleStudent);

  async function onCreateInstructor(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setCreating(true);
    setError(null);
    setNotice(null);
    try {
      await createInstructor({ fullName, email, phone: phone || undefined }, session.accessToken);
      setFullName("");
      setEmail("");
      setPhone("");
      setNotice(t.temporaryPasswordCreated);
      load();
    } catch (err) {
      onError(err);
    } finally {
      setCreating(false);
    }
  }

  async function onDeactivate(id: string) {
    if (!session) return;
    setBusyId(id);
    try {
      await deactivateUser(id, session.accessToken);
      load();
    } catch (err) {
      onError(err);
    } finally {
      setBusyId(null);
    }
  }

  async function onReactivate(id: string) {
    if (!session) return;
    setBusyId(id);
    try {
      await reactivateUser(id, session.accessToken);
      load();
    } catch (err) {
      onError(err);
    } finally {
      setBusyId(null);
    }
  }

  return (
    <>
      <h2>{t.usersTitle}</h2>
      {error && <p className="notice err">{error}</p>}
      {notice && <p className="notice">{notice}</p>}

      <div style={{ marginBottom: "1rem" }}>
        <label>
          {t.roleLabel}{" "}
          <select value={roleFilter} onChange={(e) => setRoleFilter(e.target.value)}>
            <option value="">{t.all}</option>
            <option value="admin">{t.roleAdmin}</option>
            <option value="instructor">{t.roleInstructor}</option>
            <option value="student">{t.roleStudent}</option>
          </select>
        </label>
      </div>

      {users === null && !error && <p className="muted">…</p>}
      {users !== null && users.length === 0 && <p className="muted">{t.noUsers}</p>}
      {users !== null && users.length > 0 && (
        <div style={{ overflowX: "auto", marginBottom: "1.5rem" }}>
          <table className="table">
            <thead>
              <tr>
                <th>{t.fullNameLabel}</th>
                <th>{t.emailLabel}</th>
                <th>{t.roleLabel}</th>
                <th>{t.isActiveLabel}</th>
                <th>{t.actions}</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id}>
                  <td>{u.fullName}</td>
                  <td>{u.email}</td>
                  <td>{roleLabel(u.role)}</td>
                  <td><span className="badge">{u.isActive ? t.active : t.inactive}</span></td>
                  <td>
                    {u.isActive ? (
                      <button className="btn secondary" disabled={busyId === u.id} onClick={() => onDeactivate(u.id)}>
                        {t.deactivate}
                      </button>
                    ) : (
                      <button className="btn secondary" disabled={busyId === u.id} onClick={() => onReactivate(u.id)}>
                        {t.reactivate}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <h3>{t.addInstructor}</h3>
      <form onSubmit={onCreateInstructor} style={{ display: "flex", flexDirection: "column", gap: "0.5rem", maxWidth: "24rem" }}>
        <label>
          {t.fullNameLabel}
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} required />
        </label>
        <label>
          {t.emailLabel}
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>
        <label>
          {t.phoneLabel}
          <input value={phone} onChange={(e) => setPhone(e.target.value)} />
        </label>
        <button className="btn" type="submit" disabled={creating}>
          {creating ? t.creating : t.addInstructor}
        </button>
      </form>
    </>
  );
}
