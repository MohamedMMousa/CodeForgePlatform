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
