"""
Notifier — comunicación del orquestador al Lead del proyecto.

Canales:
1. Consola enriquecida (rich)
2. Archivo Markdown en agentic/reports/
3. Webhook HTTP opcional (LEAD_WEBHOOK_URL en .env)
"""
from __future__ import annotations

import json
import os
import datetime as dt
from pathlib import Path
from typing import Literal

import httpx
from rich.console import Console
from rich.panel import Panel
from rich.table import Table
from rich import box

REPORTS_DIR = Path(__file__).parent / "reports"
REPORTS_DIR.mkdir(exist_ok=True)

console = Console(force_terminal=True)

Level = Literal["info", "start", "success", "warn", "error", "needs_input"]

LEVEL_COLORS: dict[Level, str] = {
    "info": "cyan",
    "start": "blue bold",
    "success": "green bold",
    "warn": "yellow",
    "error": "red bold",
    "needs_input": "magenta bold",
}

LEVEL_ICONS: dict[Level, str] = {
    "info": "i",
    "start": ">",
    "success": "+",
    "warn": "!",
    "error": "x",
    "needs_input": "?",
}


def notify_lead(
    message: str,
    *,
    level: Level = "info",
    phase: str | None = None,
    report: dict | None = None,
) -> None:
    """
    Notifica al Lead del proyecto por consola, archivo y webhook.

    Args:
        message: Texto principal del mensaje.
        level: Nivel de severidad.
        phase: ID de la fase actual (ej. "01").
        report: Diccionario con el reporte YAML parseado.
    """
    timestamp = dt.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    color = LEVEL_COLORS[level]
    icon = LEVEL_ICONS[level]
    phase_label = f"[FASE {phase}] " if phase else ""

    # ── 1. Consola enriquecida ──────────────────────────────────────────────
    header = f"[{color}]{icon} {phase_label}{message}[/{color}]"
    console.print(f"\n[dim]{timestamp}[/dim]  {header}")

    if report:
        _print_report_table(report, phase or "?")

    # ── 2. Archivo Markdown ─────────────────────────────────────────────────
    _save_report_file(
        timestamp=timestamp,
        level=level,
        phase=phase,
        message=message,
        report=report,
    )

    # ── 3. Webhook opcional ─────────────────────────────────────────────────
    webhook_url = os.getenv("LEAD_WEBHOOK_URL", "")
    if webhook_url:
        _send_webhook(
            url=webhook_url,
            payload={
                "timestamp": timestamp,
                "level": level,
                "phase": phase,
                "message": message,
                "report": report,
            },
        )


def _print_report_table(report: dict, phase: str) -> None:
    """Imprime el reporte YAML como tabla rich en consola."""
    table = Table(
        title=f"Reporte Fase {phase}",
        box=box.ROUNDED,
        show_header=True,
        header_style="bold",
    )
    table.add_column("Campo", style="dim", width=20)
    table.add_column("Valor")

    table.add_row("status", _colored_status(report.get("status", "?")))
    table.add_row("build", report.get("validations", {}).get("build", "—"))
    table.add_row("tests", report.get("validations", {}).get("tests", "—"))
    table.add_row("lint", report.get("validations", {}).get("lint", "—"))
    table.add_row("secrets", report.get("validations", {}).get("secrets", "—"))
    table.add_row("next_phase", report.get("next_phase", "—"))

    artifacts = report.get("artifacts", [])
    table.add_row("artifacts", f"{len(artifacts)} archivo(s)")

    blockers = report.get("blockers", [])
    if blockers:
        table.add_row("[red]blockers[/red]", "\n".join(blockers))

    console.print(table)

    notes = report.get("notes", "").strip()
    if notes:
        console.print(Panel(notes, title="Notes", border_style="dim"))


def _colored_status(status: str) -> str:
    colors = {"PASS": "green", "FAIL": "red", "BLOCKED": "magenta"}
    color = colors.get(status, "white")
    return f"[{color}]{status}[/{color}]"


def _save_report_file(
    *,
    timestamp: str,
    level: Level,
    phase: str | None,
    message: str,
    report: dict | None,
) -> None:
    """Guarda el reporte en un archivo Markdown en agentic/reports/."""
    safe_ts = timestamp.replace(":", "-").replace(" ", "_")
    phase_tag = f"phase_{phase}_" if phase else ""
    filename = REPORTS_DIR / f"{phase_tag}{level}_{safe_ts}.md"

    lines = [
        f"# Reporte — {message}",
        f"",
        f"**Timestamp**: {timestamp}  ",
        f"**Nivel**: {level}  ",
        f"**Fase**: {phase or 'N/A'}  ",
        "",
    ]

    if report:
        lines += [
            "## Detalles",
            "```yaml",
            json.dumps(report, ensure_ascii=False, indent=2),
            "```",
        ]

    filename.write_text("\n".join(lines), encoding="utf-8")


def _send_webhook(url: str, payload: dict) -> None:
    """Envía el reporte al webhook configurado en LEAD_WEBHOOK_URL."""
    try:
        with httpx.Client(timeout=10) as client:
            resp = client.post(url, json=payload)
            if resp.status_code >= 400:
                console.print(
                    f"[yellow]! Webhook retornó {resp.status_code}[/yellow]"
                )
    except Exception as exc:
        console.print(f"[yellow]! Webhook fallido: {exc}[/yellow]")


def ask_lead_confirmation(prompt: str) -> bool:
    """
    Detiene el pipeline y solicita confirmación interactiva al Lead.
    Retorna True si el Lead confirma, False si rechaza.
    """
    notify_lead(prompt, level="needs_input")
    console.print("\n[magenta bold]¿Continuar? (s/n): [/magenta bold]", end="")
    try:
        answer = input().strip().lower()
        return answer in ("s", "si", "sí", "y", "yes")
    except (EOFError, KeyboardInterrupt):
        return False
