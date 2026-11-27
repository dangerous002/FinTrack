using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;

namespace FinTrack.UnitTests.TestData;

public class AccountBuilder
{
    private string _name = "Тестовый счёт";
    private AccountType _type = AccountType.Debit;
    private decimal _initialBalance;
    private bool _isArchived;

    public AccountBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AccountBuilder OfType(AccountType type)
    {
        _type = type;
        return this;
    }

    public AccountBuilder WithInitialBalance(decimal balance)
    {
        _initialBalance = balance;
        return this;
    }

    public AccountBuilder Archived()
    {
        _isArchived = true;
        return this;
    }

    public Account Build() => new()
    {
        Name = _name,
        Type = _type,
        InitialBalance = _initialBalance,
        IsArchived = _isArchived
    };
}