# FinTrack — план разработки

FinTrack строится из пяти проектов и восьми недель-срезов по ТЗ; сначала договорённости, которые нужно принять до первой строки кода.

## Принципы и договорённости

- **Стек (из ТЗ):** C# 14, .NET 10, WPF, EF Core 10 + SQLite, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, LiveCharts2, xUnit.
- **Правило зависимостей:** `Wpf → Application → Domain`; `Infrastructure → Application, Domain`; `Domain` не знает ни о ком. Бизнес-логики во View и ViewModel нет.
- **Деньги:** везде `decimal`. Как хранить в SQLite — решение первой недели (см. раздел «Риски»).
- **Время:** никакого `DateTime.Now` в логике, только `IClock`; иначе тесты на периоды и прогноз ломаются.
- **Ошибки:** нарушение бизнес-правила = `BusinessRuleException` с понятным русским текстом; ViewModel показывает его рядом с полем или в диалоге.
- **Языки:** код, имена классов и сообщения коммитов на английском; всё, что видит пользователь, на русском.
- **Вертикальные срезы:** каждая неделя заканчивается работающей функцией от экрана до БД, а не «готовым слоем».
- **«Готово» =** код + тест на логику + зелёный CI + ревью напарника в pull request.
- **Кто за что (из ТЗ):** Артём — Domain, Application, Infrastructure; Антон — View, стили, тесты; ViewModel и интеграция — вместе.

## Структура решения

Решение — пять проектов, зависимости идут только в одну сторону, слои из ТЗ ложатся на проекты почти один к одному.

| Слой из ТЗ | Проект | Что внутри | Ссылается на |
| --- | --- | --- | --- |
| Model | FinTrack.Domain | сущности, перечисления, чистые расчёты | — |
| Services | FinTrack.Application | сервисы, интерфейсы репозиториев, DTO, сообщения, прогноз | Domain |
| Data | FinTrack.Infrastructure | EF Core, репозитории, миграции, начальные данные | Application, Domain |
| View + ViewModel | FinTrack.Wpf | окна, представления, ViewModel, навигация, стили | Application; Infrastructure — только в точке сборки (App.xaml.cs) |
| Тесты | FinTrack.UnitTests | тесты Domain, Application и ViewModel | Domain, Application, Wpf |

Если понадобится, добавляется шестый проект `FinTrack.IntegrationTests` (SQLite in-memory) — в ТЗ он не требуется.

```text
FinTrack/
├─ FinTrack.sln
├─ README.md, .gitignore, .editorconfig, Directory.Build.props
├─ .github/workflows/ci.yml
├─ docs/                       ТЗ, проектная документация, этот план
├─ src/
│  ├─ FinTrack.Domain/         Entities/ Enums/ Calculations/ Exceptions/
│  ├─ FinTrack.Application/    Abstractions/ Services/ Dto/ Forecasting/ Messages/
│  ├─ FinTrack.Infrastructure/ Persistence/ Repositories/ Seed/ System/
│  └─ FinTrack.Wpf/            Views/ ViewModels/ Controls/ Navigation/ Services/
│                              Converters/ Charts/ Resources/
└─ tests/
   └─ FinTrack.UnitTests/      зеркально папкам src + Fakes/ + TestData/
```

Содержимое каждой папки — в разделах ниже.

## Domain (Model): сущности и расчёты

Domain — это 8 сущностей, 7 перечислений и набор чистых расчётных классов без базы данных и без WPF; именно их будет проще всего покрыть тестами.

### Сущности (`Entities/`)

| Класс | Поля | Связи и правила |
| --- | --- | --- |
| `Account` | Id, Name, Type, InitialBalance, InterestRate?, TermMonths?, IsArchived | владеет операциями и сделками; ставка и срок — только у вклада; баланс не хранится |
| `Category` | Id, Name, Kind, ParentId?, Color, IsArchived | подкатегория — один уровень; вид подкатегории = виду родителя |
| `Transaction` | Id, Date, Amount, Type, AccountId, TargetAccountId?, CategoryId?, Note | перевод: есть TargetAccountId, нет CategoryId; доход и расход: наоборот |
| `Portfolio` | Id, Name, IsArchived | содержит позиции |
| `Position` | Id, PortfolioId, Name, InstrumentType, CurrentPrice, IsArchived | количество и средняя цена считаются из сделок, не хранятся |
| `Deal` | Id, PositionId, AccountId, Date, Type, Quantity, Price | сохраняется атомарно с изменением остатка счёта |
| `Goal` | Id, Name, TargetAmount, Deadline, AccountId?, PortfolioId? | ровно одна привязка: к счёту или к портфелю |
| `Asset` (доп.) | Id, Name, Group, PurchasePrice, ExtraCosts, Status, SalePrice?, SaleDate?, SaleAccountId? | продажа создаёт операцию дохода на счёт SaleAccountId |

