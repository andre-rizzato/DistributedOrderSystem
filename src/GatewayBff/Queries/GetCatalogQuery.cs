namespace GatewayBff.Queries;

using GatewayBff.Contracts;
using MediatR;

public record GetCatalogQuery() : IRequest<List<CatalogItemDto>>;
