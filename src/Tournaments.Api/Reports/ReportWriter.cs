using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Tournaments.Core.Domain;

namespace Tournaments.Api.Reports;

public sealed class ReportWriter
{
    public byte[] Csv(Standing[] rows)
    {
        var csv = "name,played,wins,losses,scored,conceded\r\n" + string.Join("\r\n",
            rows.Select(s => $"{CsvText(s.Name)},{s.Played},{s.Wins},{s.Losses},{s.Scored},{s.Conceded}"));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
    }
    public byte[] Excel(Standing[] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Standings");
        var headers = new[] { "Player", "Played", "Wins", "Losses", "Scored", "Conceded" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            sheet.Cell(i + 2, 1).Value = row.Name;
            sheet.Cell(i + 2, 2).Value = row.Played;
            sheet.Cell(i + 2, 3).Value = row.Wins;
            sheet.Cell(i + 2, 4).Value = row.Losses;
            sheet.Cell(i + 2, 5).Value = row.Scored;
            sheet.Cell(i + 2, 6).Value = row.Conceded;
        }
        sheet.Range(1, 1, rows.Length + 1, headers.Length).CreateTable();
        sheet.Column(1).Width = 36;
        sheet.Columns(2, 6).Width = 16;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    public byte[] TrainingExcel(TrainingSession[] sessions)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Trainings");
        var headers = new[] { "Date UTC", "Exercise", "Minutes", "Attempts", "Successes", "Accuracy" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < sessions.Length; i++)
        {
            var row = sessions[i];
            sheet.Cell(i + 2, 1).Value = row.PerformedAt.ToString("u", CultureInfo.InvariantCulture);
            sheet.Cell(i + 2, 2).Value = row.Exercise;
            sheet.Cell(i + 2, 3).Value = row.DurationMinutes;
            sheet.Cell(i + 2, 4).Value = row.Attempts;
            sheet.Cell(i + 2, 5).Value = row.SuccessfulAttempts;
            sheet.Cell(i + 2, 6).Value = (double)row.SuccessfulAttempts / row.Attempts;
        }
        sheet.Column(6).Style.NumberFormat.Format = "0.0%";
        sheet.Range(1, 1, sessions.Length + 1, headers.Length).CreateTable();
        sheet.Column(1).Width = 36;
        sheet.Columns(2, 6).Width = 16;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    private static string CsvText(string value)
    {
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') ||
            value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
