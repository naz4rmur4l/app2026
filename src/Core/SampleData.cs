using Core.Domain;
using Core.Dto;

namespace Core;

// Початкові дані для демонстрації: 16 примірників (лаб. 5) і 17 видач для звітів (лаб. 7).
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

    // Дати задано зсувом від today: два відкриті записи прострочені (строк 14 днів), один ні.
    // Читачі R-102 і R-103, а також R-104 і R-105 мають однакову кількість видач:
    // саме на них видно, що другий ключ сортування робить топ-N стабільним.
    public static IReadOnlyList<Loan> Loans(DateOnly today) =>
    [
        // відкриті (примірники, видані в Copies())
        MakeLoan("L-001", "C-003", "R-100", today, -30),
        MakeLoan("L-002", "C-007", "R-101", today, -20),
        MakeLoan("L-003", "C-011", "R-102", today, -5),
        // закриті
        MakeLoan("L-004", "C-001", "R-100", today, -90, -80),
        MakeLoan("L-005", "C-002", "R-100", today, -75, -65),
        MakeLoan("L-006", "C-004", "R-100", today, -60, -52),
        MakeLoan("L-007", "C-005", "R-100", today, -45, -40),
        MakeLoan("L-008", "C-006", "R-101", today, -88, -79),
        MakeLoan("L-009", "C-008", "R-101", today, -70, -61),
        MakeLoan("L-010", "C-009", "R-101", today, -50, -44),
        MakeLoan("L-011", "C-010", "R-102", today, -85, -77),
        MakeLoan("L-012", "C-012", "R-102", today, -55, -47),
        MakeLoan("L-013", "C-013", "R-103", today, -82, -74),
        MakeLoan("L-014", "C-014", "R-103", today, -58, -49),
        MakeLoan("L-015", "C-015", "R-103", today, -35, -30),
        MakeLoan("L-016", "C-016", "R-104", today, -25, -18),
        MakeLoan("L-017", "C-001", "R-105", today, -15, -10),
    ];

    private static BookCopy Copy(string id, string isbn, bool issued = false) =>
        BookCopy.FromDto(new BookCopyDto(id, isbn, issued));

    private static Loan MakeLoan(string id, string copyId, string readerId, DateOnly today,
        int issuedDays, int? returnedDays = null) =>
        Loan.FromDto(new LoanDto(id, copyId, readerId, today.AddDays(issuedDays),
            returnedDays is int d ? today.AddDays(d) : null));
}