Общая база — `Entity` (Id) и интерфейс `IArchivable` (IsArchived) для правила «не удалять, а архивировать».

### Перечисления (`Enums/`)

- `AccountType`: Debit, Savings, Deposit
- `CategoryKind`: Income, Expense
- `TransactionType`: Income, Expense, Transfer
- `InstrumentType`: Stock, Bond, Crypto, PreciousMetal
- `DealType`: Buy, Sell
- `AssetGroup`: Electronics, Car, RealEstate, Other
- `AssetStatus`: InStock, ForSale, Sold

### Расчёты (`Calculations/`)

Статические классы без состояния, принимают данные, возвращают числа:

- `BalanceCalculator` — остаток счёта (правило 5 из ТЗ).
- `PositionCalculator` — количество, средняя цена покупки, вложенные средства, стоимость, прибыль и прибыль в процентах.
- `GoalCalculator` — прогресс в процентах, остаток суммы, ежемесячный взнос до срока.
- `AssetCalculator` (доп.) — полная себестоимость и прибыль от продажи.
- `TrendCalculator` — линия тренда (линейная регрессия по точкам) и процент изменения к прошлому периоду.
- `Period` (record) — From, To; фабрики `Month`, `Quarter`, `Year`, метод `Previous()`.

### Исключения (`Exceptions/`)

`BusinessRuleException` (текст для пользователя) и `NotFoundException`. Правила одной сущности (сумма > 0, соответствие вида категории) проверяются методом `EnsureValid()` самой сущности; правила на стыке нескольких сущностей — в сервисах Application.

## Application (Services): сервисы и интерфейсы

Application — это десять сервисов, интерфейсы репозиториев и небольшой набор DTO; ViewModel говорят только с этим слоем, никогда напрямую с базой.

### Интерфейсы (`Abstractions/`)

- `IAccountRepository`, `ICategoryRepository`, `ITransactionRepository` (с методом выборки по `TransactionFilter`), `IPortfolioRepository`, `IPositionRepository`, `IDealRepository`, `IGoalRepository`, `IAssetRepository` (доп.)
- `IUnitOfWork` и `IUnitOfWorkFactory`: фабрика создаёт один UnitOfWork на одну операцию, он отдаёт репозитории свойствами и делает `SaveChangesAsync` в одной транзакции (сделки, продажа имущества)
- `IClock` — `Today`, `Now`

### Сервисы (`Services/`)

| Сервис | Что делает | Правила из ТЗ |
| --- | --- | --- |
| `IAccountService` | CRUD, архивация, остатки счетов (`GetBalancesAsync`) | 3, 5, 8 |
| `ICategoryService` | CRUD, подкатегории, архивация | 2, 8 |
| `ITransactionService` | добавить, изменить, удалить доход, расход, перевод; список по фильтру | 1–4 |
| `IPortfolioService` | портфели, позиции, ввод текущей цены, сводка портфеля | 8, 9 |
| `IDealService` | покупка и продажа позиции | 1, 3, 6, 7 |
| `IGoalService` | CRUD, прогресс и ежемесячный взнос | 10 |
| `IAssetService` (доп.) | CRUD, продажа → операция дохода | 7 |
| `IAnalyticsService` | расходы по категориям, доходы и расходы по месяцам, динамика, тренды | 11 (часть) |
| `IForecastService` | прогноз расходов на следующий месяц по категориям | 11 |
| `INetWorthService` | чистый капитал сейчас и по месяцам | 12 |

Реализации — в той же папке (`AccountService` и т. д.), все принимают зависимости через конструктор.

### Прогноз и фабрика

- `Forecasting/IForecastStrategy` — «история расходов → прогноз». Реализации: `AverageForecastStrategy` (среднее за 3 последних завершённых месяца, правило 11) и необязательно `TrendForecastStrategy` — тогда Strategy видно на защите на двух реализациях, а не на одной.
- `Factories/PositionFactory` — создаёт `Position` по типу инструмента. Чтобы паттерн был не для галочки, предлагаю правило по типу: у акций и облигаций количество целое, у крипты и металлов — дробное (в ТЗ этого правила нет, его нужно согласовать).

### DTO (`Dto/`)

