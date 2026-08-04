import { withSentryConfig } from "@sentry/nextjs";

// Server-only — never NEXT_PUBLIC_, so it's never bundled into browser JS. The
// browser only ever calls the relative /api/* prefix (see lib/api.ts); Next's own
// server rewrites that to the real API so auth cookies stay first-party. Dev and
// prod use the identical model — only this destination differs per environment.
const API_INTERNAL_URL = process.env.API_INTERNAL_URL ?? "http://localhost:5205";

/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${API_INTERNAL_URL}/:path*` }];
  }
};

export default withSentryConfig(nextConfig, {
  org: process.env.SENTRY_ORG,
  project: process.env.SENTRY_PROJECT,
  authToken: process.env.SENTRY_AUTH_TOKEN,
  silent: true,
  widenClientFileUpload: true
  // tunnelRoute intentionally NOT set: this app already proxies /api/* through Next
  // for the auth-cookie topology above, and a tunnel route would add a second,
  // competing rewrite through the same origin. Source-map upload is a no-op without
  // SENTRY_AUTH_TOKEN/SENTRY_ORG/SENTRY_PROJECT set (all optional, unset in dev/CI).
});
