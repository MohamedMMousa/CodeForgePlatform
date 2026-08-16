"use client";

import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ApiRequestError, MaterialItem, downloadAuthenticatedFile } from "@/lib/api";
import { format, type Dictionary, type Locale } from "@/lib/i18n";
import { formatFileSize } from "@/lib/format";
import { externalHref } from "@/lib/url";

type Props = {
  materials: MaterialItem[];
  emptyText: string;
  locale: Locale;
  t: Dictionary["courseContent"];
};

function typeLabel(type: string, t: Dictionary["courseContent"]): string {
  if (type === "file") return t.materialTypeFile;
  if (type === "link") return t.materialTypeLink;
  return t.materialTypeText;
}

// Handles all three MaterialDto.type values — the one piece of surface #5
// that is genuinely "content", and it's plain data, not a rich body: a file
// download, an external link, or a short text note. Shared between a
// module's resources and a session's materials, both fetched through this
// same DTO shape.
export function MaterialList({ materials, emptyText, locale, t }: Props) {
  const [downloadErrors, setDownloadErrors] = useState<Record<string, string>>({});

  if (materials.length === 0) {
    return <p className="text-body text-text-muted">{emptyText}</p>;
  }

  function download(material: MaterialItem) {
    if (!material.fileDownloadUrl) return;
    setDownloadErrors((prev) => {
      if (!(material.id in prev)) return prev;
      const next = { ...prev };
      delete next[material.id];
      return next;
    });
    // fileDownloadUrl is a relative API path behind [Authorize], never a
    // plain href — downloadAuthenticatedFile fetches with the auth cookie
    // and opens the result as a blob (lib/api.ts).
    downloadAuthenticatedFile(material.fileDownloadUrl).catch((err) => {
      setDownloadErrors((prev) => ({
        ...prev,
        [material.id]: err instanceof ApiRequestError ? err.message : t.loadError
      }));
    });
  }

  return (
    <ul className="flex flex-col gap-3">
      {materials.map((material) => (
        <li
          key={material.id}
          className="flex flex-col items-start gap-2 rounded-card border border-border bg-surface p-4"
        >
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="neutral">{typeLabel(material.type, t)}</Badge>
            <span className="text-body font-semibold text-text">{material.title}</span>
          </div>

          {material.type === "text" && material.body ? (
            <p className="whitespace-pre-line text-body text-text-secondary">{material.body}</p>
          ) : null}

          {material.type === "link" && material.linkUrl ? (
            <Button asChild variant="ghost" size="sm" className="w-fit">
              <a href={externalHref(material.linkUrl)} target="_blank" rel="noreferrer">
                {t.openLink}
              </a>
            </Button>
          ) : null}

          {material.type === "file" && material.fileDownloadUrl ? (
            <>
              {material.fileType || material.fileSizeKb != null ? (
                <p className="text-meta text-text-muted">
                  {format(t.fileMeta, {
                    type: material.fileType?.toUpperCase() ?? "",
                    size:
                      material.fileSizeKb != null
                        ? formatFileSize(material.fileSizeKb, locale)
                        : ""
                  })}
                </p>
              ) : null}
              <Button variant="secondary" size="sm" onClick={() => download(material)}>
                {t.downloadFile}
              </Button>
              {downloadErrors[material.id] ? (
                <p className="text-meta text-danger">{downloadErrors[material.id]}</p>
              ) : null}
            </>
          ) : null}
        </li>
      ))}
    </ul>
  );
}
