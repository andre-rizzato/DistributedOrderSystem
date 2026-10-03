# src/AgentService/connectors/order_rest.py
# Generic OrderBackend implementation for "the client has their own REST API"
# (Decisão · implantação in the design doc - this is the RestOrderBackend row
# of the connector table, Reuso: alto).
#
# This is gateway_client.py generalized: same two operations (get by id,
# cancel by id), but the path template, HTTP method and auth header are
# config instead of hardcoded GatewayBff routes - so the SAME class serves
# DistributedOrderSystem (the demo/reference client) and any other client's
# order API, just with different env vars. No client-specific subclassing
# needed unless a client's API genuinely can't be expressed as "one GET,
# one state-changing call, both keyed by {id}" - CustomBackend is the escape
# hatch for that case (see custom.py).

import os

import httpx


class RestOrderBackend:
    def __init__(
        self,
        base_url: str,
        get_path_template: str = "/orders/{id}",
        cancel_path_template: str = "/orders/{id}/cancel",
        cancel_method: str = "POST",
        auth_header: str | None = None,
        auth_value: str | None = None,
        timeout_seconds: float = 10.0,
    ) -> None:
        self.base_url = base_url.rstrip("/")
        self.get_path_template = get_path_template
        self.cancel_path_template = cancel_path_template
        self.cancel_method = cancel_method.upper()
        self.timeout_seconds = timeout_seconds
        # Only one auth scheme (a single header) on purpose - OAuth2 client
        # credentials, mTLS etc. are real possibilities for some client's
        # API, but they don't fit "config, not code" anymore. A client that
        # needs that graduates to CustomBackend instead of growing this
        # class into a generic HTTP-auth framework.
        self._headers = {auth_header: auth_value} if auth_header and auth_value else {}

    def _client(self) -> httpx.Client:
        return httpx.Client(base_url=self.base_url, headers=self._headers, timeout=self.timeout_seconds)

    def get_status(self, order_id: str) -> dict | None:
        path = self.get_path_template.format(id=order_id)
        with self._client() as client:
            response = client.get(path)
        if response.status_code == 404:
            return None
        response.raise_for_status()
        return response.json()

    def cancel(self, order_id: str) -> dict | None:
        path = self.cancel_path_template.format(id=order_id)
        with self._client() as client:
            response = client.request(self.cancel_method, path)
        if response.status_code == 404:
            return None
        response.raise_for_status()
        return response.json()


def build_from_env() -> RestOrderBackend:
    # ORDER_API_* is the generic name; GATEWAY_BFF_URL is kept as a fallback
    # so an existing DistributedOrderSystem .env (which only sets
    # GATEWAY_BFF_URL) keeps working without edits - the demo client's
    # config is also a valid "generic REST client" config, it just happens
    # to point at GatewayBff's specific routes.
    base_url = os.getenv("ORDER_API_BASE_URL") or os.getenv("GATEWAY_BFF_URL", "http://localhost:5189")
    return RestOrderBackend(
        base_url=base_url,
        get_path_template=os.getenv("ORDER_API_GET_PATH", "/api/queries/orders/{id}"),
        cancel_path_template=os.getenv("ORDER_API_CANCEL_PATH", "/api/commands/orders/cancel/{id}"),
        cancel_method=os.getenv("ORDER_API_CANCEL_METHOD", "PUT"),
        auth_header=os.getenv("ORDER_API_AUTH_HEADER"),
        auth_value=os.getenv("ORDER_API_AUTH_VALUE"),
    )
