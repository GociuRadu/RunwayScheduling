using System.Net;
using MediatR;

namespace Modules.Scenarios.Application.UseCases.GetScenarioConfigs;

public sealed record ScenarioConfigQuery() : IRequest<List<ScenarioConfigDto>>;