# src/AgentService/tests/test_scheduling_slots.py
# Covers compute_available_slots() (connectors/scheduling_google_calendar.py)
# in isolation - no Google credential, no network, same "pure function"
# testability graph.py's route_by_confidence() already has a comment about.
#
# Run from src/AgentService: pytest

from datetime import datetime, timedelta

from connectors.scheduling_google_calendar import compute_available_slots

DAY_START = datetime(2026, 10, 6, 9, 0)  # 9am
DAY_END = datetime(2026, 10, 6, 18, 0)  # 6pm
HALF_HOUR = timedelta(minutes=30)


def test_empty_calendar_returns_every_slot():
    slots = compute_available_slots(DAY_START, DAY_END, [], HALF_HOUR, HALF_HOUR)
    assert len(slots) == 18  # 9 hours / 30 min slots, back-to-back
    assert slots[0]["start"] == DAY_START.isoformat()
    assert slots[-1]["end"] == DAY_END.isoformat()


def test_busy_block_removes_overlapping_slots_only():
    busy = [(datetime(2026, 10, 6, 10, 0), datetime(2026, 10, 6, 11, 0))]
    slots = compute_available_slots(DAY_START, DAY_END, busy, HALF_HOUR, HALF_HOUR)
    starts = {s["start"] for s in slots}
    assert datetime(2026, 10, 6, 10, 0).isoformat() not in starts
    assert datetime(2026, 10, 6, 10, 30).isoformat() not in starts
    assert datetime(2026, 10, 6, 9, 30).isoformat() in starts  # right before, untouched
    assert datetime(2026, 10, 6, 11, 0).isoformat() in starts  # right after, untouched


def test_slot_longer_than_gap_between_busy_blocks_is_excluded():
    # Two 15-minute gaps either side of a lunch block - neither fits a
    # 30-minute service, even though neither gap itself is "busy".
    busy = [
        (datetime(2026, 10, 6, 12, 15), datetime(2026, 10, 6, 13, 0)),
    ]
    slots = compute_available_slots(
        datetime(2026, 10, 6, 12, 0), datetime(2026, 10, 6, 13, 15), busy, HALF_HOUR, timedelta(minutes=15)
    )
    starts = {s["start"] for s in slots}
    assert datetime(2026, 10, 6, 12, 0).isoformat() not in starts  # would run into the busy block
    assert datetime(2026, 10, 6, 13, 0).isoformat() not in starts  # only 15 min left in the window


def test_fully_booked_day_returns_no_slots():
    busy = [(DAY_START, DAY_END)]
    assert compute_available_slots(DAY_START, DAY_END, busy, HALF_HOUR, HALF_HOUR) == []
