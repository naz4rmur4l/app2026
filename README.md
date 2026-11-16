# CrossApp

Наскрізний проєкт з крос-платформного програмування.
Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.
Призначення: облік видач примірників книг читачам.

## Запуск

    dotnet build
    dotnet run --project src/Cli data/sample.csv      # імпорт CSV (лаб. 3)
    dotnet run --project src/Cli шлях/до/файлу.csv    # свій файл

Код завершення: `0` успіх, `1` файл не знайдено.

## Середовище

.NET SDK 10.0, Windows 11/10 x64

## Структура

    CrossApp/
      data/sample.csv                       # CSV для імпорту (лаб. 3)
      src/
        Core/
          Abstractions/  ILibraryStore      # контракт сховища (лаб. 5)
          Domain/        BookCopy, Loan     # сутності з інваріантами (лаб. 4)
          Dto/           BookDto, BookCopyDto, LoanDto, ImportResult<T>
          Import/        BookCsvImporter    # імпорт CSV (лаб. 3)
          Services/      LendingService     # бізнес-операції, подія Changed (лаб. 5, 6)
          Events/        LibraryChangeKind, LibraryChangedEventArgs  # події (лаб. 6)
          Reports/       ReportService, ReaderLine, OverdueLine, GroupSummary  # звіти LINQ (лаб. 7)
          Storage/       InMemoryLibraryStore, FileLibraryStore
          EnvironmentInfo.cs
          SampleData.cs                     # 16 примірників для демонстрації
        Cli/
          Program.cs                        # composition root і сценарій
          Demos.cs                          # демонстрації лаб. 3 і 4
          ReportCommands.cs                 # підкоманди report-* (лаб. 7)

## Лабораторна 3: імпорт книг із CSV

Формат файлу `data/sample.csv`:

- роздільник: `;` (задано константою `Separator` в `BookCsvImporter`)
- кодування: UTF-8
- колонки: `id;isbn;title;year`
- перший рядок із заголовком необов'язковий (розпізнається за префіксом `id`)
- порожні рядки та рядки, що починаються з `#`, ігноруються
- рік: ціле число від 1450 до поточного року

Імпорт не переривається на пошкодженому рядку: коректні записи завантажуються,
а для кожного пропущеного рядка повертається повідомлення з його номером.

У `sample.csv` навмисно є 3 пошкоджені рядки (тестові дані): замало колонок,
рік поза межами, порожній ISBN.

## Лабораторна 4: доменна модель

Сутності в `src/Core/Domain`: `BookCopy` (примірник) і `Loan` (видача).
Records із лабораторної 3 та нові `BookCopyDto` і `LoanDto` лишаються DTO (формат даних),
а сутності захищають правила. Зв'язок: `ToDto()` / `FromDto()`; `FromDto` проходить ті самі перевірки.

Створення лише через фабричні методи (`BookCopy.Create`, `Loan.Open`), конструктори приватні,
стан змінюється лише методами (`Issue`, `Return`, `Close`).

### Інваріанти

1. Ідентифікатор і ISBN примірника не порожні (`ArgumentException`, `BookCopy.Create`).
2. Ідентифікатори видачі, примірника і читача не порожні (`ArgumentException`, `Loan.Open` / `Loan.FromDto`).
3. Виданий примірник не можна видати повторно (`InvalidOperationException`, `BookCopy.Issue`).
4. Не виданий примірник не можна повернути (`InvalidOperationException`, `BookCopy.Return`).
5. Дата повернення не раніша за дату видачі (`ArgumentOutOfRangeException`, `Loan.Close` / `Loan.FromDto`).
6. Закриту видачу не можна закрити вдруге (`InvalidOperationException`, `Loan.Close`).
7. Видачу можна закрити лише для її примірника (`ArgumentException`, `Loan.Close`).

### Запуск

    dotnet run --project src/Cli -- --domain     # демонстрація домену (лаб. 4)
    dotnet run --project src/Cli data/sample.csv # імпорт CSV (лаб. 3)

## Лабораторна 5: сервісний шар, два сховища, ручне впровадження залежностей

- Інтерфейс сховища: `ILibraryStore` (`src/Core/Abstractions`), п'ять методів: `List`, `GetById`, `Add`, `Update`, `Remove`.
- Реалізації: `InMemoryLibraryStore` (словник у пам'яті, початкові дані через конструктор)
  і `FileLibraryStore` (JSON-файл `data/catalog.json`, кеш у пам'яті, запис на диск після кожної зміни).
- Сервіс: `LendingService(ILibraryStore store)`: `AddCopy`, `IssueCopy`, `ReturnCopy`, `All`, `Find`.
  Залежність передається через конструктор, сервіс не знає, яке саме сховище йому дали.
- Composition root: `src/Cli/Program.cs`, єдине місце, де названо конкретні сховища. Вибір реалізації за аргументом `--file`.
- Контейнер DI не підключено (`Microsoft.Extensions.DependencyInjection` не використовується): залежності передаються вручну.
- У сховищі лежать сутності `BookCopy` (лаб. 4), а на диск потрапляють `BookCopyDto`.

Схема залежностей: `Cli` → `LendingService` → `ILibraryStore` → `InMemoryLibraryStore` | `FileLibraryStore`

### Запуск

    dotnet run --project src/Cli                    # сховище в пам'яті, після виходу дані зникають
    dotnet run --project src/Cli -- --file          # файлове сховище, дані зберігаються між запусками
    dotnet run --project src/Cli -- --domain        # демонстрація домену (лаб. 4)
    dotnet run --project src/Cli data/sample.csv    # імпорт CSV (лаб. 3)

Файл даних створюється поруч зі збіркою: `src/Cli/bin/<конфігурація>/<рантайм>/data/catalog.json`.
Повний шлях програма друкує сама в рядку «Файл даних».

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