Неизменяемые `record`-ы для вывода: `AccountBalanceDto`, `TransactionDto`, `TransactionFilter`, `PortfolioSummary`, `PositionSummary`, `GoalProgressDto`, `CategoryExpenseDto`, `MonthlyTotalsDto`, `TrendSeriesDto`, `ForecastItemDto`, `NetWorthDto`. Для ввода — `Create...`/`Update...` records (например, `CreateTransactionRequest`), чтобы ViewModel не работали с сущностями напрямую.

### Сообщения (`Messages/`) — Observer

`AccountsChanged`, `CategoriesChanged`, `TransactionsChanged`, `InvestmentsChanged`, `GoalsChanged`, `AssetsChanged`. Сервисы отправляют их через `IMessenger` после успешного сохранения, а виджеты и списки подписаны и обновляются сами. Решение для недели 1: для этого Application получает пакет CommunityToolkit.Mvvm.Messaging — это допустимо, иначе пишем свой `IEventBus`.

## Infrastructure (Data): база данных

Infrastructure реализует интерфейсы из Application на EF Core и SQLite и больше ничего наружу не показывает.

### Папки и классы

- `Persistence/FinTrackDbContext` — 8 `DbSet`, `ApplyConfigurationsFromAssembly`.
- `Persistence/Configurations/` — по классу на сущность (`AccountConfiguration`, `CategoryConfiguration`, `TransactionConfiguration`, `PortfolioConfiguration`, `PositionConfiguration`, `DealConfiguration`, `GoalConfiguration`, `AssetConfiguration`): ключи, длины строк, перечисления как строки, связи с `DeleteBehavior.Restrict` (правило 8 из ТЗ), индексы по `Transaction.Date`, `AccountId`, `CategoryId`.
- `Persistence/Migrations/` — `InitialCreate` в неделе 2, дальше по миграции на каждое изменение модели.
- `Repositories/` — `EfRepository<T>` (базовые операции) и по одному классу на интерфейс; самый сложный — `TransactionRepository.QueryAsync(filter)` (период, счёт, категория, тип, текст).
- `Persistence/EfUnitOfWork` и `EfUnitOfWorkFactory` — на основе `IDbContextFactory<FinTrackDbContext>`: контекст живёт ровно одну операцию. Долгоживущий контекст в WPF даёт устаревшие данные на экранах.
- `Seed/DbInitializer` + `DefaultCategories` — применить миграции и создать категории по умолчанию при первом запуске; плюс необязательный `DemoDataSeeder` для демонстрации графиков на защите.
- `System/` — `SystemClock : IClock`, `DatabasePathProvider` (`%LocalAppData%\FinTrack\fintrack.db`), `FileLogger` (журнал ошибок в той же папке).
- `DependencyInjection.AddInfrastructure(...)` — единственная точка, где регистрируются их реализации.

### Решение недели 1: деньги в SQLite

EF Core с SQLite хранит `decimal` как текст и не может перевести `SUM` и сортировку по этому полю в SQL. Два варианта:

1. Хранить суммы в копейках (`long`) через `ValueConverter`.
2. Оставить `decimal` и суммировать в памяти после выборки.

Рекомендация — вариант 2: объёмы маленькие (требование ТЗ — 10 000 операций), а логика остаётся простой. Если упрёмся в скорость — переходим на 1. Наружу из Infrastructure в любом случае выходит только `decimal`.

## WPF: окна, экраны и ViewModel

WPF-проект — одно главное окно с боковой навигацией, девять экранов-страниц, одно универсальное окно для диалогов и по одной ViewModel на каждый экран, виджет и диалог.

### Точка входа

- `App.xaml.cs` — собирает `ServiceCollection` (`AddInfrastructure`, `AddApplication`, `AddViewModels`), запускает `DbInitializer`, показывает `MainWindow`.
- `GlobalExceptionHandler` — ловит необработанные исключения, пишет в журнал, показывает понятное сообщение (требование «не падать» из ТЗ).

### Окна

- `MainWindow` — оболочка: слева меню разделов, справа `ContentControl`, который показывает текущую ViewModel через `DataTemplate` (подход ViewModel-first). Выставляет минимум 1280×720.
- `DialogWindow` — один класс для всех диалогов: заголовок, содержимое по `DataTemplate`, кнопки «Сохранить» и «Отмена». Десять отдельных окон не нужно.

### Экраны (`Views/` и `ViewModels/`)

