# src/AgentService/connectors/custom.py
# The "Reuso: nenhum" row of every capability in the connector table - for a
# client with no REST API, no Google Calendar, no supported payment
# processor. There is deliberately no generic "CustomBackend" class here:
# a custom integration is by definition specific to one client's system
# (their database schema, their in-house booking tool, whatever it is), so
# a one-size class would just be a NotImplementedError wearing a costume.
#
# What IS here is a skeleton per protocol, to copy into a new
# connectors/<client_name>_custom.py file and fill in. Keep two things true
# when you do:
#   1. Implement ONLY the protocol(s) this client actually needs - a clinic
#      custom-connector only needs SchedulingBackend, not all four.
#   2. Prefer talking to a thin API you ask the client to put in front of
#      their database over querying it directly (see "Segurança do
#      checkout" / the gateway_client.py history comment - coupling an
#      agent to someone else's schema is exactly the fragility the generic
#      REST connectors above exist to avoid; a custom connector doesn't get
#      a pass on that just because it's custom).


class ExampleCustomOrderBackend:
    """Copy, rename, and implement for a client whose order system is
    neither a REST API (use RestOrderBackend) nor something you can wrap
    quickly - a legacy system, a spreadsheet-backed process, etc."""

    def get_status(self, order_id: str, requester_phone: str | None = None) -> dict | None:
        # requester_phone (security review 2026-10-04, item #4): this is
        # the right layer to do real identity verification, since you
        # (unlike the generic RestOrderBackend) know this client's actual
        # order schema. Pattern to follow once implemented: look up the
        # order, compare requester_phone against the phone registered on
        # it, and return None (or a distinct "not authorized" shape your
        # generate_reply_node handles) on mismatch instead of the real data
        # - never return another customer's order details just because the
        # order_id guess happened to be right.
        raise NotImplementedError("Implement against the client's real order system.")

    def cancel(self, order_id: str) -> dict | None:
        raise NotImplementedError("Implement against the client's real order system.")


class ExampleCustomSchedulingBackend:
    """Copy, rename, and implement for a client whose scheduling tool isn't
    Google Calendar and has no usable API - e.g. you end up polling a
    desktop app's export, or a vendor's undocumented endpoint."""

    def check_availability(self, service_id: str, date: str) -> list[dict]:
        raise NotImplementedError("Implement against the client's real scheduling system.")

    def book(self, service_id: str, slot: str, customer: dict) -> dict | None:
        raise NotImplementedError("Implement against the client's real scheduling system.")

    def cancel(self, appointment_id: str) -> dict | None:
        raise NotImplementedError("Implement against the client's real scheduling system.")
