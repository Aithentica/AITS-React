# syntax=docker/dockerfile:1

FROM node:20-alpine AS build
WORKDIR /app

# Kopiuj pliki package.json i package-lock.json dla lepszego cache'owania
COPY package*.json ./

# Instalacja zależności (cache'owane jeśli package*.json się nie zmieniły)
RUN npm ci --only=production=false

# Kopiuj resztę plików źródłowych
COPY . .

# Build aplikacji
RUN npm run build

# Etap produkcyjny - tylko nginx z zbudowaną aplikacją
FROM nginx:1.27-alpine AS final
WORKDIR /usr/share/nginx/html

# Kopiuj zbudowaną aplikację z etapu build
COPY --from=build /app/dist /usr/share/nginx/html

# Kopiuj konfigurację nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]




