# ---- Stage 1: Build website ----
FROM node:22-alpine AS website-build

WORKDIR /app

# Copy repository inputs consumed by website prebuild scripts
COPY docs/ ./docs/
COPY CONTRIBUTING.md ./CONTRIBUTING.md
COPY src/cli/Mappers/ ./src/cli/Mappers/
COPY tests/fixtures/ ./tests/fixtures/

# Copy website source
COPY src/website/package*.json ./src/website/
WORKDIR /app/src/website
RUN apk add --no-cache curl
RUN npm ci
RUN curl -fsSL https://microsoft.github.io/docfind/install.sh | sh

# Copy website source files
COPY src/website/ ./

# Run prebuild (generates static/text/*.txt) then Docusaurus build
RUN npm run build

# ---- Stage 2: Build CLI ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS cli-build

WORKDIR /app

COPY src/cli/ ./src/cli/

RUN dotnet publish src/cli/washington.csproj \
	--configuration Release \
	-r linux-x64 \
	-p:PublishSingleFile=true \
	--self-contained true \
	-o /out

# ---- Stage 3: Runtime ----
FROM node:22-bookworm-slim AS runtime

WORKDIR /app

# Copy the built site
COPY --from=website-build /app/src/website/build ./build

# Copy the Express server
COPY src/website/server.mts ./server.mts
COPY src/website/docsRuntime.mts ./docsRuntime.mts
COPY src/website/sshDocsServer.mts ./sshDocsServer.mts
COPY --from=cli-build /out/bce ./bin/bce

# Install only production dependency (express)
RUN apt-get update \
	&& apt-get install --yes --no-install-recommends ca-certificates libicu72 libssl3 \
	&& rm -rf /var/lib/apt/lists/* \
	&& chmod +x ./bin/bce \
	&& npm init -y > /dev/null 2>&1 \
	&& npm install express ssh2 tsx --save --production

EXPOSE 3000
EXPOSE 2222

ENV PORT=3000
ENV NODE_ENV=production
ENV BCE_BINARY_PATH=/app/bin/bce

CMD ["node", "--import", "tsx", "server.mts"]
