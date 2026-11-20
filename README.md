# FinTrack

Настольное приложение для учёта личных финансов: счета, операции, категории, инвестиционные портфели, цели, прогноз расходов и чистый капитал.

Курсовой проект по дисциплине «Разработка программных модулей».

## Стек

C#, .NET, WPF (MVVM), EF Core + SQLite, CommunityToolkit.Mvvm, LiveCharts2, xUnit.

## Структура

```
src/
  FinTrack.Domain/          сущности, перечисления, расчёты
  FinTrack.Application/     сервисы, интерфейсы, DTO, сообщения
  FinTrack.Infrastructure/  EF Core, репозитории, миграции
  FinTrack.Wpf/             окна, ViewModel, стили
tests/
  FinTrack.UnitTests/       тесты Domain, Application, ViewModel
docs/                       ТЗ, документация, план разработки
```

Правило зависимостей: `Wpf → Application → Domain`, `Infrastructure → Application, Domain`.

## Сборка и запуск

Требуется Windows и .NET SDK версии из `global.json`.

```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/FinTrack.Wpf
```

## Тесты

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Работа с репозиторием

- Ветка `main` защищена, изменения только через pull request при зелёном CI.
- Ветки: `feature/...`, `fix/...`, `docs/...`.
- Коммиты: `feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `chore:`.

## Авторы

Артём Бланк, Антон Жовнир — группа 2407г1.