using Core.Domain;
using Core.Services;

namespace Core.Reports;

// Звіти приймають IEnumerable<T>, а не сховище: чисті функції легко перевірити тестом
// і викликати з будь-якого джерела. Нічого не знають про Console. Повертають DTO, а не сутності.
public static class ReportService
{
    // 1) групування + топ-N. Другий ключ сортування (ReaderId) робить порядок однаковим за кожного запуску.
    public static IReadOnlyList<ReaderLine> TopReaders(IEnumerable<Loan> loans, int n) =>
        loans.GroupBy(l => l.ReaderId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ReaderLine(g.Key, g.Count()))
            .OrderByDescending(r => r.Loans)
            .ThenBy(r => r.ReaderId, StringComparer.Ordinal)
            .Take(n)
            .ToList();

    // 2) фільтр + сортування + проєкція в DTO. Where стоїть до OrderBy: сортуємо лише потрібне.
    public static IReadOnlyList<OverdueLine> Overdue(IEnumerable<Loan> loans, DateOnly today) =>
        loans.Where(l => !l.IsClosed && DueDate(l) < today)
            .OrderBy(l => DueDate(l)).ThenBy(l => l.Id, StringComparer.Ordinal)
            .Select(l => new OverdueLine(l.Id, l.CopyId, l.ReaderId, DueDate(l), today.DayNumber - DueDate(l).DayNumber))
            .ToList();

    // 3) групування за ISBN з підрахунком усередині групи: скільки примірників і скільки з них видано.
    public static IReadOnlyList<GroupSummary> ByIsbn(IEnumerable<BookCopy> copies) =>
        copies.GroupBy(c => c.Isbn, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(g.Key, g.Count(), g.Count(c => c.IsIssued)))
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Key, StringComparer.Ordinal)
            .ToList();

    // 4) агрегати з обробкою порожньої послідовності: Average і Max на порожній кидають виняток.
    public static (int Titles, int Copies, double Average, int Max) Totals(IEnumerable<BookCopy> copies)
    {
        var perTitle = copies.GroupBy(c => c.Isbn, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Count())
            .ToList();
        return perTitle.Count == 0
            ? (0, 0, 0d, 0)
            : (perTitle.Count, perTitle.Sum(), perTitle.Average(), perTitle.Max());
    }

    private static DateOnly DueDate(Loan loan) => loan.IssuedOn.AddDays(LendingService.LoanDays);
}