| Экран | View | ViewModel | Содержимое | Сервисы |
| --- | --- | --- | --- | --- |
| Главная | `DashboardView` | `DashboardViewModel` + 9 виджетов | сетка виджетов, период, быстрое добавление операции | Analytics, Forecast, NetWorth, Account, Goal, Portfolio, Transaction |
| Счета | `AccountsView` | `AccountsViewModel`, `AccountItemViewModel` | карточки счетов, группировка по типам, итоги | Account |
| Операции | `TransactionsView` | `TransactionsViewModel`, `TransactionFilterViewModel`, `TransactionItemViewModel` | журнал, фильтры, поиск, добавление | Transaction, Account, Category |
| Категории | `CategoriesView` | `CategoriesViewModel`, `CategoryNodeViewModel` | дерево категорий с цветами | Category |
| Инвестиции | `InvestmentsView` | `InvestmentsViewModel`, `PortfolioViewModel`, `PositionItemViewModel`, `DealItemViewModel` | портфели → позиции → сделки, цены, прибыль | Portfolio, Deal |
| Цели | `GoalsView` | `GoalsViewModel`, `GoalItemViewModel` | карточки с прогрессом и взносом | Goal |
| Аналитика | `AnalyticsView` | `AnalyticsViewModel` | подробные графики, тренды, прогноз по категориям | Analytics, Forecast |
| Имущество (доп.) | `AssetsView` | `AssetsViewModel`, `AssetItemViewModel` | список, статусы, продажа | Asset |
| Настройки | `SettingsView` | `SettingsViewModel` | период по умолчанию, тема | `ISettingsService` |

### Диалоги (формы редактирования)

`AccountEditViewModel`, `CategoryEditViewModel`, `TransactionEditViewModel`, `PortfolioEditViewModel`, `PositionEditViewModel` (включая ввод текущей цены), `DealEditViewModel`, `GoalEditViewModel`, `AssetEditViewModel` и `AssetSaleViewModel` (доп.). У каждой — своё `UserControl`-представление в `Views/Dialogs/`, открывается внутри `DialogWindow`.

### Базовые классы (`ViewModels/Base/`)

- `ViewModelBase : ObservableObject` — `IsBusy`, `ErrorMessage`, единый способ показать `BusinessRuleException` пользователю.
- `PageViewModelBase : ObservableRecipient` — `Title`, `LoadAsync()`, подписка на сообщения `...Changed`.
- `EditViewModelBase : ObservableValidator` — валидация полей, `SaveCommand`, `CancelCommand`, событие `CloseRequested`.
- `PeriodSelectorViewModel` — месяц/квартал/год + «назад/вперёд»; один на Главную и Аналитику.

### Виджеты главного экрана (`ViewModels/Dashboard/`, `Views/Widgets/`)

Ровно 9 виджетов из таблицы 1 ТЗ, по одной ViewModel и одному `UserControl` на каждый: `NetWorthWidget`, `NetWorthChartWidget`, `ExpensesByCategoryWidget`, `IncomeExpenseWidget`, `AccountsWidget`, `InvestmentsWidget`, `GoalsWidget`, `ForecastWidget`, `RecentTransactionsWidget`. Каждый слушает сообщения и период и сам перезагружается.

### Навигация и службы UI (`Navigation/`, `Services/`)

- `INavigationService` / `NavigationService` — `NavigateTo<TViewModel>()`, событие смены текущей страницы; ViewModel создаёт DI-контейнер.
- `NavigationItem` (название, значок, тип ViewModel) и `MainViewModel` — список пунктов меню и текущая страница.
- `IDialogService` / `DialogService` — `ShowDialogAsync<TViewModel>()`, `ConfirmAsync(...)`, `ShowError(...)`.
- `ISettingsService` / `JsonSettingsService` — настройки в JSON-файле рядом с базой.

### Повторно используемые части

- `Controls/`: `WidgetCard` (рамка виджета с заголовком), `KpiTile` (значение + тренд), `TrendIndicator` (↑↓ и цвет), `CategoryBadge` (цвет + название), `PeriodSelector`, `EmptyState`.
- `Converters/`: деньги в рублях, проценты, тренд → цвет, перечисления → русские названия («Дебетовый»), цвет категории → кисть, булево → видимость.
- `Charts/`: `ChartFactory` — чистые методы, которые строят серии LiveCharts2 (линия капитала с трендом, кольцевая диаграмма расходов, столбцы доходов и расходов) и `ChartTheme` (цвета). ViewModel только вызывают фабрику, а не собирают графики сами.
- `Resources/`: `Colors.xaml`, `Typography.xaml`, `Controls.xaml` (кнопки, поля, таблицы), `Cards.xaml`, `DataTemplates.xaml` (ViewModel → View), по возможности `Themes/Light.xaml` и `Dark.xaml`. Значки — символы шрифта Segoe Fluent Icons, без картинок.

