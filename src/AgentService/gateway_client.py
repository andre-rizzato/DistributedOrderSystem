# src/AgentService/gateway_client.py
# Real call to GatewayBff - the first time in the course that an agent talks
# to the real backend instead of a mock (PEDIDOS_MOCK from Weeks 6-9).
# Route confirmed by reading GatewayBff's source code, not documentation:
# GatewayBff/Controllers/QueriesController.cs -> [Route("api/queries")],
# GET "orders/{id:int}" -> GatewayBff/Queries/GetOrderByIdQuery.cs.

import os

import httpx

GATEWAY_BFF_URL = os.getenv("GATEWAY_BFF_URL", "http://localhost:5189")


def get_order_by_id(order_number: str) -> dict | None:
    # order_id on GatewayBff is an int (id:int in the route) - if the number
    # extracted by the classifier isn't a valid int, treat it as "not found"
    # instead of letting the exception bubble up.
    if not order_number.isdigit():
        return None

    with httpx.Client(timeout=10.0) as client:
        response = client.get(f"{GATEWAY_BFF_URL}/api/queries/orders/{order_number}")

    if response.status_code == 404:
        return None
    response.raise_for_status()
    return response.json()
