# ---- Stage 1: Build ----
FROM node:22-alpine AS build

WORKDIR /app

# Copy docs and root CONTRIBUTING.md
COPY docs/ ./docs/
COPY CONTRIBUTING.md ./CONTRIBUTING.md

# Copy website source
COPY src/website/package*.json ./src/website/
WORKDIR /app/src/website
RUN npm ci

# Copy website source files
COPY src/website/ ./

# Run prebuild (generates static/text/*.txt) then Docusaurus build
RUN npm run build

# ---- Stage 2: Runtime ----
FROM node:22-alpine AS runtime

WORKDIR /app

# Copy the built site
COPY --from=build /app/src/website/build ./build

# Copy the Express server
COPY src/website/server.mjs ./server.mjs

# Install only production dependency (express)
RUN npm init -y > /dev/null 2>&1 && npm install express --save --production

EXPOSE 3000

ENV PORT=3000
ENV NODE_ENV=production

CMD ["node", "server.mjs"]