## Тесты

Проект `FinTrack.UnitTests` зеркалит папки `src`, в нём около 25 тестовых классов, и каждое из 12 бизнес-правил ТЗ имеет как минимум один тест; пишет их Антон.

### Структура

- `Fakes/` — имитации, которые мы пишем сами (без библиотек «моков»): `InMemoryRepository<T>` и по наследнику на каждый интерфейс репозитория, `FakeUnitOfWork` и `FakeUnitOfWorkFactory` (умеет «уронить» при сохранении для теста атомарности), `FakeClock`, `FakeDialogService`, `FakeNavigationService`.
- `TestData/` — строители данных (`AccountBuilder`, `TransactionBuilder`, `PositionBuilder` и т. д.), чтобы тест задавал только то, что важно для него.
- `Domain/` — `BalanceCalculatorTests`, `PositionCalculatorTests`, `GoalCalculatorTests`, `AssetCalculatorTests`, `TrendCalculatorTests`, `PeriodTests`, `TransactionTests`, `CategoryTests`.
- `Application/` — по классу на сервис (`AccountServiceTests`, `CategoryServiceTests`, `TransactionServiceTests`, `PortfolioServiceTests`, `DealServiceTests`, `GoalServiceTests`, `AssetServiceTests`, `AnalyticsServiceTests`, `ForecastServiceTests`, `NetWorthServiceTests`) и `AverageForecastStrategyTests`, `PositionFactoryTests`.
- `ViewModels/` — `MainViewModelTests` (навигация), `AccountsViewModelTests`, `TransactionsViewModelTests`, `TransactionEditViewModelTests` (валидация, команды), `DashboardViewModelTests` (перезагрузка виджетов по сообщению).

### Какое правило какими тестами покрыто

| № правила из ТЗ | Правило | Тестовые классы |
| --- | --- | --- |
| 1 | сумма, количество, цена > 0 | `TransactionTests`, `DealServiceTests` |
| 2 | вид категории = тип операции | `TransactionServiceTests`, `CategoryServiceTests` |
| 3 | доходы и расходы только по дебетовым счетам | `TransactionServiceTests` |
| 4 | перевод: счета разные, категории нет | `TransactionServiceTests` |
| 5 | остаток счёта | `BalanceCalculatorTests`, `AccountServiceTests` |
| 6 | продажа не больше имеющегося количества | `DealServiceTests` |
| 7 | сделка атомарна | `DealServiceTests` (через `FakeUnitOfWork`, который уронит при сохранении) |
| 8 | архив вместо удаления | `AccountServiceTests`, `CategoryServiceTests`, `PortfolioServiceTests` |
| 9 | стоимость и прибыль позиции | `PositionCalculatorTests` |
| 10 | прогресс цели | `GoalCalculatorTests`, `GoalServiceTests` |
| 11 | прогноз расходов | `AverageForecastStrategyTests`, `ForecastServiceTests` |
| 12 | чистый капитал | `NetWorthServiceTests` |

### Правила написания

- Имя теста: `Метод_сценарий_ожидание`; тело по схеме Arrange-Act-Assert; граничные значения (0, отрицательные, ровно лимит) — через `[Theory]`.
- Тесты независимы и не обращаются к реальной базе, часу и файловой системе.
- Покрытие — командой `dotnet test --collect:"XPlat Code Coverage"` (пакет `coverlet.collector`); цель из ТЗ — 70 % строк в Domain и Application.
- Порядок на каждую функцию: Артём выкладывает интерфейс и DTO → Антон пишет тесты по правилам ТЗ → Артём реализует сервис, пока тесты не станут зелёными. Тогда тесты не копятся на последнюю неделю.

## План по неделям

Каждая неделя даёт работающую функцию от экрана до базы; недели совпадают с таблицей 5 из ТЗ, распределение — как в ТЗ (Артём: Domain, Application, Infrastructure; Антон: View, стили, тесты; ViewModel — вместе).

### Неделя 1 — основа проекта

Результат: репозиторий настроен, пустое решение собирается, тесты запускаются в CI.

**Артём**

- [x] Создать решение и 5 проектов, выставить ссылки по правилу зависимостей
- [x] Подключить пакеты: EF Core SQLite, CommunityToolkit.Mvvm, DI, LiveCharts2, xUnit, coverlet
- [x] Завести в Domain 8 сущностей и 7 перечислений (без логики)
- [ ] Зафиксировать три решения: деньги в SQLite, IMessenger в Application, IUnitOfWorkFactory
- [ ] Черновики диаграммы классов и ER в Mermaid в `docs/`

