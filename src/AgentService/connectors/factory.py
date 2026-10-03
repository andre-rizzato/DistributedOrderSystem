# src/AgentService/connectors/factory.py
# One function per capability, each reading its own env var to decide which
# concrete connector to build - the generalized version of
# `anthropic_client = Anthropic()` at the top of graph.py, but for backends
# instead of the LLM. graph.py (and any future scheduling/sales graph)
# should only ever import from here, never import a concrete connector
# class directly - that's what keeps "swap PagSeguro for Stripe" a config
# change instead of a code change.

import os

from .base import CatalogBackend, CheckoutBackend, OrderBackend, SchedulingBackend
from .catalog_rest import build_from_env as build_catalog_rest
from .order_rest import build_from_env as build_order_rest
from .sales_payment_link import build_checkout_backend_from_env
from .scheduling_google_calendar import GoogleCalendarBackend


def get_order_backend() -> OrderBackend:
    # Only one implementation exists today (RestOrderBackend) - the
    # ORDER_BACKEND switch is here anyway so adding CustomBackend for a
    # specific client later doesn't require touching this file's shape,
    # only adding a branch.
    kind = os.getenv("ORDER_BACKEND", "rest").lower()
    if kind == "rest":
        return build_order_rest()
    raise ValueError(f"Unknown ORDER_BACKEND: {kind!r}")


def get_catalog_backend() -> CatalogBackend:
    kind = os.getenv("CATALOG_BACKEND", "rest").lower()
    if kind == "rest":
        return build_catalog_rest()
    raise ValueError(f"Unknown CATALOG_BACKEND: {kind!r}")


def get_checkout_backend() -> CheckoutBackend:
    # sales_payment_link.py already reads CHECKOUT_PROCESSOR itself (its
    # factory predates this one and is also used standalone in tests) -
    # this just forwards to it for a consistent get_*_backend() shape
    # across all four capabilities.
    return build_checkout_backend_from_env()


def get_scheduling_backend() -> SchedulingBackend:
    kind = os.getenv("SCHEDULING_BACKEND", "google_calendar").lower()
    if kind == "google_calendar":
        # services: "corte:30,coloracao:90" -> {"corte": 30, "coloracao": 90}.
        # A real admin UI (the "tela de configuração do sistema por
        # cliente" from the design doc) would write this as structured
        # config instead of a parsed env string - this is the stand-in
        # until that UI exists.
        services_raw = os.environ["SCHEDULING_SERVICES"]
        services = {}
        for entry in services_raw.split(","):
            service_id, minutes = entry.split(":")
            services[service_id.strip()] = int(minutes)

        return GoogleCalendarBackend(
            service_account_file=os.environ["GOOGLE_SERVICE_ACCOUNT_FILE"],
            calendar_id=os.environ["GOOGLE_CALENDAR_ID"],
            services=services,
            business_hours=(
                int(os.getenv("SCHEDULING_BUSINESS_HOUR_START", "9")),
                int(os.getenv("SCHEDULING_BUSINESS_HOUR_END", "18")),
            ),
            slot_minutes=int(os.getenv("SCHEDULING_SLOT_MINUTES", "30")),
            timezone_name=os.getenv("SCHEDULING_TIMEZONE", "America/Sao_Paulo"),
        )
    raise ValueError(f"Unknown SCHEDULING_BACKEND: {kind!r}")
