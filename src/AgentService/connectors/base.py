# src/AgentService/connectors/base.py
# The four domain-capability interfaces this agent builds pluggable
# backends for: OrderBackend, SchedulingBackend, CatalogBackend,
# CheckoutBackend.
#
# Why four Protocols instead of one generic "SalesBackend": a payment
# processor (PagSeguro, Stripe) can generate a checkout link but has no idea
# what products a business sells; a product/catalog API can search products
# but has no idea how to take a payment. Forcing both into one interface
# would mean every CheckoutBackend implementation fakes search_products() -
# the interface segregation graph.py's own
# routing-vs-node separation already pushes for. OrderBackend and
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

    def get_status(self, order_id: str, requester_phone: str | None = None) -> dict | None:
        """Returns the order's current state, or None if not found.

        requester_phone (security review 2026-10-04, item #4): the phone
        number/channel identity of whoever is ASKING, threaded in from the
        calling orchestrator's inbound message (a verified sender phone
        number when the channel provides one; None when it doesn't - a
        channel whose chat id isn't a verified phone number has nothing
        trustworthy to compare yet). This
        parameter is accepted everywhere in the OrderBackend hierarchy so
        the plumbing exists end-to-end, but NO generic implementation here
        (RestOrderBackend) actually enforces a match - a generic REST client
        config has no agreed-upon field name for "the phone on file for this
        order", so guessing one would either silently never match (false
        rejections) or silently never check anything (no real protection).
        A real verification check belongs in a client-specific
        implementation (see ExampleCustomOrderBackend in custom.py) that
        knows its own order schema and can compare requester_phone against
        the order's registered phone before returning real data. Until a
        client needs this enforced, the parameter is forward-compatible
        plumbing only - see docs/SECURITY_REVIEW.md item #4 for the fallback
        (verification question) that still needs designing for channels
        with no phone binding.
        """
        ...

    def cancel(self, order_id: str) -> dict | None:
        """Attempts to cancel the order; returns the result, or None if not found.

        NOTE (security review 2026-10-04, item #4): the chatbot's own graph
        (see graph.py's cancel_order_agent_node) deliberately NEVER calls
        this method - cancellation always becomes a human handoff instead,
        because canceling is destructive and this service has no reliable
        way to confirm the requester owns the order. This method stays on
        the Protocol for whatever DOES need to cancel an order on purpose
        (an admin tool, a human-operator panel) once a human has confirmed
        the request - it's the chatbot's automatic path to it that is
        removed, not the capability itself.
        """
        ...


class SchedulingBackend(Protocol):
    """Appointment booking - barbershops, salons, clinics."""

    def check_availability(self, service_id: str, date: str) -> list[dict]:
        """Returns the open time slots for a given service on a given date."""
        ...

    def book(self, service_id: str, slot: str, customer: dict) -> dict | None:
        """Books the slot; returns the created appointment, or None if it was taken.

        Extended contract (calendar integration, 2026-10-11): not every real
        calendar product can create an arbitrary-time event through its API.
        Google Calendar and Microsoft Graph can - they return a dict with
        `"confirmed": True` and the provider's own event id. Calendly
        deliberately does NOT expose a "create a scheduled event at this
        exact time" endpoint to third parties - only the invitee, on
        Calendly's own hosted page, can finish a booking. For a link-based
        backend like CalendlyBackend, book() instead returns a dict with a
        `"scheduling_link"` key (a single-use URL from Calendly's own
        `POST /scheduling_links`) and no `"confirmed"` key. The caller
        (scheduling_node.py's book_appointment_node) branches on which key is
        present to decide whether to tell the customer "agendado" or hand
        them a link to finish the job themselves - see that module's
        docstring for the full reasoning. Every implementation still returns
        None (never raises) when the requested slot/service genuinely
        doesn't exist, same "not found is not an error" rule as before.
        """
        ...

    def cancel(self, appointment_id: str) -> dict | None:
        """Cancels an existing appointment; returns the result, or None if not found.

        NOTE (calendar integration, 2026-10-11, mirrors OrderBackend.cancel's
        note above): the chatbot's own graph (scheduling_node.py's
        cancel_reschedule_appointment_node) deliberately NEVER calls this
        method either - canceling or rescheduling an appointment always
        becomes a human handoff, same reasoning as order cancellation
        (destructive, and this service has no reliable way to confirm the
        requester owns the booking on every channel). This method stays on
        the Protocol for a future human-operator tool to call once a person
        has confirmed the request, not for the chatbot's automatic path.
        """
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
