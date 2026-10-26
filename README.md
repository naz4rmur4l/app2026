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
          Services/      LendingService     # бізнес-операції (лаб. 5)
          Storage/       InMemoryLibraryStore, FileLibraryStore
          EnvironmentInfo.cs
          SampleData.cs                     # 16 примірників для демонстрації
        Cli/
          Program.cs                        # composition root і сценарій
          Demos.cs                          # демонстрації лаб. 3 і 4

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
