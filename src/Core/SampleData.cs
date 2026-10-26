using Core.Domain;
using Core.Dto;

namespace Core;

// Початкові дані для демонстрації (сховище в пам'яті) і для тижня 7.
public static class SampleData
{
    public static IReadOnlyList<BookCopy> Copies() =>
    [
        Copy("C-001", "978-966-00-0001-4"),
        Copy("C-002", "978-966-00-0001-4"),
        Copy("C-003", "978-966-00-0002-1", issued: true),
        Copy("C-004", "978-966-00-0002-1"),
        Copy("C-005", "978-966-00-0003-8"),
        Copy("C-006", "978-966-00-0004-5"),
        Copy("C-007", "978-966-00-0005-2", issued: true),
        Copy("C-008", "978-966-00-0006-9"),
        Copy("C-009", "978-966-00-0007-6"),
        Copy("C-010", "978-966-00-0008-3"),
        Copy("C-011", "978-966-00-0008-3", issued: true),
        Copy("C-012", "978-966-00-0009-0"),
        Copy("C-013", "978-966-00-0010-6"),
        Copy("C-014", "978-966-00-0011-3"),
        Copy("C-015", "978-966-00-0011-3"),
        Copy("C-016", "978-966-00-0003-8"),
    ];

    private static BookCopy Copy(string id, string isbn, bool issued = false) =>
        BookCopy.FromDto(new BookCopyDto(id, isbn, issued));
}
