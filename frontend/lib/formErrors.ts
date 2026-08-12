"use client";

// Turns the API's validation envelope into per-field, localized messages.
//
// The envelope already carried everything needed (see API_CONVENTIONS.md §4) — what was
// missing was a consumer. `ApiRequestError.message` is only `detail ?? title`, and a 400
// from FluentValidation sets no `detail`, so every caller that rendered `err.message`
// showed the constant string "Validation Failed" and dropped `info.errors` on the floor.

import { useCallback, useState } from "react";
import { ApiRequestError } from "@/lib/api";
import { Dictionary } from "@/lib/i18n";

/** Backend `errors` keys are PascalCase property names, e.g. "Slug", "Title". */
export type FieldName = string;

interface FormErrorState {
  fieldErrors: Record<FieldName, string[]>;
  formError: string | null;
}

const EMPTY: FormErrorState = { fieldErrors: {}, formError: null };

/**
 * Maps a validation error code to a key in the `validation` dictionary namespace.
 * Covers the explicit codes set via `.WithErrorCode(...)` plus the FluentValidation
 * defaults the admin forms can actually hit. Anything unmapped falls through to the
 * server's own (English) message rather than being swallowed.
 */
const CODE_TO_KEY: Record<string, keyof Dictionary["validation"]> = {
  // Explicit — see Application/Common/Constants/ValidationErrorCodes.cs
  slug_format: "slugFormat",
  slug_taken: "slugTaken",
  timestamp_not_utc: "timestampNotUtc",
  // FluentValidation defaults
  NotEmptyValidator: "required",
  NotNullValidator: "required",
  MaximumLengthValidator: "tooLong",
  LengthValidator: "tooLong",
  GreaterThanOrEqualValidator: "mustBeZeroOrMore",
  EmailValidator: "invalidEmail"
};

/**
 * `errors` and `errorCodes` are index-aligned per property (the middleware builds both from
 * one de-duplicated sequence), so message[i] and code[i] describe the same failure.
 */
export function localizeFieldErrors(
  info: { errors?: Record<string, string[]>; errorCodes?: Record<string, string[]> },
  dictionary: Dictionary
): Record<FieldName, string[]> {
  const localized: Record<FieldName, string[]> = {};

  for (const [field, messages] of Object.entries(info.errors ?? {})) {
    const codes = info.errorCodes?.[field] ?? [];
    localized[field] = messages.map((serverMessage, index) => {
      const key = CODE_TO_KEY[codes[index] ?? ""];
      return key ? dictionary.validation[key] : serverMessage;
    });
  }

  return localized;
}

export interface FormErrors {
  /** Messages for a PascalCase field name, or an empty array. */
  messagesFor: (field: FieldName) => string[];
  /** Form-level message for failures that name no field (404, 500, network). */
  formError: string | null;
  /** Records an error thrown by an `apiFetch` call. */
  capture: (error: unknown) => void;
  /** Reports a failure caught client-side, so a locally checkable rule renders exactly
   * like the server's version of the same rule instead of as a native browser bubble. */
  setFieldErrors: (errors: Record<FieldName, string[]>) => void;
  /** Clears one field's messages — call from the input's `onChange`. */
  clearField: (field: FieldName) => void;
  /** Clears everything — call at the start of a submit. */
  reset: () => void;
}

export function useFormErrors(dictionary: Dictionary): FormErrors {
  const [state, setState] = useState<FormErrorState>(EMPTY);

  const capture = useCallback(
    (error: unknown) => {
      if (error instanceof ApiRequestError) {
        const fieldErrors = localizeFieldErrors(error.info, dictionary);
        if (Object.keys(fieldErrors).length > 0) {
          setState({ fieldErrors, formError: null });
          return;
        }
        // A non-validation failure — `detail` carries a real, specific message
        // (e.g. "Course was not found."), so prefer it over generic copy.
        setState({
          fieldErrors: {},
          formError: error.info.detail ?? dictionary.validation.formError
        });
        return;
      }

      setState({ fieldErrors: {}, formError: dictionary.validation.formError });
    },
    [dictionary]
  );

  const setFieldErrors = useCallback((fieldErrors: Record<FieldName, string[]>) => {
    setState({ fieldErrors, formError: null });
  }, []);

  const clearField = useCallback((field: FieldName) => {
    setState((current) => {
      if (!current.fieldErrors[field]) return current;
      const remaining = { ...current.fieldErrors };
      delete remaining[field];
      return { ...current, fieldErrors: remaining };
    });
  }, []);

  const reset = useCallback(() => setState(EMPTY), []);

  const messagesFor = useCallback(
    (field: FieldName) => state.fieldErrors[field] ?? [],
    [state.fieldErrors]
  );

  return { messagesFor, formError: state.formError, capture, setFieldErrors, clearField, reset };
}
