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
from .scheduling_calendly import CalendlyBackend
from .scheduling_google_calendar import GoogleCalendarBackend
from .scheduling_microsoft_calendar import MicrosoftCalendarBackend


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


def _parse_scheduling_services() -> dict[str, int]:
    # services: "corte:30,coloracao:90" -> {"corte": 30, "coloracao": 90}.
    # A real admin UI (the "tela de configuração do sistema por cliente"
    # from the design doc) would write this as structured config instead of
    # a parsed env string - this is the stand-in until that UI exists.
    # Shared by every SchedulingBackend that models "duration per service"
    # the same way (Google, Microsoft) - CalendlyBackend doesn't use this at
    # all, since its event type's duration is defined on Calendly's side
    # (see scheduling_calendly.py's __init__ comment).
    services_raw = os.environ["SCHEDULING_SERVICES"]
    services: dict[str, int] = {}
    for entry in services_raw.split(","):
        service_id, minutes = entry.split(":")
        services[service_id.strip()] = int(minutes)
    return services


def get_scheduling_backend() -> SchedulingBackend:
    kind = os.getenv("SCHEDULING_BACKEND", "google_calendar").lower()
    if kind == "google_calendar":
        return GoogleCalendarBackend(
            service_account_file=os.environ["GOOGLE_SERVICE_ACCOUNT_FILE"],
            calendar_id=os.environ["GOOGLE_CALENDAR_ID"],
            services=_parse_scheduling_services(),
            business_hours=(
                int(os.getenv("SCHEDULING_BUSINESS_HOUR_START", "9")),
                int(os.getenv("SCHEDULING_BUSINESS_HOUR_END", "18")),
            ),
            slot_minutes=int(os.getenv("SCHEDULING_SLOT_MINUTES", "30")),
            timezone_name=os.getenv("SCHEDULING_TIMEZONE", "America/Sao_Paulo"),
        )
    if kind == "microsoft_calendar":
        # tenant_id/client_id/client_secret identificam o App registration
        # no Azure AD (permissão Application Calendars.ReadWrite, consentida
        # pelo admin uma vez - ver docs/AGENDA_INTEGRATION.md); user_id é o
        # UPN/GUID da caixa cujo calendário o agente vai ler/escrever.
        return MicrosoftCalendarBackend(
            tenant_id=os.environ["MICROSOFT_TENANT_ID"],
            client_id=os.environ["MICROSOFT_CLIENT_ID"],
            client_secret=os.environ["MICROSOFT_CLIENT_SECRET"],
            user_id=os.environ["MICROSOFT_CALENDAR_USER_ID"],
            services=_parse_scheduling_services(),
            business_hours=(
                int(os.getenv("SCHEDULING_BUSINESS_HOUR_START", "9")),
                int(os.getenv("SCHEDULING_BUSINESS_HOUR_END", "18")),
            ),
            slot_minutes=int(os.getenv("SCHEDULING_SLOT_MINUTES", "30")),
            timezone_name=os.getenv("SCHEDULING_TIMEZONE", "America/Sao_Paulo"),
        )
    if kind == "calendly":
        # Sem SCHEDULING_SERVICES/business_hours aqui de propósito - o
        # CalendlyBackend não modela duração nem horário comercial por conta
        # própria, isso já é configurado NO Calendly (no próprio event type).
        return CalendlyBackend(
            api_token=os.environ["CALENDLY_API_TOKEN"],
            event_type_uri=os.environ["CALENDLY_EVENT_TYPE_URI"],
        )
    raise ValueError(f"Unknown SCHEDULING_BACKEND: {kind!r}")
