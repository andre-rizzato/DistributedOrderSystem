# src/AgentService/connectors/base.py
# The four domain-capability interfaces from the "Mapa de Capacidades" design
# doc (ai-customer-service-agent/docs/artifacts/mapa-capacidades.html).
#
# Why four Protocols instead of one generic "SalesBackend": a payment
# processor (PagSeguro, Stripe) can generate a checkout link but has no idea
# what products a business sells; a product/catalog API can search products
# but has no idea how to take a payment. Forcing both into one interface
# would mean every CheckoutBackend implementation fakes search_products() -
# the interface segregation the course material (Week 10, graph.py's own
# routing-vs-node separation) already pushes for. OrderBackend and
# SchedulingBackend stay single interfaces because get_status/cancel and
# check_availability/book both belong to ONE real system per client (the
# order system, the calendar) - there's no analogous split there.
#
# Every method is synchronous and returns plain dict | None (never raises for
# "not found" - only for actual failures) so a LangGraph node can call it the
# same way order_info_agent_node already calls gateway_client.get_order_by_id
# in graph.py, with no change to the node's shape.

from typing import Protocol


class OrderBackend(Protocol):
    """Status/cancellation against a client's order system (post-purchase)."""

    def get_status(self, order_id: str) -> dict | None:
        """Returns the order's current state, or None if not found."""
        ...

    def cancel(self, order_id: str) -> dict | None:
        """Attempts to cancel the order; returns the result, or None if not found."""
        ...


class SchedulingBackend(Protocol):
    """Appointment booking - barbershops, salons, clinics."""

    def check_availability(self, service_id: str, date: str) -> list[dict]:
        """Returns the open time slots for a given service on a given date."""
        ...

    def book(self, service_id: str, slot: str, customer: dict) -> dict | None:
        """Books the slot; returns the created appointment, or None if it was taken."""
        ...

    def cancel(self, appointment_id: str) -> dict | None:
        """Cancels an existing appointment; returns the result, or None if not found."""
        ...


class CatalogBackend(Protocol):
    """Product/service lookup for a sales conversation (pre-purchase)."""

    def search_products(self, query: str) -> list[dict]:
        """Free-text search over the client's catalog."""
        ...

    def get_product(self, product_id: str) -> dict | None:
        """A single product's full detail (price, stock, description)."""
        ...


class CheckoutBackend(Protocol):
    """Generates a hosted, PCI-compliant checkout link - never collects card
    data itself. See the "Segurança do checkout" section of the design doc:
    the agent's job stops at handing the customer this URL."""

    def create_checkout_link(self, order_summary: dict) -> dict:
        """order_summary: {"items": [...], "currency": "BRL", "customer": {...}}.
        Returns {"checkout_url": str, "reference_id": str, "expires_at": str | None}."""
        ...
