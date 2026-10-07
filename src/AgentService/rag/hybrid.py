# src/AgentService/rag/hybrid.py
#
# Reciprocal Rank Fusion - port of CursoClaude's week11/hybrid_search.py.
# Combines the BM25 ranking and the semantic (vector) ranking into one list
# WITHOUT summing their raw scores: cosine similarity lives in [-1, 1],
# BM25 has no upper bound at all, so adding them directly would let BM25
# dominate by accident, not because it's actually more relevant. RRF
# ignores the VALUE of each score and uses only each document's POSITION
# in each ranking.
#
# Known limitation, found and documented the hard way in the course
# (week11): if one ranking has a tie (e.g. every BM25 score is 0.0 because
# no query term appears anywhere), Python's stable sort still has to pick
# SOME order for the tied items - and RRF then treats that arbitrary
# position as if it were a real signal, with full weight. This is exactly
# why reranker.py exists as the next stage, not a redundant one: a
# cross-encoder judges each document against the query directly, with no
# ranking-combination step to contaminate.

from collections import defaultdict


def reciprocal_rank_fusion(
    rankings: list[list[tuple[float, int]]],
    k: int = 60,
) -> list[tuple[float, int]]:
    """
    Each ranking in `rankings` is a list of (score, index) pairs, already
    sorted best-first (the score VALUE is ignored here - only the position
    within the list matters). Returns (rrf_score, index) pairs, sorted
    best-first. k=60 is RRF's standard damping constant: it keeps the gap
    between 1st and 2nd place from being too extreme - without it, a
    document that's #1 in only ONE of the input rankings would dominate
    disproportionately over one that's well-placed in BOTH.
    """
    rrf_scores: dict[int, float] = defaultdict(float)
    for ranking in rankings:
        for position, (_, index) in enumerate(ranking, start=1):
            # 1/(k+position): the further down the list, the smaller the
            # contribution - but never zero, so a document ranked low in
            # every list still accumulates a (tiny) combined score.
            rrf_scores[index] += 1 / (k + position)

    combined = sorted(rrf_scores.items(), key=lambda pair: pair[1], reverse=True)
    return [(score, index) for index, score in combined]