**Антон**

- [x] `.gitignore`, `.editorconfig`, `Directory.Build.props` (Nullable, ImplicitUsings, предупреждения как ошибки)
- [x] CI в GitHub Actions: `dotnet build` и `dotnet test`
- [ ] Проект тестов с первым тестом и папками `Fakes/`, `TestData/`
- [ ] Дизайн: палитра, шрифты, макеты «Главная», «Операции», «Счета»

**Вместе**

- [x] Утвердить ТЗ, договориться о ветках и pull request, завести доску задач из этого плана

Готово, когда: в CI на `main` зёлёные `build` и `test`, в Domain все сущности.

### Неделя 2 — каркас MVVM и база

Результат: приложение запускается, создаёт базу при первом запуске, меню переключает пустые страницы.

**Артём**

- [ ] Infrastructure: `FinTrackDbContext`, 8 конфигураций, миграция `InitialCreate`
- [ ] `EfRepository<T>`, `EfUnitOfWork`, `EfUnitOfWorkFactory`, `DatabasePathProvider`, `SystemClock`
- [ ] `DbInitializer` и `DefaultCategories`
- [ ] Application: `Abstractions/` (репозитории, `IClock`, UnitOfWork), `AddApplication`, `AddInfrastructure`

**Антон**

- [ ] Стили: `Colors`, `Typography`, `Controls`, `Cards`
- [ ] `MainWindow` с боковой панелью и страницами-заглушками
- [ ] Контролы `WidgetCard`, `KpiTile`, `TrendIndicator`; базовые конвертеры
- [ ] В тестах: `InMemoryRepository<T>`, `FakeClock`, `FakeUnitOfWork`

**Вместе**

- [ ] `ViewModelBase`, `PageViewModelBase`, `MainViewModel`, `INavigationService` + `DataTemplates.xaml`
- [ ] `App.xaml.cs` с DI, `GlobalExceptionHandler`

Готово, когда: база создаётся сама, меню работает, `MainViewModelTests` зёлёный.

### Неделя 3 — счета, категории, операции

Результат: можно завести счета и категории, добавить доход, расход, перевод и увидеть остаток.

**Артём**

- [ ] `AccountService` (CRUD, архив, правила 3 и 8), `BalanceCalculator` (правило 5)
- [ ] `CategoryService` (подкатегории, правила 2 и 8)
- [ ] `TransactionService` (правила 1–4), `TransactionRepository.QueryAsync(filter)`
- [ ] DTO и сообщения `AccountsChanged`, `CategoriesChanged`, `TransactionsChanged`

**Антон**

- [ ] Тесты правил 1–5 и 8 — до реализации сервисов
- [ ] `AccountsView`, `CategoriesView`, `TransactionsView`, `DialogWindow` и формы редактирования
- [ ] `CategoryBadge`, конвертеры денег, дат и перечислений

**Вместе**

- [ ] ViewModel: `AccountsViewModel`, `CategoriesViewModel`, `TransactionsViewModel` (+ фильтр и элемент), все `...EditViewModel` этой недели
- [ ] `IDialogService` и `DialogService`

Готово, когда: операции добавляются и фильтруются, остатки счетов верны, правила 1–5 и 8 покрыты тестами.

### Неделя 4 — статистика и первая главная

Результат: на главном экране пять виджетов по реальным данным, период переключается, на графиках есть линия тренда.

**Артём**

- [ ] `Period`, `TrendCalculator`
- [ ] `AnalyticsService`: расходы по категориям, доходы и расходы по месяцам, процент изменения к прошлому периоду
- [ ] Первая версия `NetWorthService` (остатки счетов)

**Антон**

- [ ] `ChartFactory` и `ChartTheme` (LiveCharts2): столбцы с трендом, кольцевая диаграмма
- [ ] Виджеты: чистый капитал, расходы по категориям, доходы и расходы, счета, последние операции; контрол `PeriodSelector`
- [ ] Тесты `TrendCalculatorTests`, `PeriodTests`, `AnalyticsServiceTests`

**Вместе**

- [ ] `DashboardViewModel`, `PeriodSelectorViewModel`, 5 первых виджет-ViewModel
- [ ] Кнопка «+ Операция» с главного экрана

Готово, когда: добавил операцию — виджеты обновились сами.

### Неделя 5 — инвестиции

