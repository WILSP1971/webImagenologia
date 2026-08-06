"""
orchestrator.py — Orquestador Agentico de webImagenologia
Plataforma Web Imagenología Esculapio

Uso:
    python orchestrator.py                  # ejecuta todas las fases
    python orchestrator.py --phase 01       # ejecuta solo la fase 01
    python orchestrator.py --from-phase 03  # ejecuta desde la fase 03 en adelante
    python orchestrator.py --list           # lista las fases disponibles

Requiere:
    CURSOR_API_KEY en .env (o variable de entorno)
    Python 3.11+, cursor-sdk, rich, pyyaml, python-dotenv
"""
from __future__ import annotations

import argparse
import os
import re
import sys
import datetime as dt
from pathlib import Path

import yaml
from dotenv import load_dotenv
from rich.console import Console

# Cargar .env desde el directorio del orquestador
load_dotenv(Path(__file__).parent / ".env")

# Consola UTF-8 en Windows (evita UnicodeEncodeError con rich)
if sys.platform == "win32":
    for _stream in (sys.stdout, sys.stderr):
        if hasattr(_stream, "reconfigure"):
            try:
                _stream.reconfigure(encoding="utf-8", errors="replace")
            except Exception:
                pass

from bridge_compat import apply_bridge_patch

apply_bridge_patch()

from cursor_sdk import Agent, AgentOptions, LocalAgentOptions, CursorAgentError
from notifier import notify_lead, ask_lead_confirmation
from phases import PHASES, PHASE_MAP, Phase

console = Console()

# Directorio raíz del repositorio (un nivel arriba de agentic/)
REPO_ROOT = Path(__file__).resolve().parents[1]

# Prompt maestro CO-STAR
META_PROMPT_PATH = Path(__file__).parent / "prompts" / "meta_prompt_costar.md"
SPECS_PATH = REPO_ROOT / "docs" / "specs.md"
VALIDATION_RULES_PATH = REPO_ROOT / "docs" / "validation-rules.md"


# ── Utilidades ──────────────────────────────────────────────────────────────


def load_meta_prompt() -> str:
    """Carga el meta-prompt CO-STAR y anexa specs y reglas de validación."""
    meta = META_PROMPT_PATH.read_text(encoding="utf-8")
    if SPECS_PATH.exists():
        meta += f"\n\n---\n\n# ESPECIFICACIÓN FUNCIONAL (docs/specs.md)\n\n"
        meta += SPECS_PATH.read_text(encoding="utf-8")
    if VALIDATION_RULES_PATH.exists():
        meta += f"\n\n---\n\n# REGLAS DE VALIDACIÓN (docs/validation-rules.md)\n\n"
        meta += VALIDATION_RULES_PATH.read_text(encoding="utf-8")
    return meta


def build_phase_prompt(meta: str, phase: Phase) -> str:
    """Combina el meta-prompt con el prompt específico de la fase."""
    phase_content = phase.prompt_path.read_text(encoding="utf-8")
    return (
        f"{meta}\n\n"
        f"---\n\n"
        f"# TAREA ACTUAL — FASE {phase.id}: {phase.name}\n\n"
        f"{phase_content}\n\n"
        f"---\n\n"
        f"**IMPORTANTE**: Al finalizar, emite el bloque YAML de reporte definido en "
        f"el § [R] del meta-prompt. Ese bloque debe ser el ÚLTIMO elemento de tu respuesta."
    )


def parse_yaml_report(text: str) -> dict:
    """
    Extrae el bloque YAML del resultado del agente.
    Busca el último bloque delimitado por ```yaml ... ``` o --- ... ---
    """
    # Buscar bloque ```yaml ... ```
    pattern = r"```yaml\s*\n(.*?)```"
    matches = re.findall(pattern, text, re.DOTALL)
    if matches:
        try:
            return yaml.safe_load(matches[-1]) or {}
        except yaml.YAMLError:
            pass

    # Fallback: buscar bloque YAML al final del texto
    lines = text.strip().split("\n")
    yaml_lines = []
    in_yaml = False
    for line in reversed(lines):
        if line.strip().startswith("next_phase:") or line.strip().startswith("phase:"):
            in_yaml = True
        if in_yaml:
            yaml_lines.insert(0, line)
        if in_yaml and line.strip() == "":
            break

    if yaml_lines:
        try:
            return yaml.safe_load("\n".join(yaml_lines)) or {}
        except yaml.YAMLError:
            pass

    # No se pudo parsear — retornar reporte de error
    return {
        "phase": "??",
        "status": "FAIL",
        "blockers": ["No se encontró el bloque YAML de reporte en la respuesta del agente."],
        "notes": text[-500:] if len(text) > 500 else text,
    }


def save_report(phase_id: str, report: dict) -> None:
    """Persiste el reporte YAML en agentic/reports/."""
    reports_dir = Path(__file__).parent / "reports"
    reports_dir.mkdir(exist_ok=True)
    ts = dt.datetime.now().strftime("%Y%m%d_%H%M%S")
    out = reports_dir / f"phase_{phase_id}_{ts}.yaml"
    out.write_text(yaml.dump(report, allow_unicode=True), encoding="utf-8")
    console.print(f"[dim]  Reporte guardado: {out.relative_to(REPO_ROOT)}[/dim]")


# ── Ejecución de fases ───────────────────────────────────────────────────────


