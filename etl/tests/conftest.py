import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "etl"))

from extract import extract  # noqa: E402
from transform import transform  # noqa: E402


@pytest.fixture(scope="session")
def raw():
    return extract(ROOT / "archive")


@pytest.fixture(scope="session")
def titles(raw):
    return transform(*raw)[0]
