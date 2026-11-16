namespace Core.Reports;

// Рядок звіту «читач і кількість його видач». record: незмінний, зручний для порівняння в тестах.
public sealed record ReaderLine(string ReaderId, int Loans);
