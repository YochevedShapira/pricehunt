using PriceHunt.Api.Validation;
using PriceHunt.Application.History;

namespace PriceHunt.Api.History;

/// <summary>
/// Turns the raw history query string into a validated <see cref="HistoryQuery"/>.
/// </summary>
public interface IHistoryQueryParser
{
    ParseResult<HistoryQuery> Parse(HistoryParameters parameters);
}
