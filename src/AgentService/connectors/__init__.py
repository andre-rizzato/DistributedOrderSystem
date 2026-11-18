# src/AgentService/connectors/__init__.py
# Public surface of the connectors package. graph.py (and any future graph
# module for scheduling/sales) imports factories from here instead of
# reaching into order_rest.py / scheduling_google_calendar.py / etc.
# directly - keeps the "which concrete backend is wired in" decision in one
# place, selected by environment variables (the same shape used elsewhere
# in this project for provider selection, e.g. LLM/embedding providers).

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
