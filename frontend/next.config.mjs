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

export default nextConfig;

// No withSentryConfig wrapper here — deliberately. It broke production three times
// over (auto-wrapping middleware.ts, then instrumentation.ts's unconditional import
// even behind a runtime-gated dynamic import), and reading further into the
// installed package (node_modules/@sentry/nextjs/build/cjs/config/webpack.js) showed
// why a fourth attempt at gating it wasn't worth trying: its webpack function pushes
// a DefinePlugin (__SENTRY_SERVER_MODULES__) and, in any non-dev build, the full
// @sentry/webpack-plugin instance onto EVERY webpack pass it runs, including edge —
// unconditionally, with no dependency on autoInstrumentMiddleware or any other flag
// this app controls. That's the only thing left in this codebase that touched the
// edge compilation regardless of what app source files contained.
//
// Sentry itself is not gone: instrumentation-client.ts (browser) and
// lib/sentry-node.ts (Node, imported from the root layout) both call the SDK
// directly — Sentry.init()/Sentry.captureException() have never depended on this
// webpack wrapper, only the automatic route-handler/server-component wrapping and
// source-map upload did. Losing those is an accepted, explicit trade for the site
// working at all; see docs/ARCHITECTURE.md's Error monitoring section.
