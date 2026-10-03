# src/AgentService/connectors/__init__.py
# Public surface of the connectors package. graph.py (and any future graph
# module for scheduling/sales) imports factories from here instead of
# reaching into order_rest.py / scheduling_google_calendar.py / etc.
# directly - keeps the "which concrete backend is wired in" decision in one
# place, selected by environment variables, same pattern config.ts uses on
# the Node side of this product (LLM_PROVIDER, EMBEDDING_PROVIDER).

from .base import CatalogBackend, CheckoutBackend, OrderBackend, SchedulingBackend
from .factory import get_order_backend, get_scheduling_backend, get_catalog_backend, get_checkout_backend

__all__ = [
    "OrderBackend",
    "SchedulingBackend",
    "CatalogBackend",
    "CheckoutBackend",
    "get_order_backend",
    "get_scheduling_backend",
    "get_catalog_backend",
    "get_checkout_backend",
]
