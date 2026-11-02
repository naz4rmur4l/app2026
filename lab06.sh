mkdir -p src/Core/Events

cat > src/Core/Events/LibraryChangeKind.cs <<'EOF'
namespace Core.Events;

// Вид зміни. Назви з варіанта «Бібліотека» лабораторної 6.
public enum LibraryChangeKind { BookAdded, CopyIssued, CopyReturned }
EOF

cat > src/Core/Events/LibraryChangedEventArgs.cs <<'EOF'
namespace Core.Events;

// Дані події: що саме сталося. ReaderId і DueDate заповнені лише для видачі.
public sealed class LibraryChangedEventArgs(
    string copyId, LibraryChangeKind kind, string? readerId = null, DateOnly? dueDate = null) : EventArgs
{
    public string CopyId { get; } = copyId;
    public LibraryChangeKind Kind { get; } = kind;
    public string? ReaderId { get; } = readerId;
    public DateOnly? DueDate { get; } = dueDate;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
EOF

cat > src/Core/Services/LendingService.cs <<'EOF'
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

// Попередні лабораторні лишаються доступними: шлях до CSV (лаб. 3) і --domain (лаб. 4).
if (args.Length > 0 && !args[0].StartsWith("--"))
    return Demos.RunImport(args[0]);
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

# README: структура і запуск не змінюються, додаємо опис лаб. 6
awk '{print} /Services\/ +LendingService/ && !d {print "          Events/        LibraryChangeKind, LibraryChangedEventArgs  # події (лаб. 6)"; d=1}' README.md > README.tmp && mv README.tmp README.md
sed -i 's|^\(          Services/      LendingService\)  *# бізнес-операції (лаб. 5)|\1     # бізнес-операції, подія Changed (лаб. 5, 6)|' README.md

cat >> README.md <<'EOF'

## Лабораторна 6: доменні події, делегати Action/Func, лямбди

- Тип даних події в `src/Core/Events`: `LibraryChangeKind` (`BookAdded`, `CopyIssued`, `CopyReturned`)
  і `LibraryChangedEventArgs` (`CopyId`, `Kind`, `ReaderId`, `DueDate`, `At`).
- `LendingService` оголошує `event EventHandler<LibraryChangedEventArgs>? Changed` і один приватний `OnChanged`
  (`Changed?.Invoke(this, args)`). Його викликають лише після успішної зміни стану: якщо інваріант кинув виняток, події немає.
- Core не використовує `Console` і не пише у файл. Підписки лежать у `Cli/Program.cs`:
  локальний метод `OnLibraryChanged` (консоль + `logs/app.log`) і лямбда-лічильник `(_, _) => changes++` (замикання).
- Відписка `service.Changed -= OnLibraryChanged;`: після неї консоль і файл мовчать, а лічильник-лямбда продовжує рахувати.
- `FindAll(Func<BookCopy, bool> predicate)`: делегат Func там, де потрібно повернути значення, подія для цього не підходить.
- Файл логу: `src/Cli/bin/<конфігурація>/<рантайм>/logs/app.log` (шлях через `Path.Combine`, каталог створюється кодом).

### Запуск

    dotnet run --project src/Cli                    # сценарії лаб. 5 і 6, події в консолі та в logs/app.log
    dotnet run --project src/Cli -- --file          # те саме з файловим сховищем
EOF