Результат: покупка и продажа позиции меняют остаток счёта и прибыль портфеля.

**Артём**

- [ ] `PositionCalculator` (правило 9), `PositionFactory`
- [ ] `PortfolioService`, `DealService` (правила 6 и 7 через UnitOfWork), правка `BalanceCalculator` для сделок
- [ ] DTO `PortfolioSummary`, `PositionSummary`, сообщение `InvestmentsChanged`

**Антон**

- [ ] Тесты правил 6, 7, 9 и `PositionFactoryTests` — до реализации
- [ ] `InvestmentsView`, формы портфеля, позиции, сделки, виджет «Инвестиции»

**Вместе**

- [ ] `InvestmentsViewModel` (+ портфель, позиция, сделка), `PortfolioEditViewModel`, `PositionEditViewModel`, `DealEditViewModel`

Готово, когда: купил позицию — остаток счёта уменьшился; продать больше, чем есть, нельзя.

### Неделя 6 — цели, прогноз, чистый капитал

Результат: все девять виджетов главного экрана работают, есть экраны «Цели» и «Аналитика».

**Артём**

- [ ] `GoalCalculator`, `GoalService` (правило 10)
- [ ] `IForecastStrategy`, `AverageForecastStrategy` (и по желанию `TrendForecastStrategy`), `ForecastService` (правило 11)
- [ ] `NetWorthService` полностью: счета + портфели, история по месяцам (правило 12)

**Антон**

- [ ] Тесты правил 10–12 — до реализации
- [ ] `GoalsView`, `AnalyticsView`, виджеты «Цели», «Прогноз», «Динамика капитала»

**Вместе**

- [ ] `GoalsViewModel`, `GoalEditViewModel`, `AnalyticsViewModel`, 4 оставшихся виджет-ViewModel

Готово, когда: прогноз и чистый капитал совпадают с ручным расчётом на тестовых данных.

### Неделя 7 — имущество, надёжность, стиль

Результат: приложение не падает на плохом вводе, выглядит цельно, покрытие тестами — не менее 70 %.

**Артём**

- [ ] Доп. модуль: `AssetService`, `AssetCalculator`, продажа → операция дохода
- [ ] `ISettingsService`, журнал `FileLogger`, понятные сообщения при ошибках БД
- [ ] `DemoDataSeeder` на 10 000 операций и замер скорости экранов (требование «не более 1 с»)

**Антон**

- [ ] `AssetsView`, `SettingsView`, формы имущества и продажи
- [ ] `EmptyState`, светлая и тёмная темы (по возможности), проверка 1280×720
- [ ] Довести покрытие Domain и Application до 70 %

**Вместе**

- [ ] Валидация всех форм, подтверждения удаления, проход по требованиям 4.1.9 и 4.2 ТЗ
- [ ] Решение о модуле «Имущество»: если отстаём, вырезаем (см. «Риски»)

Готово, когда: все пункты 4.2 ТЗ выполнены, покрытие ≥ 70 %.

### Неделя 8 — финал

Результат: опубликованный релиз, документация, готовность к защите.

**Вместе**

- [ ] Ручное тестирование по списку сценариев (15–20 штук, `docs/test-scenarios.md`), исправление ошибок
- [ ] `dotnet publish` (win-x64, self-contained), запуск на «чистом» Windows (Windows Sandbox или другой ПК)
- [ ] Проектная документация по ГОСТ 19.404-79: диаграмма классов, ER, навигация, структура проекта, результаты проверки, источники
- [ ] `README.md`: описание, скриншоты, сборка, запуск, тесты
- [ ] Релиз `v1.0` на GitHub (архив с приложением)
- [ ] Репетиция защиты: каждый объясняет свои слои и паттерны (таблица 4 ТЗ)

Готово, когда: выполнены критерии приёмки из раздела 8 ТЗ.

## Работа в GitHub

### Ветки и pull request

- `main` защищена: прямых коммитов нет, слияние только через pull request при зелёном CI
- Ветки короткие, по одной задаче: `feature/accounts-service`, `feature/dashboard-widgets`, `fix/balance-rounding`, `docs/readme`
- Каждый pull request смотрит второй участник: Артём проверяет код Антона, Антон — код Артёма; одного одобрения достаточно
- Слияние через Squash, чтобы история `main` оставалась читаемой
- Pull request небольшой: одна функция или один экран, не больше нескольких сотен строк

### Коммиты

- Формат: `тип: что сделано`, где тип — `feat`, `fix`, `test`, `refactor`, `docs`, `chore`; пример: `feat: добавить AccountService`
- Тест и код, который его проходит, можно коммитить отдельно: так в истории видно «тест сначала»

