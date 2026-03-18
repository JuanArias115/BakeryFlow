FROM node:22-alpine AS build
WORKDIR /app

ARG BUILD_CONFIGURATION=production

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration ${BUILD_CONFIGURATION}
RUN set -e; \
    mkdir -p /tmp/frontend-dist; \
    if [ -d /app/dist/bakeryflow-app/browser ]; then \
      cp -R /app/dist/bakeryflow-app/browser/. /tmp/frontend-dist/; \
    else \
      cp -R /app/dist/bakeryflow-app/. /tmp/frontend-dist/; \
    fi

FROM nginx:1.27-alpine AS runtime
WORKDIR /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
RUN mkdir -p /usr/share/nginx/html/bakeryFlow
COPY --from=build /tmp/frontend-dist/ /usr/share/nginx/html/
COPY --from=build /tmp/frontend-dist/ /usr/share/nginx/html/bakeryFlow/

EXPOSE 8085
