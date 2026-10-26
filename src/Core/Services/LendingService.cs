using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

// Бізнес-операції видачі. Тут немає жодної згадки про файл чи словник:
// сервіс бачить лише ILibraryStore.
public sealed class LendingService(ILibraryStore store)
{
    private readonly ILibraryStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public BookCopy AddCopy(string isbn)
    {
        var copy = BookCopy.Create(Guid.NewGuid().ToString("N")[..8], isbn);
        _store.Add(copy);        // інваріанти вже перевірив BookCopy.Create
        return copy;
    }

    public Loan IssueCopy(string copyId, string readerId, DateOnly issuedOn)
    {
        var copy = _store.GetById(copyId)
            ?? throw new InvalidOperationException($"Немає примірника з id={copyId}.");

        var loan = Loan.Open(Guid.NewGuid().ToString("N")[..8], copy, readerId, issuedOn);
        _store.Update(copy);     // без Update у файловому сховищі зміна не потрапила б на диск
        return loan;
    }

    public void ReturnCopy(string copyId)
    {
        var copy = _store.GetById(copyId)
            ?? throw new InvalidOperationException($"Немає примірника з id={copyId}.");

        copy.Return();           // метод сутності з лабораторної 4
        _store.Update(copy);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);
}
