# CrossApp

Наскрізний проєкт з крос-платформного програмування.
Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.
Призначення: облік видач примірників книг читачам.

## Запуск

    dotnet build
    dotnet run --project src/Cli                      # data/sample.csv
    dotnet run --project src/Cli шлях/до/файлу.csv    # свій файл

Код завершення: `0` успіх, `1` файл не знайдено.

## Середовище

.NET SDK 10.0, Windows 11/10 x64

## Структура

    CrossApp/
      data/sample.csv
      src/
        Core/
          Dto/       BookDto, ImportResult<T>
          Import/    BookCsvImporter
          EnvironmentInfo.cs
        Cli/         Program.cs (аргументи, виклик імпорту, вивід)

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

    dotnet run --project src/Cli                 # демонстрація домену (лаб. 4)
    dotnet run --project src/Cli data/sample.csv # імпорт CSV (лаб. 3)
