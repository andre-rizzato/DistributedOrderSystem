# src/AgentService/rag/bm25.py
#
# Hand-rolled BM25 - port of CursoClaude's week11/hybrid_search.py, English
# stopwords instead of Portuguese (this corpus/repo is in English). Kept as
# plain functions with no external dependency (not the `rank_bm25` library)
# on purpose: the algorithm is small enough to own directly, and avoiding a
# new dependency matters specifically because of the shared deploy VM's
# 892MB RAM ceiling (see rag/config.py's top comment).
#
# Why BM25 at all, next to semantic (vector) search: embeddings are good at
# "what is this about" but bad at exact, rare tokens - an order id like
# "20038" or an error code has no useful semantic neighbors for a model to
# generalize from. BM25 is literally "count matching words, weighted by how
# rare/important they are" - the opposite failure mode, which is exactly
# why combining the two (see hybrid.py) covers more ground than either
# alone.

import math
from collections import Counter

# Short, corpus-specific list (not a full English stopword list from a
# library) - same philosophy as the course version: only the words that
# actually show up often enough in THIS corpus to need filtering, not an
# exhaustive general-purpose list.
STOPWORDS_EN = {
    "a", "an", "the", "of", "to", "in", "on", "at", "for", "and", "or",
    "is", "was", "were", "be", "been", "being", "with", "by", "from",
    "it", "its", "this", "that", "these", "those", "as", "if", "but",
    "not", "no", "did", "didn't", "do", "does", "their", "they",
}


def tokenize(text: str) -> list[str]:
    """
    Lowercase + split on any non-alphanumeric character, dropping stopwords
    (see STOPWORDS_EN above). No stemming, no punctuation handling beyond
    splitting on it - good enough at this corpus's size and register.
    """
    word_chars: list[str] = []
    tokens: list[str] = []
    for c in text.lower():
        if c.isalnum():
            word_chars.append(c)
        elif word_chars:
            # Hit a non-alphanumeric character after collecting letters -
            # that's the end of one token, flush it and start the next.
            tokens.append("".join(word_chars))
            word_chars = []
    if word_chars:
        # The text didn't end on a separator (e.g. no trailing period) -
        # the last word would otherwise never get flushed.
        tokens.append("".join(word_chars))
    return [t for t in tokens if t not in STOPWORDS_EN]


def _bm25_idf(term: str, tokenized_docs: list[list[str]]) -> float:
    """
    Inverse document frequency: a term that appears in almost every
    document (e.g. "order") contributes almost nothing to the score; a
    term that appears in only one or two documents (e.g. a specific status
    name) contributes a lot. The +0.5/+1 constants prevent log(0) or
    log(negative) when a term appears in nearly every document - part of
    the original BM25 formula, not a tweak of ours.
    """
    n_docs = len(tokenized_docs)
    n_with_term = sum(1 for doc in tokenized_docs if term in doc)
    return math.log((n_docs - n_with_term + 0.5) / (n_with_term + 0.5) + 1)


def _bm25_score(
    query: str,
    doc_tokens: list[str],
    tokenized_docs: list[list[str]],
    k1: float = 1.5,
    b: float = 0.75,
) -> float:
    """
    One document's BM25 score against the query. k1 controls term-frequency
    saturation (the 5th occurrence of a word matters a lot less than the
    1st - not "more repeats = linearly better"); b controls length
    normalization (a long document shouldn't win just by being long and
    containing the term once more by chance). k1=1.5/b=0.75 are the
    standard textbook defaults, not something tuned for this corpus.
    """
    avg_doc_len = sum(len(d) for d in tokenized_docs) / len(tokenized_docs)
    term_freq = Counter(doc_tokens)
    score = 0.0
    for term in tokenize(query):
        f = term_freq.get(term, 0)
        if f == 0:
            # Query term never appears in this document - zero
            # contribution, skip the rest of the formula for this term.
            continue
        idf = _bm25_idf(term, tokenized_docs)
        numerator = f * (k1 + 1)
        denominator = f + k1 * (1 - b + b * len(doc_tokens) / avg_doc_len)
        score += idf * (numerator / denominator)
    return score


def bm25_rank(query: str, docs: list[str]) -> list[tuple[float, int]]:
    """
    Ranks `docs` against `query`, returning (score, original_index) pairs
    sorted highest-first. Returns the INDEX rather than the document text
    (the course version returned the text itself) - production candidate
    pools can contain duplicate or near-duplicate texts, so the index is
    the only unambiguous way for retrieval.py to map a BM25 rank back to
    the right Qdrant point/payload.
    """
    tokenized_docs = [tokenize(d) for d in docs]
    scores = [
        (_bm25_score(query, doc_tokens, tokenized_docs), i)
        for i, doc_tokens in enumerate(tokenized_docs)
    ]
    scores.sort(reverse=True, key=lambda pair: pair[0])
    return scores
