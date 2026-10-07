# src/AgentService/rag/ingest.py
#
# CLI script that reads the two seed_data/*.jsonl files, embeds every
# record, and upserts them into their respective Qdrant collections. Run
# manually (python -m rag.ingest) whenever the seed corpus changes - same
# spirit as the Node product's `npm run ingest`, not something that runs
# automatically on every AgentService startup (embedding ~60 records on
# every restart would be wasteful and slow).

import json
from pathlib import Path

from dotenv import load_dotenv

# This script is a standalone entrypoint (python -m rag.ingest), not
# something imported through main.py's chain - main.py calls load_dotenv()
# for itself, but that doesn't help here, so this file needs its own call,
# before any of the rag.* imports below construct clients that read
# os.environ at import time (embeddings.py's voyage_client, in particular).
load_dotenv()

from qdrant_client import models  # noqa: E402 - after load_dotenv(), see above

from rag.config import COLLECTION_FAQ_POLICY, COLLECTION_ORDER_SUPPORT_NOTES  # noqa: E402
from rag.embeddings import embed_documents  # noqa: E402
from rag.qdrant_client import ensure_collections, get_qdrant_client  # noqa: E402

# seed_data/ lives next to this file regardless of the current working
# directory the script is invoked from - resolving relative to __file__
# instead of "./seed_data" avoids a "works on my machine, breaks in CI"
# surprise.
SEED_DATA_DIR = Path(__file__).parent / "seed_data"


def _load_jsonl(path: Path) -> list[dict]:
    """Reads a .jsonl file (one JSON object per line) into a list of dicts - the standard shape for small, inspectable seed corpora."""
    with path.open(encoding="utf-8") as f:
        # Skip blank lines - the seed files use blank lines to visually
        # group records by issue_tag/category, which would otherwise raise
        # a JSONDecodeError on json.loads("").
        return [json.loads(line) for line in f if line.strip()]


def ingest_order_support_notes() -> int:
    """
    Embeds and upserts every record in order_support_notes.jsonl. The
    embedded TEXT is just note_text (what semantic search matches against);
    every other field (order_id, customer_phone, order_status, issue_tag,
    created_at) travels as Qdrant PAYLOAD - structured metadata used for
    filtering (see rag/retrieval.py), never embedded itself. Returns the
    number of records ingested, so the __main__ block below can print a
    confirmation instead of just trusting it silently worked.
    """
    records = _load_jsonl(SEED_DATA_DIR / "order_support_notes.jsonl")
    vectors = embed_documents([r["note_text"] for r in records])

    get_qdrant_client().upsert(
        COLLECTION_ORDER_SUPPORT_NOTES,
        points=[
            models.PointStruct(
                # Plain integer id (position in the file) - good enough for
                # a static seed corpus that gets fully replaced on every
                # ingest run, not a production system generating new notes
                # continuously (which would need a stable id scheme, e.g.
                # order_id + a sequence number).
                id=i,
                vector=vector,
                payload=record,
            )
            for i, (record, vector) in enumerate(zip(records, vectors))
        ],
    )
    return len(records)


def ingest_faq_policy() -> int:
    """Same shape as ingest_order_support_notes(), over the smaller, unfiltered FAQ/policy corpus."""
    records = _load_jsonl(SEED_DATA_DIR / "faq_policy.jsonl")
    vectors = embed_documents([r["note_text"] for r in records])

    get_qdrant_client().upsert(
        COLLECTION_FAQ_POLICY,
        points=[
            models.PointStruct(id=i, vector=vector, payload=record)
            for i, (record, vector) in enumerate(zip(records, vectors))
        ],
    )
    return len(records)


if __name__ == "__main__":
    # ensure_collections() both creates the collections (if missing) and
    # the payload indexes filtering depends on - must run before any
    # upsert, every time, which is why it's not a separate manual step.
    ensure_collections()

    notes_count = ingest_order_support_notes()
    print(f"Ingested {notes_count} records into '{COLLECTION_ORDER_SUPPORT_NOTES}'.")

    faq_count = ingest_faq_policy()
    print(f"Ingested {faq_count} records into '{COLLECTION_FAQ_POLICY}'.")
