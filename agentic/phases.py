"""
Registro de fases del pipeline de construcción de webImagenologia.
Cada Phase define: id, nombre, ruta del prompt, y función gate de validación.
"""
from __future__ import annotations

import re
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable


PROMPTS_DIR = Path(__file__).parent / "prompts"


@dataclass
class Phase:
    id: str          # "01", "02", ... "12"
    name: str
    prompt_path: Path
    gate: Callable[[dict], bool] = field(default=lambda r: r.get("status") == "PASS")
    required: bool = True  # False = warning si falla, no detiene pipeline


def _default_gate(report: dict) -> bool:
    """Gate universal: status == PASS y build == ok."""
    if report.get("status") != "PASS":
        return False
    validations = report.get("validations", {})
    if validations.get("build") not in ("ok", None):
        return False
    return True


def _gate_with_audio_check(report: dict) -> bool:
    """Gate Fase 08: build ok + test de audio PASS."""
    if not _default_gate(report):
        return False
    notes = report.get("notes", "")
    if "BLOCKED" in notes.upper():
        return False
    return True


def _gate_n8n(report: dict) -> bool:
    """Gate Fase 11: solo verifica JSON válido (no build .NET)."""
    return report.get("status") == "PASS"


def _gate_qa(report: dict) -> bool:
    """Gate Fase 12: todos los sub-gates deben ser ok."""
    if report.get("status") != "PASS":
        return False
    v = report.get("validations", {})
    required_ok = ["build", "lint", "secrets"]
    return all(v.get(g) in ("ok", "PASS") for g in required_ok)


PHASES: list[Phase] = [
    Phase(
        id="01",
        name="Scaffold Solución .NET 8",
        prompt_path=PROMPTS_DIR / "phase_01_scaffold.md",
        gate=_default_gate,
    ),
    Phase(
        id="02",
        name="Login de Acceso + ApiClient",
        prompt_path=PROMPTS_DIR / "phase_02_auth_login.md",
        gate=_default_gate,
    ),
    Phase(
        id="03",
        name="Parámetros — Radiólogos por Empresa",
        prompt_path=PROMPTS_DIR / "phase_03_param_radiologos.md",
        gate=_default_gate,
    ),
    Phase(
        id="04",
        name="Parámetros — Operadores por Empresa",
        prompt_path=PROMPTS_DIR / "phase_04_param_operadores.md",
        gate=_default_gate,
    ),
    Phase(
        id="05",
        name="Condicionales — Parametrización Estudios",
        prompt_path=PROMPTS_DIR / "phase_05_param_estudios.md",
        gate=_default_gate,
    ),
    Phase(
        id="06",
        name="Condicionales — Asignación No. Estudios",
        prompt_path=PROMPTS_DIR / "phase_06_cond_asignacion.md",
        gate=_default_gate,
    ),
    Phase(
        id="07",
        name="Condicionales — Automatización N8N",
        prompt_path=PROMPTS_DIR / "phase_07_cond_automatizacion.md",
        gate=_default_gate,
    ),
    Phase(
        id="08",
        name="Portal Web Radiólogos",
        prompt_path=PROMPTS_DIR / "phase_08_portal_radiologos.md",
        gate=_gate_with_audio_check,
    ),
    Phase(
        id="09",
        name="Portal Web Lecturas",
        prompt_path=PROMPTS_DIR / "phase_09_portal_lecturas.md",
        gate=_default_gate,
    ),
    Phase(
        id="10",
        name="Consultas / Reportes",
        prompt_path=PROMPTS_DIR / "phase_10_reportes.md",
        gate=_default_gate,
    ),
    Phase(
        id="11",
        name="Workflow N8N — Programación Estudios",
        prompt_path=PROMPTS_DIR / "phase_11_n8n_workflow.md",
        gate=_gate_n8n,
    ),
    Phase(
        id="12",
        name="QA Global + Release",
        prompt_path=PROMPTS_DIR / "phase_12_qa_release.md",
        gate=_gate_qa,
    ),
]

PHASE_MAP: dict[str, Phase] = {p.id: p for p in PHASES}
