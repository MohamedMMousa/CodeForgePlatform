"use client";

import * as React from "react";
import { Dialog as DialogPrimitive } from "radix-ui";

import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { XIcon } from "lucide-react";

// A dialog is a floating layer, so §2.4 allows it the one subtle shadow. Panel
// radius is the card value (12); the surface, border and text all come from the
// active theme, so a dialog opened from a light work surface is light and one
// opened from the dark catalog is dark, with no per-call configuration.
//
// RTL: the centring transform has to be mirrored explicitly. `start-1/2` moves
// the panel by the inline start edge, which flips with direction, so the
// compensating translate has to flip too — hence the `rtl:` pair below. This is
// the one place a direction-aware override is unavoidable, because a transform
// has no logical equivalent.

function Dialog({ ...props }: React.ComponentProps<typeof DialogPrimitive.Root>) {
  return <DialogPrimitive.Root data-slot="dialog" {...props} />;
}

function DialogTrigger({
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Trigger>) {
  return <DialogPrimitive.Trigger data-slot="dialog-trigger" {...props} />;
}

function DialogPortal({
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Portal>) {
  return <DialogPrimitive.Portal data-slot="dialog-portal" {...props} />;
}

function DialogClose({
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Close>) {
  return <DialogPrimitive.Close data-slot="dialog-close" {...props} />;
}

function DialogOverlay({
  className,
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Overlay>) {
  return (
    <DialogPrimitive.Overlay
      data-slot="dialog-overlay"
      className={cn(
        "fixed inset-0 isolate z-50 bg-scrim duration-100",
        "supports-backdrop-filter:backdrop-blur-xs",
        "data-open:animate-in data-open:fade-in-0 data-closed:animate-out data-closed:fade-out-0",
        className
      )}
      {...props}
    />
  );
}

function DialogContent({
  className,
  children,
  showCloseButton = true,
  closeLabel = "Close",
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Content> & {
  showCloseButton?: boolean;
  /**
   * Accessible name for the close control. Pass the caller's translated string
   * — every user-facing string ships in both `en` and `ar` (§2.5), and a
   * screen-reader label is user-facing. The English default is a fallback for
   * a dialog opened outside a locale context, not a licence to leave it.
   */
  closeLabel?: string;
}) {
  return (
    <DialogPortal>
      <DialogOverlay />
      <DialogPrimitive.Content
        data-slot="dialog-content"
        className={cn(
          "fixed top-1/2 start-1/2 z-50 grid w-full max-w-[calc(100%-2rem)] sm:max-w-md",
          "-translate-x-1/2 rtl:translate-x-1/2 -translate-y-1/2",
          "gap-5 rounded-card border border-border bg-surface p-5 text-body text-text",
          "shadow-overlay duration-100 outline-none",
          "data-open:animate-in data-open:fade-in-0 data-open:zoom-in-95",
          "data-closed:animate-out data-closed:fade-out-0 data-closed:zoom-out-95",
          // Reserve the close button's footprint on the header's inline end, so
          // a long title can't run underneath it. The header's block box spans
          // the full panel width, so without this the collision is silent until
          // a title happens to be long enough — and Arabic titles run toward the
          // same edge the button sits on.
          showCloseButton && "[&_[data-slot=dialog-header]]:pe-10",
          className
        )}
        {...props}
      >
        {children}
        {showCloseButton && (
          <DialogPrimitive.Close data-slot="dialog-close" asChild>
            {/* `end-3` is logical: the close control sits top-right in English
                and top-left in Arabic. The X glyph itself is non-directional. */}
            <Button
              variant="ghost"
              size="icon-sm"
              className="absolute top-3 end-3 text-text-muted"
            >
              <XIcon />
              <span className="sr-only">{closeLabel}</span>
            </Button>
          </DialogPrimitive.Close>
        )}
      </DialogPrimitive.Content>
    </DialogPortal>
  );
}

function DialogHeader({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="dialog-header"
      className={cn("flex flex-col gap-2 text-start", className)}
      {...props}
    />
  );
}

function DialogFooter({
  className,
  children,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="dialog-footer"
      className={cn(
        "-mx-5 -mb-5 flex flex-col-reverse gap-3 rounded-b-card border-t border-border",
        "bg-surface-2 p-5 sm:flex-row sm:justify-end",
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}

function DialogTitle({
  className,
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Title>) {
  return (
    <DialogPrimitive.Title
      data-slot="dialog-title"
      className={cn("text-h3 text-text", className)}
      {...props}
    />
  );
}

function DialogDescription({
  className,
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Description>) {
  return (
    <DialogPrimitive.Description
      data-slot="dialog-description"
      className={cn("text-body text-text-muted", className)}
      {...props}
    />
  );
}

export {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogOverlay,
  DialogPortal,
  DialogTitle,
  DialogTrigger
};
