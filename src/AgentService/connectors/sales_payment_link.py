# src/AgentService/connectors/sales_payment_link.py
# CheckoutBackend implementations - the "Decisão · pagamento" piece of the
# design doc. Each one does exactly one thing: take an order_summary and
# return a URL to the processor's own hosted checkout page. Neither class
# ever touches a card number - that's the whole point (see "Segurança do
# checkout" in mapa-capacidades.html).
#
# StripeCheckoutBackend is implemented and matches Stripe's stable, widely
# documented Checkout Sessions API (unchanged in shape for years) - safe to
# trust without a live test here.
#
# PagSeguroCheckoutBackend follows PagBank's current Orders/Checkout API
# shape as best known, but PagBank's API surface moves more than Stripe's -
# VERIFY the endpoint path and payload against https://dev.pagbank.com.br
# before pointing this at production. Treat the endpoint/payload here as a
# well-informed draft, not a verified contract - this session had no live
# PagSeguro credential to test against.

import os

import httpx


class StripeCheckoutBackend:
    def __init__(self, secret_key: str, success_url: str, cancel_url: str) -> None:
        self.secret_key = secret_key
        self.success_url = success_url
        self.cancel_url = cancel_url

    def create_checkout_link(self, order_summary: dict) -> dict:
        # Stripe's API is form-encoded, not JSON, and uses bracket notation
        # for arrays/nested objects (line_items[0][price_data][...]) - httpx
        # handles this fine if we hand it a list of tuples instead of a dict,
        # since a dict would collapse repeated keys.
        form: list[tuple[str, str]] = [
            ("mode", "payment"),
            ("success_url", self.success_url),
            ("cancel_url", self.cancel_url),
        ]
        for i, item in enumerate(order_summary["items"]):
            form.append((f"line_items[{i}][quantity]", str(item["quantity"])))
            form.append((f"line_items[{i}][price_data][currency]", order_summary.get("currency", "usd").lower()))
            form.append((f"line_items[{i}][price_data][product_data][name]", item["name"]))
            # Stripe wants the unit price in the smallest currency unit
            # (cents), never a float - avoids the classic float-rounding bug.
            form.append((f"line_items[{i}][price_data][unit_amount]", str(item["unit_amount_cents"])))

        with httpx.Client(base_url="https://api.stripe.com/v1", auth=(self.secret_key, ""), timeout=10.0) as client:
            response = client.post("/checkout/sessions", data=form)
        response.raise_for_status()
        session = response.json()
        return {
            "checkout_url": session["url"],
            "reference_id": session["id"],
            "expires_at": None,
        }


class PagSeguroCheckoutBackend:
    def __init__(self, token: str, notification_url: str | None = None, sandbox: bool = False) -> None:
        self.token = token
        self.notification_url = notification_url
        self.base_url = "https://sandbox.api.pagseguro.com" if sandbox else "https://api.pagseguro.com"

    def create_checkout_link(self, order_summary: dict) -> dict:
        payload = {
            "reference_id": order_summary.get("reference_id"),
            "customer": order_summary.get("customer", {}),
            "items": [
                {
                    "name": item["name"],
                    "quantity": item["quantity"],
                    "unit_amount": item["unit_amount_cents"],
                }
                for item in order_summary["items"]
            ],
            "payment_methods": [
                {"type": "CREDIT_CARD"},
                {"type": "DEBIT_CARD"},
                {"type": "PIX"},
            ],
        }
        if self.notification_url:
            payload["notification_urls"] = [self.notification_url]

        with httpx.Client(
            base_url=self.base_url,
            headers={"Authorization": f"Bearer {self.token}"},
            timeout=10.0,
        ) as client:
            response = client.post("/checkouts", json=payload)
        response.raise_for_status()
        checkout = response.json()
        pay_link = next(
            (link["href"] for link in checkout.get("links", []) if link.get("rel") == "PAY"),
            None,
        )
        return {
            "checkout_url": pay_link,
            "reference_id": checkout.get("id", payload.get("reference_id")),
            "expires_at": checkout.get("expiration_date"),
        }


def build_checkout_backend_from_env() -> StripeCheckoutBackend | PagSeguroCheckoutBackend:
    # CHECKOUT_PROCESSOR selects the connector, same "pick by env var" shape
    # as LLM_PROVIDER / EMBEDDING_PROVIDER on the Node side. Decisão ·
    # pagamento: "pagseguro" for domestic clients (Pix), "stripe" for
    # clients selling internationally.
    processor = os.getenv("CHECKOUT_PROCESSOR", "pagseguro").lower()
    if processor == "stripe":
        return StripeCheckoutBackend(
            secret_key=os.environ["STRIPE_SECRET_KEY"],
            success_url=os.environ["STRIPE_SUCCESS_URL"],
            cancel_url=os.environ["STRIPE_CANCEL_URL"],
        )
    if processor == "pagseguro":
        return PagSeguroCheckoutBackend(
            token=os.environ["PAGSEGURO_TOKEN"],
            notification_url=os.getenv("PAGSEGURO_NOTIFICATION_URL"),
            sandbox=os.getenv("PAGSEGURO_SANDBOX", "false").lower() == "true",
        )
    raise ValueError(f"Unknown CHECKOUT_PROCESSOR: {processor!r} (expected 'pagseguro' or 'stripe')")
