using Core.Domain;

namespace Core.Abstractions;

// Контракт сховища примірників. Лише операції, потрібні сервісу:
// нічого про файл, базу чи пам'ять тут немає.
public interface ILibraryStore
{
    IReadOnlyList<BookCopy> List();
    BookCopy? GetById(string id);
    void Add(BookCopy item);
    void Update(BookCopy item);
    bool Remove(string id);
}
