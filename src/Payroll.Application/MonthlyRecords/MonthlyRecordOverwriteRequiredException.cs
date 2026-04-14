namespace Payroll.Application.MonthlyRecords;

public sealed class MonthlyRecordOverwriteRequiredException : InvalidOperationException
{
    public MonthlyRecordOverwriteRequiredException()
        : base("Der Monat enthaelt bereits gespeicherte Daten. Bitte Ueberschreiben bestaetigen.")
    {
    }
}
