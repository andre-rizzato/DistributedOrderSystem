# src/AgentService/sale_store.py
# Local storage for the short "reference_code" a customer gets after the
# agent issues a checkout link (sales capability, 2026-10-20) - same role
# appointment_store.py plays for scheduling's "protocolo", copied
# deliberately instead of generalized into a shared module: the two stores
# have different record shapes (product/quantity vs service/slot) and
# keeping them separate means a change to one's fields never risks the
# other's, at the cost of a few duplicated lines.
#
# WHY a short code instead of the processor's own reference_id: Stripe
# session ids and PagSeguro checkout ids are long opaque strings - nothing a
# customer could read back over WhatsApp/Telegram to ask about "pedido X". A
# 6-digit code is.
#
# WHY a local JSON Lines file instead of a database or the processor's own
# metadata: this product already stores every other kind of per-turn
# record this way (ConversationStore, audit-log.jsonl, usage-log.jsonl on
# the Node side, appointment_store.py here) - same reasoning applies (zero
# new infra, zero new ops burden, a human can `cat`/`grep` it directly).
# Unlike appointment_store.py, there is nowhere on the processor's side to
# ALSO stamp this code as a redundant copy (Stripe/PagSeguro don't expose an
# equivalent to Google Calendar's extendedProperties here) - this file is
# the only record, not just the primary one.
#
# Append-only, same shape as appointment_store.py: nothing is ever
# rewritten in place, so a record already handed to a customer can never
# silently change out from under them.
#
# KNOWN LIMITATION (documented, not solved here): neither Stripe nor
# PagSeguro has a webhook wired into this service, so this store never
# learns whether the payment behind a reference_code actually completed -
# it records that a checkout link was ISSUED, not that it was PAID. See the
# "Fora de escopo" section of the sales implementation plan.

import json
import secrets
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

# Mesma margem generosa de appointment_store.py - 6 dígitos dá 1 milhão de
# combinações, 10 tentativas é folga, não um número calibrado com cuidado.
_MAX_GENERATION_ATTEMPTS = 10


class SaleStore:
    # Preâmbulo: __init__ só guarda o caminho e garante que o diretório
    # exista - mesmo raciocínio de AppointmentStore.__init__ (erro de
    # permissão/caminho aparece na construção, não silenciosamente na
    # primeira venda de verdade).
    def __init__(self, path: str) -> None:
        self.path = Path(path)
        self.path.parent.mkdir(parents=True, exist_ok=True)

    # Preâmbulo: _exists() varre o arquivo inteiro - mesma troca O(n) de
    # AppointmentStore._exists(), pelo mesmo motivo (n = "quantas vendas
    # este negócio já teve", não um volume que justifique um banco).
    def _exists(self, reference_code: str) -> bool:
        if not self.path.exists():
            return False
        with self.path.open("r", encoding="utf-8") as handle:
            for line in handle:
                line = line.strip()
                if not line:
                    continue
                if json.loads(line).get("reference_code") == reference_code:
                    return True
        return False

    # Preâmbulo: generate_code() só gera e checa, não grava nada - quem
    # chama decide se e quando persistir (ver sales_node.py's
    # sales_start_checkout_node, que só chama create() DEPOIS que o
    # CheckoutBackend confirmou o link com sucesso). secrets.randbelow()
    # pelo mesmo motivo de generate_protocolo() em appointment_store.py.
    def generate_code(self) -> str:
        for _ in range(_MAX_GENERATION_ATTEMPTS):
            candidate = f"{secrets.randbelow(1_000_000):06d}"
            if not self._exists(candidate):
                return candidate
        raise RuntimeError(
            f"Não foi possível gerar um reference_code sem colisão após {_MAX_GENERATION_ATTEMPTS} tentativas."
        )

    # Preâmbulo: create() grava UMA linha JSONL - chamado só depois que o
    # CheckoutBackend já devolveu um checkout_url de verdade (nunca antes,
    # pra este arquivo nunca ter um reference_code "fantasma" sem link
    # nenhum por trás).
    def create(
        self,
        *,
        reference_code: str,
        product_id: Optional[str],
        quantity: int,
        customer: dict,
        checkout_url: str,
        provider_reference_id: Optional[str],
    ) -> None:
        record = {
            "reference_code": reference_code,
            "product_id": product_id,
            "quantity": quantity,
            "customer": customer,
            "checkout_url": checkout_url,
            "provider_reference_id": provider_reference_id,
            "created_at": datetime.now(timezone.utc).isoformat(),
        }
        with self.path.open("a", encoding="utf-8") as handle:
            handle.write(json.dumps(record, ensure_ascii=False) + "\n")

    # Preâmbulo: find() é o caminho de leitura - usado quando um cliente ou
    # atendente cita o reference_code. Devolve o registro completo ou None
    # se não existe - nunca lança, mesmo padrão de AppointmentStore.find().
    def find(self, reference_code: str) -> Optional[dict]:
        if not self.path.exists():
            return None
        with self.path.open("r", encoding="utf-8") as handle:
            for line in handle:
                line = line.strip()
                if not line:
                    continue
                record = json.loads(line)
                if record.get("reference_code") == reference_code:
                    return record
        return None
