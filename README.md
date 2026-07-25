# Accounts API

API de contas e autenticação construída em ASP.NET Core 8, com persistência em PostgreSQL, autenticação JWT, publicação de eventos em RabbitMQ e logging em DynamoDB.

## Stack

- .NET 8 (ASP.NET Core Web API)
- Entity Framework Core + Npgsql (PostgreSQL)
- JWT ******
- FluentValidation
- RabbitMQ.Client
- Prometheus (`/metrics`) e Health Checks (`/health`)
- Swagger e ReDoc para documentação da API

## Estrutura do repositório

- `AccountsApi/`: projeto Web API (controllers, middlewares, configuração e bootstrap)
- `Core/`: domínio (entidades, contratos, DTOs e modelos)
- `Infrastructure/`: acesso a dados (DbContext, mapeamentos, repositórios e migrations)

## Funcionalidades principais

- Login e geração de token JWT (`POST /api/auth`)
- Validação de token para integração com outros serviços (`POST /api/auth/validate`)
- Cadastro público de usuário com perfil `Donor` (`POST /api/account/register`)
- CRUD de usuários para perfil `Admin` (`/api/account`)
- Publicação de evento `user.created` em exchange topic `users.events`
- Logging estruturado com middleware e persistência em DynamoDB

## Pré-requisitos

- .NET SDK 8.0
- Docker e Docker Compose

## Configuração de ambiente

1. Copie o arquivo de exemplo:

```bash
cp .env.example .env
```

2. Preencha as variáveis necessárias no `.env`:

- `ASPNETCORE_ENVIRONMENT`
- `DB_CONNECTION_STRING`
- `PG_USER`
- `PG_PASSWORD`
- `Jwt__Key`
- `DynamoDb__LogTableName`
- `DynamoDb__UseLocal`
- `DynamoDb__LocalUrl`
- `DynamoDb__Region`
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_DEFAULT_REGION`

## Executando com Docker

Use os dois arquivos de compose em conjunto:

```bash
docker compose -f docker-compose.local.yaml -f docker-compose.api.yaml up --build
```

Observação: o serviço da API depende de `rabbitmq` e `dynamodb-local` na rede `app-network`.

## Executando localmente com .NET

```bash
dotnet restore AccountsApi.sln
dotnet build AccountsApi.sln
dotnet run --project AccountsApi/AccountsApi.csproj
```

No ambiente `Development`, as migrations do Entity Framework são aplicadas automaticamente no startup.

## Endpoints úteis

- Swagger UI: `http://localhost:5015/swagger`
- ReDoc: `http://localhost:5015/redoc` (via middleware `UseReDoc`)
- Health Check: `http://localhost:5015/health`
- Prometheus Metrics: `http://localhost:5015/metrics`
