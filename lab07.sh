mkdir -p src/Core/Reports

cat > src/Core/Reports/ReaderLine.cs <<'EOF'
namespace Core.Reports;

// Рядок звіту «читач і кількість його видач». record: незмінний, зручний для порівняння в тестах.
public sealed record ReaderLine(string ReaderId, int Loans);
EOF

cat > src/Core/Reports/OverdueLine.cs <<'EOF'
namespace Core.Reports;

// Рядок звіту про прострочену видачу. Сутність Loan назовні не віддається.
public sealed record OverdueLine(string LoanId, string CopyId, string ReaderId, DateOnly DueDate, int DaysOverdue);
EOF

cat > src/Core/Reports/GroupSummary.cs <<'EOF'
namespace Core.Reports;

// Підсумок групи: ключ, кількість примірників у групі і скільки з них зараз видано.
public sealed record GroupSummary(string Key, int Count, int Issued);
EOF

cat > src/Core/Reports/ReportService.cs <<'EOF'
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
EOF

# SampleData: додаємо видачі (відносно «сьогодні», щоб вивід не залежав від дати запуску)
cat > src/Core/SampleData.cs <<'EOF'
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
EOF

cat > src/Cli/ReportCommands.cs <<'EOF'
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
EOF

cat > src/Cli/Program.cs <<'EOF'
using System.Text;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = Encoding.UTF8; // щоб кирилиця читалась у консолі Windows

// Підкоманда: перший аргумент, що не починається з "--" (прапорець --file підкомандою не вважається).
string? command = args.FirstOrDefault(a => !a.StartsWith("--"));

// Попередні лабораторні лишаються доступними: шлях до CSV (лаб. 3) і --domain (лаб. 4).
if (command is not null && !command.StartsWith("report-"))
    return Demos.RunImport(command);
if (args.Contains("--domain"))
    return Demos.RunDomain();

// ===== Composition root: єдине місце, де названо конкретні класи =====
bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
ILibraryStore store = useFile
    ? new FileLibraryStore(dataPath)
    : new InMemoryLibraryStore(SampleData.Copies());
var service = new LendingService(store);
// =====================================================================

// Лаб. 7: підкоманди звітів report-*
if (command is not null)
    return ReportCommands.Run(command, service);

Console.WriteLine($"Сховище: {store.GetType().Name}");
if (useFile)
    Console.WriteLine($"Файл даних: {Path.GetFullPath(dataPath)}");
Console.WriteLine($"Записів на старті: {service.All().Count}");

// ===== Лаб. 6: підписка на події (Core лише піднімає подію) =====
var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

// Підписник 1: локальний метод (його можна згодом зняти через -=).
void OnLibraryChanged(object? sender, LibraryChangedEventArgs e)
{
    string details = e.Kind == LibraryChangeKind.CopyIssued
        ? $"видано читачу {e.ReaderId} до дати {e.DueDate:yyyy-MM-dd}"
        : "";
    var line = $"{e.At:HH:mm:ss} {e.Kind,-12} {e.CopyId} {details}".TrimEnd();
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}
service.Changed += OnLibraryChanged;

// Підписник 2: лямбда із замиканням над локальною змінною changes.
int changes = 0;
service.Changed += (_, _) => changes++;
// ================================================================

DateOnly today = DateOnly.FromDateTime(DateTime.Today);

Console.WriteLine();
Console.WriteLine("=== Сценарій 1: додати, видати, показати, знайти, повернути ===");

BookCopy created = service.AddCopy("978-966-00-0001-4");
Console.WriteLine($"Додано: {created}");

Loan loan = service.IssueCopy(created.Id, "R-100", today);
Console.WriteLine($"Видано: {loan}");

Console.WriteLine("Список:");
foreach (BookCopy c in service.All())
    Console.WriteLine($"  {c.Id,-9} {c.Isbn,-18} {(c.IsIssued ? "видано" : "у фонді")}");

BookCopy? found = service.Find(created.Id.ToUpperInvariant()); // пошук не залежить від регістру
Console.WriteLine($"Знайдено за id {created.Id.ToUpperInvariant()}: {found?.ToString() ?? "(немає)"}");

service.ReturnCopy(created.Id);
Console.WriteLine($"Повернено: {service.Find(created.Id)}");
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: відмови (виняток є, рядка події немає) ===");
Try("повернути вдруге", () => service.ReturnCopy(created.Id));
Try("видати неіснуючий id", () => service.IssueCopy("no-such-id", "R-101", today));
Try("видати вже виданий примірник", () =>
{
    service.IssueCopy(created.Id, "R-102", today);   // успіх: подія є
    service.IssueCopy(created.Id, "R-103", today);   // інваріант: події немає
});
Try("додати дубль id у сховище",
    () => store.Add(BookCopy.Create(created.Id.ToUpperInvariant(), "978-966-00-0002-1")));
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 3: відписка ===");
service.Changed -= OnLibraryChanged;
service.ReturnCopy(created.Id); // тиха операція для консолі й файлу
Console.WriteLine($"Подій отримано: {changes}"); // лічильник-лямбда ще підписаний: +1

