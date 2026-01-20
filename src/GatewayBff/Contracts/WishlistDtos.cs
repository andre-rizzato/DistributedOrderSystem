namespace GatewayBff.Contracts;

public record WishlistDto(
    Guid UserId,
    List<WishlistItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record WishlistItemDto(
    Guid Id,
    Guid ProductId,
    DateTime CreatedAt
);

public record AddToWishlistRequest(Guid ProductId);

public record MoveToCartRequest(string SessionId);
