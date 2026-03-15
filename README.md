# BakeryFlow

BakeryFlow es la base de Fase 1 para administrar una panadería o pastelería con enfoque operativo:

- autenticación JWT
- usuarios almacenados en PostgreSQL con hash seguro
- maestros: categorías, productos, ingredientes, unidades, proveedores y clientes
- recetas y costeo
- compras
- inventario de materias primas
- producción simple
- ventas
- dashboard básico
- reportes básicos

## Estado actual

El backend cubre los módulos operativos y reglas de negocio principales con API .NET 8 + PostgreSQL.

La autenticación es propia del backend:

- no usa Firebase Auth
- no usa Auth0
- no usa proveedores externos
- el login se resuelve contra la API .NET
- la API emite JWT propios
- el endpoint `GET /api/auth/me` devuelve el usuario autenticado actual

El frontend Angular cubre:

- login y sesión
- administración de usuarios para Admin
- layout principal
- dashboard
- CRUD de maestros
- consulta de recetas y costeo
- inventario con existencias, movimientos y ajuste manual
- historial de compras, producción y ventas
- reportes básicos con exportación CSV

La API ya expone endpoints para usuarios, compras, recetas, producción, ventas, inventario, dashboard y reportes. La UI quedó preparada para seguir extendiendo formularios operativos complejos sin cambiar la arquitectura base.

## Stack

- Frontend: Angular, TypeScript, Angular Material, Reactive Forms, routing modular
- Backend: ASP.NET Core Web API, .NET 8, EF Core, PostgreSQL, FluentValidation, JWT, Swagger
- Infraestructura: Docker, Docker Compose, Nginx reverse proxy, GitHub Actions

## Estructura

```text
.
├── src
│   ├── backend
│   │   ├── BakeryFlow.Api
│   │   ├── BakeryFlow.Application
│   │   ├── BakeryFlow.Domain
│   │   └── BakeryFlow.Infrastructure
│   └── frontend
│       └── bakeryflow-app
├── deploy
│   ├── nginx
│   └── scripts
├── docker-compose.yml
├── docker-compose.prod.yml
└── .github/workflows/deploy.yml
```

## Requisitos

- .NET SDK 8
- Node.js 22+
- npm 11+
- Docker + Docker Compose plugin

## Variables de entorno

### Desarrollo

1. Crea `.env` a partir de `.env.example`.
2. Ajusta al menos:

```env
DEV_POSTGRES_DB=bakeryflow_dev
DEV_POSTGRES_USER=postgres
DEV_POSTGRES_PASSWORD=postgres
DEV_JWT_ISSUER=BakeryFlow.Dev
DEV_JWT_AUDIENCE=BakeryFlow.Dev
DEV_JWT_KEY=cambia-esto
ADMIN_EMAIL=admin@bakeryflow.local
ADMIN_NAME=Administrador
ADMIN_PASSWORD=Bakey2026*
```

`ADMIN_PASSWORD=Bakey2026*` es un valor temporal de arranque. Cámbialo antes de usar el sistema en un entorno real.

### Producción

1. Crea `.env.production` a partir de `.env.production.example`.
2. Ajusta:

```env
POSTGRES_DB=bakeryflow
POSTGRES_USER=bakeryflow
POSTGRES_PASSWORD=cambia-esto
JWT_ISSUER=BakeryFlow
JWT_AUDIENCE=BakeryFlow
JWT_KEY=clave-larga-y-segura
ADMIN_EMAIL=admin@deliciasbakery.shop
ADMIN_NAME=Administrador
ADMIN_PASSWORD=Bakey2026*
```

Si el usuario con `ADMIN_EMAIL` ya existe, BakeryFlow no lo duplica ni reescribe su contraseña al arrancar.

## Ejecución local

### Opción 1: Docker Compose

```bash
cp .env.example .env
docker compose up --build
```

Servicios:

