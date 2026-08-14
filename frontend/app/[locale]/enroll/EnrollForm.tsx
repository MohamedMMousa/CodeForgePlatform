"use client";

import { useState } from "react";
import { useSearchParams } from "next/navigation";
import {
  ApiRequestError,
  CouponValidationResult,
  submitEnrollmentRequest,
  validateCoupon
} from "@/lib/api";
import { defaultLocale, format, getDictionary, isLocale } from "@/lib/i18n";

export default function EnrollForm({ locale: rawLocale }: { locale: string }) {
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).enroll;

  const searchParams = useSearchParams();
  const courseId = searchParams.get("courseId") ?? undefined;
  const trackId = searchParams.get("trackId") ?? undefined;
  const name = searchParams.get("name") ?? "";
  const price = Number(searchParams.get("price") ?? 0);
  const currency = searchParams.get("currency") ?? "EGP";

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("");
  const [couponCode, setCouponCode] = useState("");
  const [coupon, setCoupon] = useState<CouponValidationResult | null>(null);
  const [couponBusy, setCouponBusy] = useState(false);
  const [couponError, setCouponError] = useState<string | null>(null);
  const [proofFile, setProofFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<{ ok: boolean; text: string } | null>(null);

  const finalPrice = coupon?.valid ? coupon.finalPrice : price;

  async function onApplyCoupon() {
    if (!couponCode.trim()) return;
    setCouponBusy(true);
    setCouponError(null);
    try {
      const validation = await validateCoupon(couponCode.trim(), { courseId, trackId });
      setCoupon(validation);
      if (!validation.valid) {
        setCouponError(validation.message ?? t.couponInvalid);
      }
    } catch (error) {
      setCoupon(null);
      setCouponError(error instanceof ApiRequestError ? error.message : t.couponInvalid);
    } finally {
      setCouponBusy(false);
    }
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!proofFile) return;

    setBusy(true);
    setResult(null);
    try {
      await submitEnrollmentRequest({
        fullName,
        email,
        phoneNumber: phone || undefined,
        courseId,
        trackId,
        paymentMethod,
        couponCode: coupon?.valid ? couponCode.trim() : undefined,
        paymentProof: proofFile,
        locale
      });
      setResult({ ok: true, text: t.success });
    } catch (error) {
      const text =
        error instanceof ApiRequestError && error.info.errors
          ? Object.values(error.info.errors).flat().join(" ")
          : (error as Error).message || t.error;
      setResult({ ok: false, text });
    } finally {
      setBusy(false);
    }
  }

  if (!courseId && !trackId) {
    return (
      <main className="cf-container">
        <div className="notice err">{t.error}</div>
      </main>
    );
  }

  if (result?.ok) {
    return (
      <main className="cf-container">
        <div className="card">
          <div className="notice ok">{result.text}</div>
        </div>
      </main>
    );
  }

  return (
    <main className="cf-container">
      <div className="card" style={{ maxWidth: 560, margin: "0 auto" }}>
        <h1>{format(t.title, { name })}</h1>

        <form onSubmit={onSubmit}>
          <div className="field">
            <label htmlFor="fullName">{t.fullName}</label>
            <input id="fullName" value={fullName} onChange={(e) => setFullName(e.target.value)} required />
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
            <label htmlFor="paymentMethod">{t.paymentMethod}</label>
            <input
              id="paymentMethod"
              placeholder={t.paymentMethodPlaceholder}
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              required
            />
          </div>

          <div className="field">
            <label htmlFor="coupon">
              {t.couponCode} <span className="muted">({t.couponOptional})</span>
            </label>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <input
                id="coupon"
                value={couponCode}
                onChange={(e) => {
                  setCouponCode(e.target.value);
                  setCoupon(null);
                  setCouponError(null);
                }}
                style={{ flex: 1 }}
              />
              <button
                type="button"
                className="btn secondary"
                onClick={onApplyCoupon}
                disabled={couponBusy || !couponCode.trim()}
              >
                {t.applyCoupon}
              </button>
            </div>
            {coupon?.valid && (
              <p className="muted">
                {format(t.couponApplied, {
                  label: coupon.type === "percent" ? `${coupon.value}%` : `${coupon.value} ${currency}`
                })}
              </p>
            )}
            {couponError && <div className="notice err">{couponError}</div>}
          </div>

          <div className="card" style={{ marginBottom: "1rem" }}>
            <p>
              {t.priceOriginal}: {price} {currency}
            </p>
            <p className="price">
              {t.priceFinal}: {finalPrice} {currency}
            </p>
          </div>

          <div className="field">
            <label htmlFor="proof">{t.paymentProof}</label>
            <input
              id="proof"
              type="file"
              accept="image/jpeg,image/png,image/webp,application/pdf"
              onChange={(e) => setProofFile(e.target.files?.[0] ?? null)}
              required
            />
            <span className="muted">{t.paymentProofHint}</span>
          </div>

          <button className="btn" type="submit" disabled={busy}>
            {busy ? t.submitting : t.submit}
          </button>

          {result && !result.ok && <div className="notice err">{result.text}</div>}
        </form>
      </div>
    </main>
  );
}
