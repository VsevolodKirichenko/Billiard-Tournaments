# Billiard Tournaments

API для проведения бильярдных турниров, учёта тренировок и статистики игроков.

## Возможности

* Круговые турниры, генерация матчей и исправление результатов.
* Таблица турнира и общий рейтинг игроков.
* Регистрация, JWT и роли администратора, организатора, тренера и игрока.
* Журнал тренировок и отчёты CSV/Excel.
* Обработка результатов через Kafka с transactional outbox и inbox.
* Кэширование рейтингов в Redis и защита от конкурентной перезаписи результатов.

## Стек и структура

C#, .NET 10, ASP.NET Core, PostgreSQL, Entity Framework Core, Redis, Kafka, AutoMapper, xUnit, Moq, Testcontainers.

* `Tournaments.Core` — доменная логика, команды, запросы и интерфейсы.
* `Tournaments.Infrastructure` — PostgreSQL, миграции, Redis и Kafka.
* `Tournaments.Api` — HTTP API, авторизация, DTO и отчёты.

Модульный монолит с трёхслойной архитектурой и CQRS. Общий рейтинг обновляется асинхронно; таблица турнира доступна сразу после записи результата.

## Запуск

Требуются .NET SDK 10, PowerShell 7 и Docker с Linux containers.

```powershell
./scripts/dev.ps1 up
```

API: `http://127.0.0.1:5080`. OpenAPI: `/openapi/v1.json`.
Локальные учётные данные создаются в `.env`. Примеры запросов находятся в `requests.http`.

Для запуска API из Rider открой `BilliardTournaments.slnx`, выполни `./scripts/dev.ps1 infra` и выбери профиль `Tournaments.Api`.

```powershell
dotnet test BilliardTournaments.slnx
./scripts/smoke.ps1
./scripts/dev.ps1 down
```

Интеграционные тесты используют отдельные контейнеры PostgreSQL и Redis. Smoke сценарий выполняется на запущенном API.

## CI/CD

GitHub Actions выполняет сборку и тесты. Отдельные workflows публикуют Docker образ в GHCR и развёртывают выбранную версию через SSH. Для deploy нужны настройки environment и secrets из `.github/workflows/deploy.yml`.

AutoMapper: настройка лицензии через `AutoMapper__LicenseKey`, условия — [документация](https://docs.automapper.io/en/stable/License-configuration.html).
