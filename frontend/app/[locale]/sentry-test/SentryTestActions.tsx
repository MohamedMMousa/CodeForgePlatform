"use client";

import { useState } from "react";
import * as Sentry from "@sentry/nextjs";
import { sendServerSentryTestError } from "./actions";

export default function SentryTestActions({
  clientButtonLabel,
  clientSentLabel,
  serverButtonLabel,
  serverSentLabel
}: {
  clientButtonLabel: string;
  clientSentLabel: string;
  serverButtonLabel: string;
  serverSentLabel: string;
}) {
  const [clientSent, setClientSent] = useState(false);
  const [serverSent, setServerSent] = useState(false);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem", marginTop: "1rem" }}>
      <button
        type="button"
        className="btn"
        onClick={() => {
          Sentry.captureException(new Error("Sentry test error triggered via the frontend client."));
          setClientSent(true);
        }}
      >
        {clientButtonLabel}
      </button>
      {clientSent && <p className="muted">{clientSentLabel}</p>}

      <button
        type="button"
        className="btn"
        onClick={async () => {
          await sendServerSentryTestError();
          setServerSent(true);
        }}
      >
        {serverButtonLabel}
      </button>
      {serverSent && <p className="muted">{serverSentLabel}</p>}
    </div>
  );
}