- frontend: [http://localhost:4200](http://localhost:4200)
- backend: [http://localhost:5126/api/docs](http://localhost:5126/api/docs)
- postgres: `localhost:5432`

### Opción 2: local nativo

1. Levanta PostgreSQL.
2. Exporta variables para admin inicial y JWT.
3. Ejecuta:

```bash
export PATH="$HOME/.dotnet:$PATH"
cd src/backend
dotnet build BakeryFlow.sln
dotnet tool restore
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=bakeryflow_dev;Username=postgres;Password=postgres" \
Jwt__Issuer="BakeryFlow.Dev" \
Jwt__Audience="BakeryFlow.Dev" \
Jwt__Key="cambia-esto" \
ADMIN_EMAIL="admin@bakeryflow.local" \
ADMIN_NAME="Administrador" \
ADMIN_PASSWORD="Bakey2026*" \
dotnet run --project BakeryFlow.Api
```

En otra terminal:

```bash
cd src/frontend/bakeryflow-app
npm ci
npm start
```

## Base de datos y migraciones

## Usuarios y seguridad

- los usuarios se almacenan en PostgreSQL
- las contraseñas se guardan con `BCrypt`
- no existe registro público
- solo `Admin` puede acceder al módulo de usuarios y a `GET/POST/PUT/PATCH` de `/api/users`
- roles soportados: `Admin` y `Operator`
- al crear usuario desde el sistema se define nombre, email, rol, estado y contraseña
- existe cambio manual de contraseña desde el módulo de usuarios

### Administrador inicial

Al arrancar la API:

- si no existe el usuario con `ADMIN_EMAIL`, se crea automáticamente
- `ADMIN_NAME` es opcional y si no existe se usa `Administrador`
- `ADMIN_PASSWORD` puede omitirse y en ese caso se usa temporalmente `Bakey2026*`
- si el usuario ya existe, BakeryFlow no lo duplica
- si el usuario ya existe, BakeryFlow no reescribe su contraseña

Variables soportadas:

```env
ADMIN_EMAIL=admin@bakeryflow.local
ADMIN_NAME=Administrador
ADMIN_PASSWORD=Bakey2026*
```

`Bakey2026*` debe considerarse temporal y cambiarse en producción.

La migración inicial ya está creada en:

- [20260315222144_InitialCreate.cs](/Users/juanarias/Workspaces/Web/BakeryFlow/src/backend/BakeryFlow.Infrastructure/Persistence/Migrations/20260315222144_InitialCreate.cs)

Para aplicar migraciones manualmente:

```bash
export PATH="$HOME/.dotnet:$PATH:$HOME/.dotnet/tools"
cd src/backend
dotnet tool restore
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=bakeryflow_dev;Username=postgres;Password=postgres" \
dotnet ef database update --project BakeryFlow.Infrastructure/BakeryFlow.Infrastructure.csproj --startup-project BakeryFlow.Api/BakeryFlow.Api.csproj
```

## Subruta de producción

La publicación de producción está preparada para:

- frontend: `https://deliciasbakery.shop/bakeryFlow/`
- API: `https://deliciasbakery.shop/bakeryFlow/api/`

Claves técnicas:

- Angular build de producción usa `base href` y `deploy url` en `/bakeryFlow/`
- la API usa `App__PathBase=/bakeryFlow`
- Nginx reverse proxy enruta:
  - `/bakeryFlow/` al contenedor frontend
  - `/bakeryFlow/api/` al contenedor backend
- solo Nginx publica `80` y `443`
- frontend y backend usan puertos internos `8085` y `8086`
- PostgreSQL queda solo en la red interna Docker

## Despliegue manual en servidor Linux

1. Copia el proyecto o al menos:
   - repo completo
   - `.env.production`
2. Coloca los certificados TLS en:
   - `deploy/nginx/certs/fullchain.pem`
   - `deploy/nginx/certs/privkey.pem`
3. Ejecuta:

```bash
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build --remove-orphans
```

Verificación:

```bash
curl -I https://deliciasbakery.shop/bakeryFlow/
curl https://deliciasbakery.shop/bakeryFlow/api/health
```

## Cloudflare con deliciasbakery.shop

Configuración recomendada:

1. Crea un registro `A` o `AAAA` para `deliciasbakery.shop` apuntando al servidor.
2. Déjalo proxied con la nube naranja.
3. En Cloudflare usa `SSL/TLS -> Full (strict)`.
4. Genera un Cloudflare Origin Certificate y guárdalo en:
   - `deploy/nginx/certs/fullchain.pem`
   - `deploy/nginx/certs/privkey.pem`
5. Mantén abierto en el servidor solo `80` y `443`.
6. Valida:
   - `https://deliciasbakery.shop/bakeryFlow/`
   - `https://deliciasbakery.shop/bakeryFlow/api/health`

## GitHub Actions

Workflow:

- archivo: [.github/workflows/deploy.yml](/Users/juanarias/Workspaces/Web/BakeryFlow/.github/workflows/deploy.yml)
- dispara en push a `main` o `master`
- no ejecuta tests ni lint antes del deploy
- no builda en GitHub
- entra por SSH al VPS
- hace `git pull` en el repo ya clonado en servidor
- ejecuta `docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build --remove-orphans`
- limpia imágenes huérfanas con `docker image prune -f`

Secrets requeridos:

- `VPS_SSH_KEY_B64`
- `VPS_HOST`
- `VPS_USER`
- `VPS_PATH`

El deploy depende de que el VPS ya tenga:

- Docker
- Docker Compose plugin
- este repo clonado en `VPS_PATH`
- `.env.production` configurado en esa ruta
- el branch correcto activo en ese clon

Si haces push a otra rama, no se despliega.

## Verificación realizada

Comandos validados en este repo:

```bash
export PATH="$HOME/.dotnet:$PATH"
cd src/backend && dotnet build BakeryFlow.sln
cd src/frontend/bakeryflow-app && npm run build
```

Nota: en este entorno no había daemon Docker disponible, por eso no se validó `docker build` ni `docker compose up` localmente desde aquí.