def run_phase(phase: Phase, meta_prompt: str, api_key: str) -> bool:
    """
    Ejecuta una fase del pipeline usando el Cursor SDK.

    Patrón:
      - Agent.create (local, cwd=REPO_ROOT)
      - agent.send(prompt completo)
      - stream de mensajes → opcional log
      - run.wait() → parsear YAML → evaluar gate → notificar Lead

    Retorna True si la fase PASÓ el gate, False en caso contrario.
    """
    prompt = build_phase_prompt(meta_prompt, phase)

    notify_lead(f"Iniciando fase {phase.id}: {phase.name}", level="start", phase=phase.id)

    try:
        with Agent.create(
            AgentOptions(
                api_key=api_key,
                model="composer-2.5",
                local=LocalAgentOptions(cwd=str(REPO_ROOT)),
            )
        ) as agent:
            run = agent.send(prompt)

            # Stream opcional — muestra texto del agente en consola en tiempo real
            for msg in run.messages():
                if msg.type == "assistant":
                    for block in msg.message.content:
                        if block.type == "text" and os.getenv("STREAM_OUTPUT", "0") == "1":
                            console.print(block.text, end="")

            result = run.wait()

            if result.status == "error":
                notify_lead(
                    f"Fase {phase.id} falló durante la ejecución del agente.",
                    level="error",
                    phase=phase.id,
                    report={"status": "FAIL", "notes": result.result or "Sin detalles."},
                )
                return False

            report = parse_yaml_report(result.result or "")
            save_report(phase.id, report)

            # Evaluar gate
            passed = phase.gate(report)

            if passed:
                notify_lead(
                    f"Fase {phase.id} completada exitosamente.",
                    level="success",
                    phase=phase.id,
                    report=report,
                )
                return True
            else:
                status = report.get("status", "?")
                if status == "BLOCKED":
                    notify_lead(
                        f"Fase {phase.id} BLOQUEADA — se requiere intervención del Lead.",
                        level="needs_input",
                        phase=phase.id,
                        report=report,
                    )
                    confirmed = ask_lead_confirmation(
                        "¿Desea continuar con la siguiente fase a pesar del bloqueo?"
                    )
                    return confirmed
                else:
                    notify_lead(
                        f"Fase {phase.id} FALLÓ los gates de validación.",
                        level="error",
                        phase=phase.id,
                        report=report,
                    )
                    return False

    except CursorAgentError as exc:
        hint = ""
        if exc.status == 401 or exc.code == "unauthenticated":
            hint = (
                " Verifica CURSOR_API_KEY en agentic/.env "
                "(https://cursor.com/dashboard/integrations)."
            )
        notify_lead(
            f"Error de inicio del agente en fase {phase.id}: {exc!s}{hint} "
            f"(retryable={exc.is_retryable})",
            level="error",
            phase=phase.id,
        )
        sys.exit(1)
    except OSError as exc:
        notify_lead(
            f"Error del bridge local en fase {phase.id}: {exc}. "
            "En Windows, asegúrate de tener cursor-sdk>=0.1.5 y Cursor CLI instalado.",
            level="error",
            phase=phase.id,
        )
        sys.exit(1)


# ── CLI ──────────────────────────────────────────────────────────────────────


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Orquestador agentico — webImagenologia Esculapio"
    )
    group = parser.add_mutually_exclusive_group()
    group.add_argument("--phase", metavar="ID", help="Ejecutar solo la fase indicada (ej. 01)")
    group.add_argument("--from-phase", metavar="ID", help="Ejecutar desde esta fase en adelante")
    group.add_argument("--list", action="store_true", help="Listar fases disponibles")
    parser.add_argument(
        "--stream",
        action="store_true",
        help="Mostrar output del agente en tiempo real",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    if args.stream:
        os.environ["STREAM_OUTPUT"] = "1"

    if args.list:
        console.print("\n[bold]Fases disponibles:[/bold]")
        for p in PHASES:
            console.print(f"  [cyan]{p.id}[/cyan]  {p.name}")
        console.print()
        return

    # Validar API key
    api_key = os.getenv("CURSOR_API_KEY", "")
    if not api_key:
        console.print(
            "[red bold]ERROR[/red bold]: CURSOR_API_KEY no encontrada. "
            "Configura .env o la variable de entorno."
        )
        sys.exit(1)

    # Seleccionar fases a ejecutar
    if args.phase:
        if args.phase not in PHASE_MAP:
            console.print(f"[red]Fase '{args.phase}' no encontrada.[/red]")
            sys.exit(1)
        phases_to_run = [PHASE_MAP[args.phase]]
    elif args.from_phase:
        if args.from_phase not in PHASE_MAP:
            console.print(f"[red]Fase '{args.from_phase}' no encontrada.[/red]")
            sys.exit(1)
        start_idx = next(i for i, p in enumerate(PHASES) if p.id == args.from_phase)
        phases_to_run = PHASES[start_idx:]
    else:
        phases_to_run = PHASES

    # Cargar meta-prompt una sola vez
    meta_prompt = load_meta_prompt()

    # Banner de inicio
    notify_lead(
        f"Pipeline iniciado — {len(phases_to_run)} fase(s) a ejecutar.",
        level="info",
    )

    # Ejecutar fases secuencialmente
    for phase in phases_to_run:
        ok = run_phase(phase, meta_prompt, api_key)
        if not ok:
            notify_lead(
                f"Pipeline detenido en fase {phase.id}. Corrige los problemas y reanuda "
                f"con: python orchestrator.py --from-phase {phase.id}",
                level="error",
            )
            sys.exit(2)

    notify_lead("Pipeline completado exitosamente. Todas las fases en PASS.", level="success")


if __name__ == "__main__":
    main()
