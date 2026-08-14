"use client";

import { Direction } from "radix-ui";

/**
 * Feeds the locale's direction to Radix.
 *
 * Setting `dir="rtl"` on `<html>` is necessary but not sufficient: Radix
 * resolves direction from React context, not from the DOM, and its floating
 * layers are portalled to `document.body` where they'd inherit nothing useful
 * anyway. Without this provider every portalled primitive — select panels,
 * dialogs, dropdowns, popovers — stamps `dir="ltr"` on itself and renders
 * mirrored the wrong way inside an otherwise correct Arabic page. Verified
 * directly: the select's check indicator sat on the right in `ar` until this
 * was added.
 *
 * Mounted once in `app/[locale]/layout.tsx`, alongside where `dir` is set on
 * `<html>`, so the two can't drift apart.
 */
export function DirectionProvider({
  dir,
  children
}: {
  dir: "ltr" | "rtl";
  children: React.ReactNode;
}) {
  return <Direction.Provider dir={dir}>{children}</Direction.Provider>;
}
