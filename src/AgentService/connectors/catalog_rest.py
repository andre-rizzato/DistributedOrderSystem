# src/AgentService/connectors/catalog_rest.py
# Generic CatalogBackend for "the client has their own product API" - the
# other half of SalesBackend in the design doc (checkout is
# sales_payment_link.py). For DistributedOrderSystem itself this points at
# ProductService through GatewayBff, same demo-client role order_rest.py
# plays for OrderBackend.

import os
from urllib.parse import quote

import httpx


class RestCatalogBackend:
    def __init__(
        self,
        base_url: str,
        search_path: str = "/products?q={query}",
        get_path_template: str = "/products/{id}",
        auth_header: str | None = None,
        auth_value: str | None = None,
        timeout_seconds: float = 10.0,
    ) -> None:
        self.base_url = base_url.rstrip("/")
        self.search_path = search_path
        self.get_path_template = get_path_template
        self.timeout_seconds = timeout_seconds
        self._headers = {auth_header: auth_value} if auth_header and auth_value else {}

    def _client(self) -> httpx.Client:
        return httpx.Client(base_url=self.base_url, headers=self._headers, timeout=self.timeout_seconds)

    def search_products(self, query: str) -> list[dict]:
        path = self.search_path.format(query=quote(query, safe=""))
        with self._client() as client:
            response = client.get(path)
        response.raise_for_status()
        body = response.json()
        # Tolerate either a bare list or a {"items": [...]} envelope - the
        # two shapes real product APIs return most often, and guessing
        # wrong here would silently return zero products instead of failing
        # loudly, which is worse.
        return body if isinstance(body, list) else body.get("items", [])

    def get_product(self, product_id: str) -> dict | None:
        path = self.get_path_template.format(id=product_id)
        with self._client() as client:
            response = client.get(path)
        if response.status_code == 404:
            return None
        response.raise_for_status()
        return response.json()


def build_from_env() -> RestCatalogBackend:
    base_url = os.getenv("CATALOG_API_BASE_URL") or os.getenv("GATEWAY_BFF_URL", "http://localhost:5189")
    return RestCatalogBackend(
        base_url=base_url,
        search_path=os.getenv("CATALOG_API_SEARCH_PATH", "/api/queries/products?search={query}"),
        get_path_template=os.getenv("CATALOG_API_GET_PATH", "/api/queries/products/{id}"),
        auth_header=os.getenv("CATALOG_API_AUTH_HEADER"),
        auth_value=os.getenv("CATALOG_API_AUTH_VALUE"),
    )
