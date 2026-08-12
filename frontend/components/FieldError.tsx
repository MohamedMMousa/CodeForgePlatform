"use client";

/**
 * Renders validation messages directly beneath the input that caused them.
 *
 * Pair it with `fieldErrorProps` so the input carries the matching `aria-invalid` and
 * `aria-describedby` — a message that is only visually adjacent is not announced to a
 * screen reader, and nothing in this repo checks that automatically.
 */
export function FieldError({ id, messages }: { id: string; messages: string[] }) {
  if (messages.length === 0) {
    return null;
  }

  return (
    <span className="field-err" id={id} role="alert">
      {messages.join(" ")}
    </span>
  );
}

/**
 * Spread onto the input: `<input {...fieldErrorProps("slug", messages)} />`.
 * `describedBy` chains an always-visible hint (e.g. the slug format rule) ahead of the
 * error, so both are announced.
 */
export function fieldErrorProps(
  id: string,
  messages: string[],
  describedBy?: string
): { "aria-invalid"?: true; "aria-describedby"?: string } {
  const ids = [describedBy, messages.length > 0 ? `${id}-error` : undefined]
    .filter(Boolean)
    .join(" ");

  return {
    ...(messages.length > 0 ? { "aria-invalid": true as const } : {}),
    ...(ids ? { "aria-describedby": ids } : {})
  };
}