Console.WriteLine();
Console.WriteLine("=== Сценарій 4: Func замість події ===");
var issued = service.FindAll(c => c.IsIssued);
Console.WriteLine($"FindAll(c => c.IsIssued): {issued.Count}");
foreach (BookCopy c in issued)
    Console.WriteLine($"  {c.Id} {c.Isbn}");

Console.WriteLine();
Console.WriteLine($"Записів наприкінці: {service.All().Count}");
Console.WriteLine($"Лог подій: {logPath}");
Console.WriteLine($"Звіти (лаб. 7): dotnet run --project src/Cli -- {ReportCommands.Usage}");
return 0;

static void Try(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: помилка — {ex.Message}");
    }
}
EOF

# README: дерево проєкту
awk '{print} /Events\/ +LibraryChangeKind/ && !r {print "          Reports/       ReportService, ReaderLine, OverdueLine, GroupSummary  # звіти LINQ (лаб. 7)"; r=1}' README.md > README.tmp && mv README.tmp README.md
awk '{print} /Demos\.cs +# демонстрації/ && !c {print "          ReportCommands.cs                 # підкоманди report-* (лаб. 7)"; c=1}' README.md > README.tmp && mv README.tmp README.md

cat >> README.md <<'EOF'

## Лабораторна 7: звіти через LINQ to Objects

Звіти лежать у `src/Core/Reports`. `ReportService` приймає `IEnumerable<T>` (не сховище) і повертає `IReadOnlyList<DTO>`
(record-и `ReaderLine`, `OverdueLine`, `GroupSummary`), тому клієнт не залежить від сутностей. Core нічого не знає про `Console`.

| Метод | Питання | Оператори |
|---|---|---|
| `TopReaders(loans, n)` | топ-N читачів за кількістю видач | `GroupBy`, `Select`, `OrderByDescending`, `ThenBy`, `Take` |
| `Overdue(loans, today)` | які видачі прострочені (строк 14 днів) | `Where`, `OrderBy`, `ThenBy`, `Select` |
| `ByIsbn(copies)` | скільки примірників кожної назви і скільки з них видано | `GroupBy`, `Select`, `Count`, `OrderByDescending`, `ThenBy` |
| `Totals(copies)` | скільки назв, примірників, середня і максимальна кількість на назву | `GroupBy`, `Sum`, `Average`, `Max`, іменований кортеж |

- Топ-N має другий ключ сортування (`ReaderId`), тому порядок рядків однаковий за кожного запуску.
- Порожнє джерело обробляється без винятку: `Totals` перевіряє `Count == 0` до виклику `Average` і `Max`.
- Видачі (`Loan`) у сховищі не зберігаються, тому `report-top` і `report-overdue` беруть демо-дані `SampleData.Loans(today)`;
  `report-groups` і `report-sum` працюють з поточним сховищем. Дати демо-видач задано зсувом від сьогодні, вивід не залежить від дня запуску.

### Відкладене виконання

`Where(...)` лише описує запит і нічого не обчислює. Запит виконується тоді, коли результат хтось перебирає або викликає
завершальний оператор (`Count()`, `ToList()`, `foreach`). У команді `report-lazy` запит `source.Where(c => c.IsIssued)` будується один раз.
Перший `query.Count()` проходить список і дає 3. Після `source.Add(...)` другий `query.Count()` проходить уже змінений список і дає 4,
бо довжина в запиті не зберігається. `ToList()` фіксує результат: `snapshot.Count` читає готову властивість, і
після `source.Clear()` він лишається 4.

### Запуск

    dotnet run --project src/Cli -- report-top        # топ-5 читачів
    dotnet run --project src/Cli -- report-overdue    # прострочені видачі
    dotnet run --project src/Cli -- report-groups     # примірники за ISBN
    dotnet run --project src/Cli -- report-sum        # підсумки й середнє
    dotnet run --project src/Cli -- report-lazy       # демо відкладеного виконання
    dotnet run --project src/Cli -- report-empty      # звіти на порожньому джерелі
    dotnet run --project src/Cli                      # сценарії лаб. 5 і 6 (подія, лог)

Прапорець `--file` не вважається назвою звіту: `-- report-sum --file` читає дані з файлового сховища.
EOF
