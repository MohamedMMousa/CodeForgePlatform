"use client";

import { useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { BookOpen, CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { FieldError, fieldErrorProps } from "@/components/FieldError";
import { useFormErrors } from "@/lib/formErrors";
import {
  ApiRequestError,
  CouponValidationResult,
  submitEnrollmentRequest,
  validateCoupon
} from "@/lib/api";
import { defaultLocale, format, getDictionary, isLocale } from "@/lib/i18n";

export default function EnrollForm({ locale: rawLocale }: { locale: string }) {
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.enroll;
  // Overrides the hook's generic fallback with enroll's own, more specific
  // "could not submit" copy — kept as the non-field-error message rather than
  // dropped in favor of the app-wide generic string.
  const formErrors = useFormErrors({
    ...dictionary,
    validation: { ...dictionary.validation, formError: t.error }
  });

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
  const [success, setSuccess] = useState(false);

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

    formErrors.reset();
    setBusy(true);
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
      setSuccess(true);
    } catch (error) {
      formErrors.capture(error);
    } finally {
      setBusy(false);
    }
  }

  if (!courseId && !trackId) {
    return (
      <main data-theme="light" className="min-h-screen bg-bg [&_:is(h1,h2,h3,p)]:m-0">
        <div className="mx-auto flex w-full max-w-2xl flex-col gap-6 ps-5 pe-5 py-16">
          <div className="flex flex-col items-center gap-4 rounded-card border border-border bg-surface p-10 text-center">
            <BookOpen className="size-8 text-text-muted" aria-hidden="true" />
            <p className="text-body text-text-muted">{t.missingTarget}</p>
            <Button asChild variant="secondary">
              <Link href={`/${locale}/catalog`}>{dictionary.home.browseCourses}</Link>
            </Button>
          </div>
        </div>
      </main>
    );
  }

  if (success) {
    return (
      <main data-theme="light" className="min-h-screen bg-bg [&_:is(h1,h2,h3,p)]:m-0">
        <div className="mx-auto flex w-full max-w-2xl flex-col gap-6 ps-5 pe-5 py-16">
          <div className="flex flex-col items-center gap-4 rounded-card border border-success-border bg-success-soft p-10 text-center">
            <CheckCircle2 className="size-8 text-success" aria-hidden="true" />
            <p className="text-body text-text">{t.success}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main data-theme="light" className="min-h-screen bg-bg [&_:is(h1,h2,h3,p)]:m-0">
      <div className="mx-auto flex w-full max-w-2xl flex-col gap-8 ps-5 pe-5 py-10">
        <h1 className="text-h1 text-text">{format(t.title, { name })}</h1>

        <Card>
          <CardContent>
            <form onSubmit={onSubmit} className="flex flex-col gap-5">
              <div className="flex flex-col gap-1.5">
                <label htmlFor="fullName" className="text-label text-text">
                  {t.fullName}
                </label>
                <Input
                  id="fullName"
                  value={fullName}
                  onChange={(e) => {
                    setFullName(e.target.value);
                    formErrors.clearField("FullName");
                  }}
                  disabled={busy}
                  required
                  {...fieldErrorProps("fullName", formErrors.messagesFor("FullName"))}
                />
                <FieldError id="fullName-error" messages={formErrors.messagesFor("FullName")} />
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="email" className="text-label text-text">
                  {t.email}
                </label>
                <Input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value);
                    formErrors.clearField("Email");
                  }}
                  disabled={busy}
                  required
                  {...fieldErrorProps("email", formErrors.messagesFor("Email"))}
                />
                <FieldError id="email-error" messages={formErrors.messagesFor("Email")} />
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="phone" className="text-label text-text">
                  {t.phone}
                </label>
                <Input
                  id="phone"
                  value={phone}
                  onChange={(e) => {
                    setPhone(e.target.value);
                    formErrors.clearField("PhoneNumber");
                  }}
                  disabled={busy}
                  {...fieldErrorProps("phone", formErrors.messagesFor("PhoneNumber"))}
                />
                <FieldError id="phone-error" messages={formErrors.messagesFor("PhoneNumber")} />
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="paymentMethod" className="text-label text-text">
                  {t.paymentMethod}
                </label>
                <Input
                  id="paymentMethod"
                  placeholder={t.paymentMethodPlaceholder}
                  value={paymentMethod}
                  onChange={(e) => {
                    setPaymentMethod(e.target.value);
                    formErrors.clearField("PaymentMethod");
                  }}
                  disabled={busy}
                  required
                  {...fieldErrorProps("paymentMethod", formErrors.messagesFor("PaymentMethod"))}
                />
                <FieldError
                  id="paymentMethod-error"
                  messages={formErrors.messagesFor("PaymentMethod")}
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="coupon" className="text-label text-text">
                  {t.couponCode} <span className="text-text-muted">({t.couponOptional})</span>
                </label>
                <div className="flex gap-2">
                  <Input
                    id="coupon"
                    value={couponCode}
                    onChange={(e) => {
                      setCouponCode(e.target.value);
                      setCoupon(null);
                      setCouponError(null);
                    }}
                    disabled={busy}
                    className="flex-1"
                  />
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={onApplyCoupon}
                    disabled={couponBusy || busy || !couponCode.trim()}
                  >
                    {t.applyCoupon}
                  </Button>
                </div>
                {coupon?.valid ? (
                  <p className="text-body text-text-secondary">
                    {format(t.couponApplied, {
                      label:
                        coupon.type === "percent"
                          ? `${coupon.value}%`
                          : `${coupon.value} ${currency}`
                    })}
                  </p>
                ) : null}
                {couponError ? (
                  <p role="alert" className="text-body text-danger">
                    {couponError}
                  </p>
                ) : null}
              </div>

              <div className="flex flex-col gap-1 rounded-card border border-border bg-surface-2 p-4">
                <p className="text-body text-text-secondary">
                  {t.priceOriginal}: {price} {currency}
                </p>
                <p className="text-h3 font-bold text-text">
                  {t.priceFinal}: {finalPrice} {currency}
                </p>
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="proof" className="text-label text-text">
                  {t.paymentProof}
                </label>
                <Input
                  id="proof"
                  type="file"
                  accept="image/jpeg,image/png,image/webp,application/pdf"
                  onChange={(e) => {
                    setProofFile(e.target.files?.[0] ?? null);
                    formErrors.clearField("PaymentProof");
                  }}
                  disabled={busy}
                  required
                  {...fieldErrorProps("proof", formErrors.messagesFor("PaymentProof"))}
                />
                <span className="text-meta text-text-muted">{t.paymentProofHint}</span>
                <FieldError id="proof-error" messages={formErrors.messagesFor("PaymentProof")} />
              </div>

              {formErrors.formError ? (
                <p
                  role="alert"
                  className="rounded-card border border-danger-border bg-danger-soft p-3 text-body text-danger"
                >
                  {formErrors.formError}
                </p>
              ) : null}

              <Button type="submit" disabled={busy} className="w-full">
                {busy ? t.submitting : t.submit}
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </main>
  );
}
