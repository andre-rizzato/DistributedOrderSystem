# src/AgentService/rag/embeddings.py
#
# Thin wrapper around the Voyage AI client - the same embedding provider
# used throughout the course (CursoClaude/ai/week3 onward). Kept as its own
# tiny module (instead of calling voyageai.Client() inline wherever an
# embedding is needed) for one reason: the query/document asymmetry below
# is easy to get backwards, and a single pair of functions makes it
# impossible to accidentally call the wrong one at a call site.

import os

import voyageai

from rag.config import VOYAGE_EMBEDDING_MODEL

# Built once at import time, same lifetime/pattern as graph.py's
# anthropic_client - cheaper than constructing a new client per call, and
# this module is only ever imported after main.py has already loaded
# secrets (see main.py's import-order comment), so VOYAGE_API_KEY is
# guaranteed to be in os.environ by the time this line runs.
voyage_client = voyageai.Client(api_key=os.getenv("VOYAGE_API_KEY"))


def embed_documents(texts: list[str]) -> list[list[float]]:
    """
    Embeds a batch of texts meant to be STORED and searched against later
    (support notes, FAQ clauses). input_type="document" is not
    interchangeable with "query" below - Voyage's model generates slightly
    different vectors for each, an intentional asymmetry (same one taught
    in week3 of the course), not a bug. Used only by ingest.py.
    """
    # One network call for the whole batch, not one call per text - cheaper
    # and faster than embedding texts one at a time (same reasoning as
    # week3's embed_chunks()).
    result = voyage_client.embed(texts, model=VOYAGE_EMBEDDING_MODEL, input_type="document")
    return result.embeddings


def embed_query(text: str) -> list[float]:
    """
    Embeds a single user-facing search string - either the customer's raw
    message or a HyDE hypothetical passage (see hyde.py), both of which
    play the "query" role at search time. input_type="query" here, not
    "document" - see embed_documents()'s docstring for why that matters.
    """
    # Voyage's API always takes/returns a list, even for a single text -
    # wrap in a 1-element list, then unwrap the single result back out so
    # every OTHER file in rag/ can just call embed_query(text) -> vector
    # instead of remembering the [0] indexing themselves.
    result = voyage_client.embed([text], model=VOYAGE_EMBEDDING_MODEL, input_type="query")
    return result.embeddings[0]
