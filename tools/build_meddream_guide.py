# -*- coding: utf-8 -*-
"""Genera docs/visor/Guia-Instalacion-Visor.pdf desde Guia-Instalacion-Visor.md"""
import os
import re
from reportlab.lib.pagesizes import letter
from reportlab.lib.units import mm
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Preformatted, Table, TableStyle, PageBreak
)
from reportlab.lib.enums import TA_LEFT

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
MD = os.path.join(REPO, "docs", "visor", "Guia-Instalacion-Visor.md")
OUT = os.path.join(REPO, "docs", "visor", "Guia-Instalacion-Visor.pdf")
OUT_ALIAS = os.path.join(REPO, "docs", "Guia_Visor_DICOM_PortalImagenologia.pdf")

AZUL = colors.HexColor("#1a3a5c")
AZUL2 = colors.HexColor("#2a6099")
NARANJA = colors.HexColor("#d9781f")
GRIS_BORDE = colors.HexColor("#cfcfcf")

ss = getSampleStyleSheet()
H1 = ParagraphStyle("H1", parent=ss["Heading1"], fontName="Helvetica-Bold",
                    fontSize=16, textColor=AZUL, spaceBefore=8, spaceAfter=8, leading=20)
H2 = ParagraphStyle("H2", parent=ss["Heading2"], fontName="Helvetica-Bold",
                    fontSize=12.5, textColor=AZUL2, spaceBefore=10, spaceAfter=5, leading=16)
H3 = ParagraphStyle("H3", parent=ss["Heading3"], fontName="Helvetica-Bold",
                    fontSize=11, textColor=NARANJA, spaceBefore=7, spaceAfter=3, leading=14)
P = ParagraphStyle("P", parent=ss["BodyText"], fontName="Helvetica",
                   fontSize=9.5, leading=13.5, spaceAfter=5, alignment=TA_LEFT)
LI = ParagraphStyle("LI", parent=P, leftIndent=12, spaceAfter=2)
CODE = ParagraphStyle("CODE", fontName="Courier", fontSize=8, leading=10.5)
QUOTE = ParagraphStyle("QUOTE", parent=P, textColor=colors.HexColor("#444"),
                       leftIndent=8, borderPadding=4)


def esc(text: str) -> str:
    return (text.replace("&", "&amp;")
                .replace("<", "&lt;")
                .replace(">", "&gt;"))


def md_inline(text: str) -> str:
    text = esc(text)
    text = re.sub(r"`([^`]+)`", r"<font face='Courier' size='8'>\1</font>", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"\*([^*]+)\*", r"<i>\1</i>", text)
    return text


def build():
    with open(MD, "r", encoding="utf-8") as f:
        lines = f.read().splitlines()

    story = []
    in_code = False
    code_buf = []
    in_table = False
    table_rows = []

    def flush_code():
        nonlocal code_buf
        if not code_buf:
            return
        pre = Preformatted("\n".join(code_buf), CODE)
        t = Table([[pre]], colWidths=[170 * mm])
        t.setStyle(TableStyle([
            ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#f6f8fa")),
            ("BOX", (0, 0), (-1, -1), 0.5, GRIS_BORDE),
            ("LEFTPADDING", (0, 0), (-1, -1), 6),
            ("RIGHTPADDING", (0, 0), (-1, -1), 6),
            ("TOPPADDING", (0, 0), (-1, -1), 4),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ]))
        story.append(t)
        story.append(Spacer(1, 6))
        code_buf = []

    def flush_table():
        nonlocal table_rows, in_table
        if not table_rows:
            in_table = False
            return
        data = []
        for row in table_rows:
            cells = [c.strip() for c in row.strip("|").split("|")]
            data.append([Paragraph(md_inline(c), P) for c in cells])
        if len(data) >= 2 and all(set(c.text.replace("<", "").replace(">", "")) <= set("-: ") for c in data[1]):
            # skip markdown separator row visually by dropping it
            data = [data[0]] + data[2:]
        col_w = 170 * mm / max(len(data[0]), 1)
        t = Table(data, colWidths=[col_w] * len(data[0]))
        t.setStyle(TableStyle([
            ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#e8eef5")),
            ("GRID", (0, 0), (-1, -1), 0.4, GRIS_BORDE),
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ("LEFTPADDING", (0, 0), (-1, -1), 4),
            ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ]))
        story.append(t)
        story.append(Spacer(1, 8))
        table_rows = []
        in_table = False

    for raw in lines:
        line = raw.rstrip()
        if line.startswith("```"):
            if in_code:
                flush_code()
                in_code = False
            else:
                if in_table:
                    flush_table()
                in_code = True
            continue
        if in_code:
            code_buf.append(line)
            continue
        if line.startswith("|") and "|" in line[1:]:
            in_table = True
            table_rows.append(line)
            continue
        if in_table:
            flush_table()

        if not line.strip():
            story.append(Spacer(1, 4))
            continue
        if line.startswith("# "):
            story.append(Paragraph(md_inline(line[2:]), H1))
        elif line.startswith("## "):
            story.append(Paragraph(md_inline(line[3:]), H2))
        elif line.startswith("### "):
            story.append(Paragraph(md_inline(line[4:]), H3))
        elif line.startswith("> "):
            story.append(Paragraph(md_inline(line[2:]), QUOTE))
        elif line.startswith("- ") or line.startswith("* "):
            story.append(Paragraph("• " + md_inline(line[2:]), LI))
        elif re.match(r"^\d+\.\s", line):
            story.append(Paragraph(md_inline(line), LI))
        elif line.strip() == "---":
            story.append(Spacer(1, 8))
        else:
            story.append(Paragraph(md_inline(line), P))

    if in_code:
        flush_code()
    if in_table:
        flush_table()

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    doc = SimpleDocTemplate(
        OUT,
        pagesize=letter,
        leftMargin=16 * mm,
        rightMargin=16 * mm,
        topMargin=14 * mm,
        bottomMargin=14 * mm,
        title="Guia Instalacion Visor MedDream - Portal Imagenologia",
    )
    doc.build(story)

    # Alias histórico pedido por tools/build_visor_guide.py
    try:
        import shutil
        shutil.copyfile(OUT, OUT_ALIAS)
    except OSError:
        pass
    print("OK:", OUT)


if __name__ == "__main__":
    build()