### Задачи

- Каждый пункт чеклиста из плана по неделям превращается в Issue с меткой недели (`week-3`) и исполнителя
- Доска GitHub Projects с колонками «Бэклог», «В работе», «На ревью», «Готово»
- В описании pull request ссылка на задачу: `Closes #12`
- Раз в неделю короткая встреча: что закрыто, что переносится, не пора ли резать объём

### CI (GitHub Actions)

- Один файл `.github/workflows/ci.yml`, запуск на каждый push и pull request
- Шаги: `actions/checkout`, `actions/setup-dotnet` (версия из `global.json`), `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test --no-build -c Release --collect:"XPlat Code Coverage"`
- Запускать на `windows-latest`: WPF на Linux не соберётся
- Отчёт о покрытии сохранять как артефакт сборки; порог 70 % проверять вручную в конце недели 7

### Файлы репозитория

- `README.md` — описание, скриншоты, как собрать и запустить, как гонять тесты
- `.gitignore` для Visual Studio и .NET (`bin/`, `obj/`, `.vs/`, `*.db`)
- `.editorconfig` — отступы, `var`, порядок `using`, чтобы форматирование не спорило в pull request
- `global.json` фиксирует версию SDK, `Directory.Build.props` — общие настройки проектов
- `docs/` — ТЗ, диаграммы, сценарии ручных тестов, проектная документация
- `LICENSE` — по желанию

### Определение «готово» для задачи

- Код собирается без предупреждений, CI зелёный
- Для логики есть тесты, для экрана — пройден сценарий вручную
- Pull request просмотрен вторым участником
- Нет закомментированного кода и `TODO` без номера задачи

## Риски и что вырезать при отставании

### Технические риски

- **Деньги в SQLite.** `decimal` хранится как текст, суммы и сортировка по нему в запросах не переводятся. Решение из раздела про данные: агрегировать в памяти или хранить копейки. Проверить на неделе 2, до написания сервисов, иначе придётся переделывать.
- **Совместимость LiveCharts2 с .NET 10.** Библиотека может отставать от свежих версий. Проверить на неделе 1 пустым графиком; запасной вариант — .NET 8 (в примере преподавателя он и стоит) или OxyPlot.
- **Диспетчер UI в тестах.** ViewModel, который трогает `Application.Current` или `Dispatcher`, в xUnit падает. Правило: ViewModel не знает про WPF-типы, всё UI-зависимое идёт через интерфейсы (`IDialogService`, `INavigationService`).
- **Долгоживущий `DbContext`.** Один контекст на всё приложение копит устаревшие данные. Правило: контекст создаёт фабрика на каждую операцию.
- **Атомарность сделок.** Покупка списывает деньги и меняет позицию, и всё должно произойти вместе или не произойти вовсе. Только через `IUnitOfWork`, с тестом на откат при ошибке.
- **Медленные экраны.** 10 000 операций и несколько графиков на главной. Считать на сервисах, а не в ViewModel; замерить на неделе 7 при `DemoDataSeeder`.

### Организационные риски

- **Раздувание объёма.** Идей много (фьючерсы, регулярные платежи, экспорт уже вырезаны). Новая идея идёт в Issue с меткой `later` и не берётся, пока не закрыт обязательный объём.
- **Расхождение интерфейсов.** Артём и Антон работают параллельно. Интерфейсы сервисов и DTO согласуются в начале недели и меняются только через обсуждение в Issue.
- **Мёртвая неделя 8.** Документация и защита не терпят спешки. Проектную документацию начинать писать с недели 6, диаграммы брать из `docs/`.
- **Один человек — единственный носитель знаний.** Каждый должен уметь объяснить чужой слой хотя бы в общих чертах (вопросы защиты).

### Что вырезать, если отстаём

Порядок: от самого безболезненного к самому больному.

1. Светлая и тёмная темы, переключатель в настройках
2. Модуль «Имущество» целиком (`Asset`, `AssetService`, `AssetsView`, 3 формы): он дополнительный и стоит последним
3. Вторая стратегия прогноза (`TrendForecastStrategy`), остаётся средняя
4. Виджеты главной: оставить 5–6 основных, остальные превратить в раздел «Аналитика»
5. История чистого капитала по месяцам: оставить только текущее значение
6. Подкатегории: плоский список категорий

Не вырезать никогда: счета, категории, операции, портфели со сделками, цели, статистика с графиками и трендом, тесты логики. Это ядро ТЗ и критерии преподавателя.
