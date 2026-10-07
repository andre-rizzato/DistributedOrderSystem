# src/AgentService/rag/qdrant_client.py
#
# Owns the Qdrant connection and the two collections' schemas. Nothing else
# in rag/ should construct a QdrantClient directly or know the vector
# dimension/distance metric - this is the one place that knowledge lives,
# same reasoning as connectors/factory.py owning which OrderBackend gets
# built.

from qdrant_client import QdrantClient, models

from rag.config import (
    COLLECTION_FAQ_POLICY,
    COLLECTION_ORDER_SUPPORT_NOTES,
    QDRANT_API_KEY,
    QDRANT_URL,
)
from rag.embeddings import embed_documents

# Built once at import time, not per request - QdrantClient keeps its own
# HTTP connection pool internally, so there is no benefit to recreating it
# on every call, only cost.
#
# port=None is load-bearing, not redundant: QdrantClient's `port` parameter
# defaults to 6333 and gets appended to the host EVEN when `url` is already
# a full https:// URL with no port of its own - found running this against
# the real Azure Container Apps Qdrant instance (which is only reachable on
# the standard HTTPS port 443 externally; 6333 is just the container's
# internal target port, not exposed). Without port=None, every request
# silently tried host:6333 instead of the URL's actual port and timed out
# with no useful error message. Harmless for the local docker-compose
# Qdrant, whose URL already embeds ":6333" explicitly in the string itself.
_client = QdrantClient(url=QDRANT_URL, api_key=QDRANT_API_KEY, port=None)


def get_qdrant_client() -> QdrantClient:
    """Returns the shared client instance - the only way any other file in rag/ should reach Qdrant."""
    return _client


def _create_collection_if_missing(collection_name: str, vector_size: int) -> None:
    """
    Creates a collection with cosine distance (same metric used throughout
    the course - Voyage embeddings are compared by cosine, not Euclidean
    distance, see CursoClaude week6's writeup on why). Idempotent: does
    nothing if the collection already exists, so ingest.py can be re-run
    safely without needing to check state itself first.
    """
    if _client.collection_exists(collection_name):
        return
    _client.create_collection(
        collection_name,
        vectors_config=models.VectorParams(size=vector_size, distance=models.Distance.COSINE),
    )


def ensure_collections() -> None:
    """
    Creates both collections (if missing) and the payload indexes that make
    filtered search on order_support_notes fast - Qdrant CAN filter on a
    field without an index, but it then has to scan every point's payload
    instead of using an index lookup, which stops scaling the moment the
    collection grows past a handful of points. Called once by ingest.py
    before upserting any points, and safe to call again on every process
    startup (every call after the first is a no-op).
    """
    # The embedding dimension isn't hardcoded anywhere - it's read from a
    # real embedding call, same principle the course's own qdrant/intro_qdrant.py
    # exercise used: never guess a model's output size, verify it.
    probe_vector = embed_documents(["dimension probe"])[0]
    vector_size = len(probe_vector)

    _create_collection_if_missing(COLLECTION_ORDER_SUPPORT_NOTES, vector_size)
    _create_collection_if_missing(COLLECTION_FAQ_POLICY, vector_size)

    # Keyword indexes on the three fields retrieve.py filters on for
    # order_support_notes (see rag_node.py's privacy guardrail for why
    # customer_phone is always part of that filter). faq_policy has no
    # per-customer data and no filtered search today, so it gets no index.
    for field_name in ("customer_phone", "order_status", "issue_tag"):
        _client.create_payload_index(
            COLLECTION_ORDER_SUPPORT_NOTES,
            field_name=field_name,
            field_schema=models.PayloadSchemaType.KEYWORD,
        )
