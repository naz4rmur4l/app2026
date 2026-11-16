using Core;
using Core.Domain;
using Core.Reports;
using Core.Services;

// Підкоманди звітів: лише виклики ReportService і вивід (форматування).
// Усі запити (Where, GroupBy, ...) лежать у Core/Reports.
static class ReportCommands
{
    public const string Usage = "report-top | report-overdue | report-groups | report-sum | report-lazy | report-empty";

    public static int Run(string command, LendingService service)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        var copies = service.All();            // знімок поточного стану сховища
        var loans = SampleData.Loans(today);   // видачі поки що не зберігаються в сховищі: беремо демо-дані

        switch (command)
        {
            case "report-top":
                Console.WriteLine("Топ-5 читачів за кількістю видач:");
                foreach (var r in ReportService.TopReaders(loans, 5))
                    Console.WriteLine($"  {r.ReaderId,-8} видач: {r.Loans,3}");
                break;

            case "report-overdue":
                Console.WriteLine($"Прострочені видачі на {today:yyyy-MM-dd}:");
                foreach (var l in ReportService.Overdue(loans, today))
                    Console.WriteLine($"  {l.LoanId,-6} {l.CopyId,-6} {l.ReaderId,-6} строк {l.DueDate:yyyy-MM-dd}  прострочено {l.DaysOverdue,3} дн.");
                break;

            case "report-groups":
                Console.WriteLine("Примірники за ISBN:");
                foreach (var g in ReportService.ByIsbn(copies))
                    Console.WriteLine($"  {g.Key,-18} примірників: {g.Count,3}  видано: {g.Issued,3}");
                break;

            case "report-sum":
                var t = ReportService.Totals(copies);
                Console.WriteLine($"  назв {t.Titles}, примірників {t.Copies}, середнє на назву {t.Average:F2}, макс {t.Max}");
                break;

            case "report-lazy":
                LazyDemo(service);
                break;

            case "report-empty":
                // Порожнє джерело: жоден звіт не падає.
                Console.WriteLine($"  топ: {ReportService.TopReaders([], 5).Count} рядків");
                Console.WriteLine($"  прострочені: {ReportService.Overdue([], today).Count} рядків");
                Console.WriteLine($"  групи: {ReportService.ByIsbn([]).Count} рядків");
                var e = ReportService.Totals([]);
                Console.WriteLine($"  підсумки: назв {e.Titles}, примірників {e.Copies}, середнє {e.Average:F2}, макс {e.Max}");
                break;

            default:
                Console.WriteLine($"Невідома команда {command}. Доступні: {Usage}");
                return 1;
        }
        return 0;
    }

    // Відкладене виконання. service.All() щоразу повертає новий знімок, тому
    // джерелом для демо беремо локальний список.
    private static void LazyDemo(LendingService service)
    {
        var source = service.All().ToList();
        var query = source.Where(c => c.IsIssued);          // запит побудовано, але не виконано
        Console.WriteLine($"Виданих до змін: {query.Count()}");

        var extra = BookCopy.Create("tmp00001", "978-966-00-9999-9");
        extra.Issue();
        source.Add(extra);
        Console.WriteLine($"Виданих після змін: {query.Count()}"); // число інше: запит виконався заново

        var snapshot = query.ToList();                      // результат зафіксовано
        source.Clear();
        Console.WriteLine($"У знімку лишилось: {snapshot.Count}"); // не змінюється
    }
}
