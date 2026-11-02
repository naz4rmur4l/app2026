using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class LendingService(ILibraryStore store)
{
    // Строк видачі в днях: від нього рахується DueDate у події.
    public const int LoanDays = 14;

    private readonly ILibraryStore _store = store ?? throw new ArgumentNullException(nameof(store));

    // Подія змін. Підписатися (+=) і відписатися (-=) може будь-хто, викликати лише цей клас.
    public event EventHandler<LibraryChangedEventArgs>? Changed;

    // Єдине місце, де піднімається подія. ?. — підписників може не бути.
    private void OnChanged(LibraryChangedEventArgs args) => Changed?.Invoke(this, args);

    public BookCopy AddCopy(string isbn)
    {
        var copy = BookCopy.Create(Guid.NewGuid().ToString("N")[..8], isbn);
        _store.Add(copy);
        OnChanged(new LibraryChangedEventArgs(copy.Id, LibraryChangeKind.BookAdded)); // після зміни, не до
        return copy;
    }

    public Loan IssueCopy(string copyId, string readerId, DateOnly issuedOn)
    {
        var copy = _store.GetById(copyId)
            ?? throw new InvalidOperationException($"Немає примірника з id={copyId}.");
        var loan = Loan.Open(Guid.NewGuid().ToString("N")[..8], copy, readerId, issuedOn); // інваріант може кинути виняток: тоді події не буде
        _store.Update(copy);
        OnChanged(new LibraryChangedEventArgs(copy.Id, LibraryChangeKind.CopyIssued,
            loan.ReaderId, issuedOn.AddDays(LoanDays)));
        return loan;
    }

    public void ReturnCopy(string copyId)
    {
        var copy = _store.GetById(copyId)
            ?? throw new InvalidOperationException($"Немає примірника з id={copyId}.");
        copy.Return();
        _store.Update(copy);
        OnChanged(new LibraryChangedEventArgs(copy.Id, LibraryChangeKind.CopyReturned));
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);

    // Func<BookCopy, bool>: критерій пошуку задає той, хто викликає.
    public IReadOnlyList<BookCopy> FindAll(Func<BookCopy, bool> predicate) =>
        _store.List().Where(predicate).ToList();
}
