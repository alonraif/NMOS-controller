FROM node:20-alpine AS build
WORKDIR /app

COPY src/frontend/nmos-controller-ui/package.json src/frontend/nmos-controller-ui/package-lock.json ./
RUN npm ci

COPY src/frontend/nmos-controller-ui ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY docker/frontend/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
