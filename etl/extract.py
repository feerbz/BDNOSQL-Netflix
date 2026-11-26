"""Lectura de los CSV del catálogo."""
from pathlib import Path

import pandas as pd


def extract(data_dir):
    """Devuelve (titles, credits) con todo como texto; la limpieza va en transform."""
    paths = [Path(data_dir) / name for name in ("titles.csv", "credits.csv")]
    missing = [str(p) for p in paths if not p.is_file()]
    if missing:
        raise FileNotFoundError(f"Falta el archivo: {', '.join(missing)}")
    # keep_default_na=False: el código de país "NA" (Namibia) no es un nulo.
    return tuple(pd.read_csv(p, dtype=str, keep_default_na=False) for p in paths)
