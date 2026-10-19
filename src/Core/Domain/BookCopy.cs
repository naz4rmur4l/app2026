using Core.Dto;

namespace Core.Domain;

public sealed class BookCopy
{
    public string Id { get; }
    public string Isbn { get; }
    public bool IsIssued { get; private set; }

    private BookCopy(string id, string isbn)
    {
        Id = id;
        Isbn = isbn;
    }

    // Єдиний спосіб створити примірник: усі перевірки тут.
    public static BookCopy Create(string id, string isbn)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор примірника обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не може бути порожнім", nameof(isbn));

        return new BookCopy(id.Trim(), isbn.Trim());
    }

    public void Issue()
    {
        if (IsIssued)
            throw new InvalidOperationException(
                $"Примірник {Id} вже виданий, повторна видача неможлива");

        IsIssued = true;
    }

    public void Return()
    {
        if (!IsIssued)
            throw new InvalidOperationException(
                $"Примірник {Id} не був виданий, повертати нічого");

        IsIssued = false;
    }

    // Мапінг у формат DTO і назад: знадобиться сховищу.
    public BookCopyDto ToDto() => new(Id, Isbn, IsIssued);

    public static BookCopy FromDto(BookCopyDto dto)
    {
        BookCopy copy = Create(dto.Id, dto.Isbn); // ті самі перевірки, що й при створенні
        if (dto.IsIssued)
            copy.Issue();
        return copy;
    }

    public override string ToString() =>
        $"{Id} [ISBN {Isbn}] — {(IsIssued ? "видано" : "у фонді")}";
}
