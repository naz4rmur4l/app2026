namespace Core.Reports;

// Рядок звіту про прострочену видачу. Сутність Loan назовні не віддається.
public sealed record OverdueLine(string LoanId, string CopyId, string ReaderId, DateOnly DueDate, int DaysOverdue);
