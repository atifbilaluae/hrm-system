# syntax=docker/dockerfile:1

FROM node:20-alpine AS base
WORKDIR /app
# libc6-compat: some Next.js native deps expect glibc symbols on Alpine.
RUN apk add --no-cache libc6-compat

# ---------- Dependencies ----------
FROM base AS deps
COPY package.json package-lock.json* ./
# npm ci when a lockfile exists (reproducible), npm install otherwise.
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

# ---------- Development image (used by docker compose) ----------
# Source is bind-mounted at runtime, so editing a .tsx file hot-reloads
# the browser without any rebuild.
FROM base AS dev
ENV NODE_ENV=development \
    NEXT_TELEMETRY_DISABLED=1 \
    WATCHPACK_POLLING=true
COPY --from=deps /app/node_modules ./node_modules
COPY . .
EXPOSE 3000
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD wget --quiet --spider http://localhost:3000 || exit 1
CMD ["npm", "run", "dev"]

# ---------- Production build ----------
FROM base AS builder
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=deps /app/node_modules ./node_modules
COPY . .
# NEXT_PUBLIC_* values are inlined at build time, so this must be the URL the
# BROWSER will use - not an internal Docker hostname.
ARG NEXT_PUBLIC_API_URL=http://localhost:8080
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
RUN npm run build

# ---------- Production runtime ----------
FROM base AS prod
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000
RUN addgroup -g 1001 -S nodejs && adduser -S nextjs -u 1001
COPY --from=builder /app/public ./public
# "standalone" output bundles only the files the server actually needs.
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
USER nextjs
EXPOSE 3000
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --quiet --spider http://localhost:3000 || exit 1
CMD ["node", "server.js"]
