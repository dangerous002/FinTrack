# Модель предметной области

Диаграммы описывают сущности слоя `FinTrack.Domain` (8 классов, 7 перечислений). Баланс счёта и количество по позиции в сущностях не хранятся: они вычисляются из операций и сделок.

## Диаграмма классов

```mermaid
classDiagram
    class Entity { +int Id }
    class IArchivable { <<interface>> +bool IsArchived }

    class Account { +string Name; +AccountType Type; +decimal InitialBalance; +decimal? InterestRate; +int? TermMonths }
    class Category { +string Name; +CategoryKind Kind; +int? ParentId; +string Color }
    class Transaction { +DateOnly Date; +decimal Amount; +TransactionType Type; +string? Note }
    class Portfolio { +string Name }
    class Position { +string Name; +InstrumentType InstrumentType; +decimal CurrentPrice }
    class Deal { +DateOnly Date; +DealType Type; +decimal Quantity; +decimal Price }
    class Goal { +string Name; +decimal TargetAmount; +DateOnly Deadline }
    class Asset { +string Name; +AssetGroup Group; +decimal PurchasePrice; +decimal ExtraCosts; +AssetStatus Status; +decimal? SalePrice; +DateOnly? SaleDate }

    Entity <|-- Account
    Entity <|-- Category
    Entity <|-- Transaction
    Entity <|-- Portfolio
    Entity <|-- Position
    Entity <|-- Deal
    Entity <|-- Goal
    Entity <|-- Asset
    IArchivable <|.. Account
    IArchivable <|.. Category
    IArchivable <|.. Portfolio
    IArchivable <|.. Position

    Account "1" --> "*" Transaction
    Category "1" --> "*" Transaction
    Category "1" --> "*" Category : подкатегории
    Portfolio "1" --> "*" Position
    Position "1" --> "*" Deal
    Account "1" --> "*" Deal
    Account "0..1" --> "*" Goal
    Portfolio "0..1" --> "*" Goal
    Account "0..1" --> "*" Asset : счёт продажи
```

## ER-диаграмма

```mermaid
erDiagram
    ACCOUNT ||--o{ TRANSACTION : "AccountId"
    ACCOUNT |o--o{ TRANSACTION : "TargetAccountId (перевод)"
    CATEGORY |o--o{ TRANSACTION : "CategoryId"
    CATEGORY |o--o{ CATEGORY : "ParentId"
    PORTFOLIO ||--o{ POSITION : "PortfolioId"
    POSITION ||--o{ DEAL : "PositionId"
    ACCOUNT ||--o{ DEAL : "AccountId"
    ACCOUNT |o--o{ GOAL : "AccountId"
    PORTFOLIO |o--o{ GOAL : "PortfolioId"
    ACCOUNT |o--o{ ASSET : "SaleAccountId"
```

## Перечисления

| Перечисление | Значения |
|---|---|
| `AccountType` | Debit, Savings, Deposit |
| `CategoryKind` | Income, Expense |
| `TransactionType` | Income, Expense, Transfer |
| `InstrumentType` | Stock, Bond, Crypto, PreciousMetal |
| `DealType` | Buy, Sell |
| `AssetGroup` | Electronics, Car, RealEstate, Other |
| `AssetStatus` | InStock, ForSale, Sold |
