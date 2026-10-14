# src/AgentService/appointment_store.py
# Local storage for the short "protocolo" (confirmation code) a customer
# gets after booking an appointment (calendar integration, 13/10/2026).
#
# WHY a short code instead of the provider's own event id: Google Calendar
# (and Microsoft Graph) event ids are long opaque strings
# ("4h2j8f9qr2bk5l1m0n9p8q7r6s...") - nothing a customer could read back over
# WhatsApp/Telegram to say "cancel appointment X". A 6-digit code is.
#
# WHY a local JSON Lines file instead of a database or the calendar's own
# extended properties: this product already stores every other kind of
# per-turn record this way on the Node side (ConversationStore,
# audit-log.jsonl, usage-log.jsonl) - same reasoning applies here (zero new
# infra, zero new ops burden, a human can `cat`/`grep` it directly) AND,
# unlike a Google-specific lookup, this works identically for Microsoft or
# any future provider without changing how a code gets looked up. The
# calendar event ALSO gets the protocolo stamped into its own
# extendedProperties (see connectors/scheduling_google_calendar.py's book())
# purely as a free, human-visible redundancy - this file is the real
# source of truth for lookups (find()), never a Google API query.
#
# Append-only, same shape as the Node side's audit-log.jsonl: nothing is
# ever rewritten in place, so a record already handed to a customer can
# never silently change out from under them.

import json
import secrets
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

# Tentativas de gerar um código sem colisão antes de desistir - 6 dígitos dá
# 1 milhão de combinações; mesmo com milhares de agendamentos no arquivo, a
# chance de precisar de uma segunda tentativa já é muito baixa, então 10
# tentativas é uma margem generosa, não um número ajustado com cuidado.
_MAX_GENERATION_ATTEMPTS = 10


class AppointmentStore:
    # Preâmbulo: __init__ só guarda o caminho e garante que o diretório
    # exista - criar o diretório aqui (não só no primeiro create()) significa
    # que um erro de permissão/caminho aparece assim que o backend é
    # construído, não silenciosamente na primeira reserva de verdade.
    def __init__(self, path: str) -> None:
        self.path = Path(path)
        self.path.parent.mkdir(parents=True, exist_ok=True)

    # Preâmbulo: _exists() varre o arquivo inteiro procurando o protocolo -
    # O(n) por checagem, mas n aqui é "quantos agendamentos este negócio já
    # teve", não milhões de linhas; um grep linear num arquivo texto é
    # exatamente a troca que vale a pena por não precisar de um banco de
    # verdade (ver o comentário do topo do arquivo). Nunca lança em arquivo
    # ausente - "nunca teve agendamento nenhum ainda" não é um erro.
    def _exists(self, protocolo: str) -> bool:
        if not self.path.exists():
            return False
        with self.path.open("r", encoding="utf-8") as handle:
            for line in handle:
                line = line.strip()
                if not line:
                    continue
                if json.loads(line).get("protocolo") == protocolo:
                    return True
        return False

    # Preâmbulo: generate_protocolo() devolve um código de 6 dígitos ainda
    # não usado - só gera e checa, NÃO grava nada (quem chama decide se e
    # quando persistir, normalmente só depois que o backend de calendário já
    # confirmou a reserva de verdade - ver scheduling_node.py's
    # book_appointment_node). secrets.randbelow() (não random.randint): o
    # resto deste projeto já trata qualquer identificador exposto ao cliente
    # como superfície de segurança (ver sessionId = crypto.randomUUID() do
    # lado Node) - aqui o risco é baixo (não é um token de acesso), mas usar
    # o gerador criptográfico por padrão custa nada e evita ter que
    # justificar a escolha depois.
    def generate_protocolo(self) -> str:
        for _ in range(_MAX_GENERATION_ATTEMPTS):
            candidate = f"{secrets.randbelow(1_000_000):06d}"
            if not self._exists(candidate):
                return candidate
        raise RuntimeError(
            f"Não foi possível gerar um protocolo sem colisão após {_MAX_GENERATION_ATTEMPTS} tentativas."
        )

    # Preâmbulo: create() grava UMA linha JSONL - chamado só depois que o
    # SchedulingBackend já confirmou a reserva (nunca antes, pra este arquivo
    # nunca ter um protocolo "fantasma" que não corresponde a nada de
    # verdade na agenda). `protocolo` vem de generate_protocolo(), chamado
    # separadamente - ver o preâmbulo dele pra entender por que as duas
    # etapas são funções diferentes.
    def create(
        self,
        *,
        protocolo: str,
        provider: str,
        service_id: str,
        slot: str,
        customer: dict,
        backend_event_id: Optional[str],
    ) -> None:
        record = {
            "protocolo": protocolo,
            "provider": provider,
            "service_id": service_id,
            "slot": slot,
            "customer": customer,
            "backend_event_id": backend_event_id,
            "created_at": datetime.now(timezone.utc).isoformat(),
        }
        with self.path.open("a", encoding="utf-8") as handle:
            handle.write(json.dumps(record, ensure_ascii=False) + "\n")

    # Preâmbulo: find() é o caminho de leitura - usado quando um cliente
    # cita o protocolo (ex.: pedindo pra cancelar). Devolve o registro
    # completo (inclusive backend_event_id, pro atendente humano localizar o
    # evento de verdade na agenda) ou None se o protocolo não existe - nunca
    # lança, mesmo erro de "não encontrado" de qualquer outro find() deste
    # projeto (OrderBackend.get_status, etc.).
    def find(self, protocolo: str) -> Optional[dict]:
        if not self.path.exists():
            return None
        with self.path.open("r", encoding="utf-8") as handle:
            for line in handle:
                line = line.strip()
                if not line:
                    continue
                record = json.loads(line)
                if record.get("protocolo") == protocolo:
                    return record
        return None
