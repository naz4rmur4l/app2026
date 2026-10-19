using Core.Dto;

namespace Core.Domain;

public sealed class Loan
{
    public string Id { get; }
    public string CopyId { get; }
    public string ReaderId { get; }
    public DateOnly IssuedOn { get; }
    public DateOnly? ReturnedOn { get; private set; }

    public bool IsClosed => ReturnedOn is not null;

    private Loan(string id, string copyId, string readerId, DateOnly issuedOn)
    {
        Id = id;
        CopyId = copyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
    }

    // Відкрити видачу: спочатку перевіряємо все і створюємо об'єкт,
    // і лише потім змінюємо стан примірника.
    public static Loan Open(string id, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);

        Loan loan = Create(id, copy.Id, readerId, issuedOn);
        copy.Issue(); // кидає InvalidOperationException, якщо примірник уже виданий
        return loan;
    }

    // Закрити видачу: усі перевірки йдуть до зміни стану.
    public void Close(BookCopy copy, DateOnly returnedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);

        if (copy.Id != CopyId)
            throw new ArgumentException(
                $"Видача {Id} стосується примірника {CopyId}, а не {copy.Id}", nameof(copy));
        if (IsClosed)
            throw new InvalidOperationException(
                $"Видача {Id} вже закрита {ReturnedOn:yyyy-MM-dd}, повторно закрити не можна");
        EnsureNotBeforeIssue(returnedOn);

        copy.Return();            // може кинути виняток, але ще до зміни ReturnedOn
        ReturnedOn = returnedOn;
    }

    public LoanDto ToDto() => new(Id, CopyId, ReaderId, IssuedOn, ReturnedOn);

    public static Loan FromDto(LoanDto dto)
    {
        Loan loan = Create(dto.Id, dto.CopyId, dto.ReaderId, dto.IssuedOn);
        if (dto.ReturnedOn is DateOnly returnedOn)
        {
            loan.EnsureNotBeforeIssue(returnedOn);
            loan.ReturnedOn = returnedOn;
        }
        return loan;
    }

    private static Loan Create(string id, string copyId, string readerId, DateOnly issuedOn)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор видачі обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(copyId))
            throw new ArgumentException("Ідентифікатор примірника обов'язковий", nameof(copyId));
        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));

        return new Loan(id.Trim(), copyId.Trim(), readerId.Trim(), issuedOn);
    }

    private void EnsureNotBeforeIssue(DateOnly returnedOn)
    {
        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn,
                $"Дата повернення {returnedOn:yyyy-MM-dd} раніша за дату видачі {IssuedOn:yyyy-MM-dd}");
    }

    public override string ToString() =>
        $"{Id}: примірник {CopyId}, читач {ReaderId}, видано {IssuedOn:yyyy-MM-dd}, " +
        (ReturnedOn is DateOnly r ? $"повернуто {r:yyyy-MM-dd}" : "не повернуто");
}
