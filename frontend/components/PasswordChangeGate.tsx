"use client";

// Redirects to the forced change-password screen whenever the current session says
// mustChangePassword, and backstops a stale/tampered localStorage session by also
// listening for the 403 the API sends on any other blocked call (see lib/api.ts).

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth";
import { PASSWORD_CHANGE_REQUIRED_EVENT } from "@/lib/api";

export function PasswordChangeGate({ locale }: { locale: string }) {
  const { session } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const changePasswordPath = `/${locale}/change-password`;

  useEffect(() => {
    if (session?.mustChangePassword && pathname !== changePasswordPath) {
      router.replace(changePasswordPath);
    }
  }, [session, pathname, changePasswordPath, router]);

  useEffect(() => {
    function onPasswordChangeRequired() {
      if (pathname !== changePasswordPath) {
        router.replace(changePasswordPath);
      }
    }
    window.addEventListener(PASSWORD_CHANGE_REQUIRED_EVENT, onPasswordChangeRequired);
    return () =>
      window.removeEventListener(PASSWORD_CHANGE_REQUIRED_EVENT, onPasswordChangeRequired);
  }, [pathname, changePasswordPath, router]);

  return null;
}
