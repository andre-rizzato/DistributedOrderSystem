# src/AgentService/rag/hyde.py
#
# HyDE (Hypothetical Document Embeddings) - port of CursoClaude's
# week13/hyde.py. Instead of embedding the customer's raw message, ask the
# LLM to write a HYPOTHETICAL passage in the same register as the real
# corpus (formal support-note/policy language), then embed THAT instead.
#
# Why this helps: a bi-encoder (the Voyage embedding model) compares two
# vectors that were never computed together - a casual question ("my
# payment didn't go through, what now?") and a formal support note
# ("Order 20004 pending because the installment plan was rejected...")
# can be about the exact same thing and still land far apart in embedding
# space, because they're different SPEECH ACTS (a question vs. a
# statement), not just different vocabulary. The hypothetical passage is
# declarative, not interrogative - same register as the real corpus - so
# its embedding lands closer to the real matching note, even though the
# hypothetical passage itself may get facts wrong (that's fine: it's
# discarded immediately after embedding, see retrieval.py - it is NEVER
# shown to the customer and NEVER used as a source of truth).

from anthropic import Anthropic

# Built once at import time, same pattern as graph.py's anthropic_client -
# this is a SEPARATE client instance (not importing graph.py's) to keep
# rag/ free of any dependency on graph.py, avoiding a circular import
# (graph.py will import rag_node.py, which imports this file).
_anthropic_client = Anthropic()

# Haiku, not Sonnet (the model graph.py's generate_reply_node uses) - the
# hypothetical passage only needs to be in the right REGISTER, not be
# deeply reasoned about; a cheaper/faster model is the right tradeoff here,
# same reasoning the course's implementation plan called out explicitly.
_HYDE_MODEL = "claude-haiku-4-5-20251001"

_SYSTEM_PROMPT = (
    "You write short, formal customer-support note or policy excerpts for "
    "an e-commerce order system, in the style of an internal operations "
    "log (terms like 'order', 'status', 'eligibility', 'processed within'). "
    "Don't worry about whether the specific facts are correct - what "
    "matters is the formal REGISTER, as if this were a real excerpt from "
    "the support notes or policy manual. Reply with only the passage, no "
    "introduction."
)


def generate_hypothetical_passage(query: str) -> str:
    """
    Generates one hypothetical passage for the given customer query. Returns
    plain text - callers are responsible for embedding it (rag/embeddings.py)
    and for never surfacing it to the end user or treating it as a fact.
    """
    response = _anthropic_client.messages.create(
        model=_HYDE_MODEL,
        max_tokens=200,
        system=_SYSTEM_PROMPT,
        messages=[{"role": "user", "content": query}],
    )
    return response.content[0].text
