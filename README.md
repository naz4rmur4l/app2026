# CrossApp

Наскрізний проєкт з крос-платформного програмування.
Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.
Призначення: облік видач примірників книг читачам.

## Структура рішення

CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/
        ├── Cli.csproj   (ProjectReference → Core)
        └── Program.cs

Core — бібліотека класів (class library), не має точки входу. Містить логіку, яку
надалі використовуватимуть і Cli, і майбутні Api/Blazor-проєкти. Cli залежить від
Core (ProjectReference), зворотної залежності немає.

## Запуск

dotnet build
dotnet run --project src/Cli

## Публікація

dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r win-x64 --self-contained false

| RID     | Режим               | Розмір publish | Runtime потрібен |
|---------|---------------------|----------------|-------------------|
| win-x64 | self-contained      | ~0.19 МБ         | ні                |
| win-x64 | framework-dependent | ~0.19 МБ          | так (.NET 10)     |

- **self-contained** — у каталог publish кладеться код, залежності та копія .NET
  runtime. Застосунок працює на машині без встановленого .NET, але каталог значно
  більший і прив'язаний до конкретної RID.
- **framework-dependent** — лише код і залежності NuGet, без runtime. Каталог
  малий, але на цільовій машині має бути встановлений сумісний .NET 10.

## Структура Core (заплановано на наступні тижні)

- `Core/Dto/` — record-типи форматів даних (тиждень 3)
- `Core/Domain/` — сутності з поведінкою та інваріантами (тиждень 4)
- `Core/Storage/` — реалізації сховищ (тиждень 5)

## Середовище

.NET SDK 10.0, Windows 11/10 x64
