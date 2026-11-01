using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournaments.Api.Authentication;
using Tournaments.Api.Reports;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;

namespace Tournaments.Api.Controllers;

[ApiController]
public sealed class ReportsController(ReportWriter writer) : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    [HttpGet("tournaments/{id:guid}/standings.csv"), AllowAnonymous]
    public async Task<IActionResult> Csv(Guid id, [FromServices] IQueryHandler<GetStandings, Standing[]> handler, CancellationToken ct) =>
        File(writer.Csv(await handler.Handle(new(id), ct)), "text/csv; charset=utf-8", "standings.csv");

    [HttpGet("tournaments/{id:guid}/standings.xlsx"), AllowAnonymous]
    public async Task<IActionResult> Excel(Guid id, [FromServices] IQueryHandler<GetStandings, Standing[]> handler, CancellationToken ct) =>
        File(writer.Excel(await handler.Handle(new(id), ct)), ExcelContentType, "standings.xlsx");

    [HttpGet("trainings/report.xlsx"), Authorize(Roles = Roles.ManageTrainings)]
    public async Task<IActionResult> TrainingExcel([FromQuery] Guid playerId,
        [FromServices] IQueryHandler<GetTrainings, TrainingSession[]> handler, CancellationToken ct) =>
        File(writer.TrainingExcel(await handler.Handle(new(playerId, 200), ct)), ExcelContentType, "trainings.xlsx");
}
