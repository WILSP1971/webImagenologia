# -*- coding: utf-8 -*-
"""
Generador de la Guia PDF del Visor DICOM (Portal Web Radiologos / PortalImagenologia).
Recopila TODO lo trabajado en la conversacion: hallazgos del PACS, arquitectura,
instalacion/config de Orthanc en Windows Server 2012 R2, IIS/ARR, OHIF, integracion
.NET 8, seguridad y bitacora de operacion del enjambre.

Uso:   python tools/build_visor_guide.py
Salida: docs/Guia_Visor_DICOM_PortalImagenologia.pdf
Requiere: pip install reportlab pypdf
"""
import os
from reportlab.lib.pagesizes import letter
from reportlab.lib.units import mm
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
                                Preformatted, PageBreak, KeepTogether)
from reportlab.lib.enums import TA_CENTER, TA_LEFT

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
OUT = os.path.join(REPO, "docs", "Guia_Visor_DICOM_PortalImagenologia.pdf")

# ---------------- paleta ----------------
AZUL = colors.HexColor("#1a3a5c")
AZUL2 = colors.HexColor("#2a6099")
NARANJA = colors.HexColor("#d9781f")
GRIS = colors.HexColor("#f2f2f2")
GRIS_BORDE = colors.HexColor("#cfcfcf")
VERDE = colors.HexColor("#e5f2e5")
VERDE_BORDE = colors.HexColor("#2e7d32")
AMBAR = colors.HexColor("#fff4e0")
AMBAR_BORDE = colors.HexColor("#d9781f")
ROJO = colors.HexColor("#fdeaea")
ROJO_BORDE = colors.HexColor("#c62828")

ss = getSampleStyleSheet()
H1 = ParagraphStyle("H1", parent=ss["Heading1"], fontName="Helvetica-Bold",
                    fontSize=18, textColor=AZUL, spaceBefore=6, spaceAfter=10, leading=22)
H2 = ParagraphStyle("H2", parent=ss["Heading2"], fontName="Helvetica-Bold",
                    fontSize=13.5, textColor=AZUL2, spaceBefore=12, spaceAfter=6, leading=17)
H3 = ParagraphStyle("H3", parent=ss["Heading3"], fontName="Helvetica-Bold",
                    fontSize=11.5, textColor=NARANJA, spaceBefore=8, spaceAfter=4, leading=15)
P = ParagraphStyle("P", parent=ss["BodyText"], fontName="Helvetica",
                   fontSize=10, leading=14.5, spaceAfter=6, alignment=TA_LEFT)
PB = ParagraphStyle("PB", parent=P, fontName="Helvetica-Bold")
LI = ParagraphStyle("LI", parent=P, leftIndent=14, spaceAfter=3, bulletIndent=4)
CODE = ParagraphStyle("CODE", fontName="Courier", fontSize=8.3, leading=11, textColor=colors.HexColor("#1b1b1b"))
TOCI = ParagraphStyle("TOCI", parent=P, fontSize=10.5, leading=17, spaceAfter=0)

story = []

def h1(t): story.append(Paragraph(t, H1))
def h2(t): story.append(Paragraph(t, H2))
def h3(t): story.append(Paragraph(t, H3))
def p(t): story.append(Paragraph(t, P))
def sp(h=6): story.append(Spacer(1, h))

def bullets(items):
    for it in items:
        story.append(Paragraph(("• " + it), LI))
    sp(4)

def code(txt):
    txt = txt.strip("\n")
    pre = Preformatted(txt, CODE)
    t = Table([[pre]], colWidths=[165*mm])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0,0), (-1,-1), colors.HexColor("#f6f8fa")),
        ("BOX", (0,0), (-1,-1), 0.6, GRIS_BORDE),
        ("LEFTPADDING",(0,0),(-1,-1),8),("RIGHTPADDING",(0,0),(-1,-1),8),
        ("TOPPADDING",(0,0),(-1,-1),6),("BOTTOMPADDING",(0,0),(-1,-1),6),
    ]))
    story.append(t); sp(6)

def box(title, txt, bg, border):
    inner = []
    if title:
        inner.append(Paragraph(f'<b>{title}</b>', ParagraphStyle("bt", parent=P, spaceAfter=3)))
    inner.append(Paragraph(txt, ParagraphStyle("bx", parent=P, spaceAfter=0)))
    t = Table([[inner]], colWidths=[165*mm])
    t.setStyle(TableStyle([
        ("BACKGROUND",(0,0),(-1,-1),bg),
        ("LINEBEFORE",(0,0),(-1,-1),3,border),
        ("BOX",(0,0),(-1,-1),0.4,border),
        ("LEFTPADDING",(0,0),(-1,-1),10),("RIGHTPADDING",(0,0),(-1,-1),10),
        ("TOPPADDING",(0,0),(-1,-1),7),("BOTTOMPADDING",(0,0),(-1,-1),7),
    ]))
    story.append(t); sp(7)

def callout(title, txt): box(title, txt, VERDE, VERDE_BORDE)
def warn(title, txt): box(title, txt, AMBAR, AMBAR_BORDE)
def danger(title, txt): box(title, txt, ROJO, ROJO_BORDE)

def table(data, widths, header=True):
    t = Table(data, colWidths=widths, repeatRows=1 if header else 0)
    style = [
        ("FONTNAME",(0,0),(-1,-1),"Helvetica"),
        ("FONTSIZE",(0,0),(-1,-1),8.6),
        ("VALIGN",(0,0),(-1,-1),"TOP"),
        ("GRID",(0,0),(-1,-1),0.4,GRIS_BORDE),
        ("LEFTPADDING",(0,0),(-1,-1),5),("RIGHTPADDING",(0,0),(-1,-1),5),
        ("TOPPADDING",(0,0),(-1,-1),4),("BOTTOMPADDING",(0,0),(-1,-1),4),
        ("ROWBACKGROUNDS",(0, 1 if header else 0),(-1,-1),[colors.white, GRIS]),
    ]
    if header:
        style += [("BACKGROUND",(0,0),(-1,0),AZUL),("TEXTCOLOR",(0,0),(-1,0),colors.white),
                  ("FONTNAME",(0,0),(-1,0),"Helvetica-Bold")]
    t.setStyle(TableStyle(style))
    story.append(t); sp(8)

# ============================ PORTADA ============================
def portada():
    story.append(Spacer(1, 55*mm))
    story.append(Paragraph("Guía de Implementación", ParagraphStyle("pt", parent=H1, fontSize=26, alignment=TA_CENTER, textColor=AZUL, spaceAfter=4)))
    story.append(Paragraph("Visor DICOM — Portal Web Radiólogos", ParagraphStyle("pt2", parent=H1, fontSize=19, alignment=TA_CENTER, textColor=AZUL2, spaceAfter=18)))
    story.append(Paragraph("Instalación y configuración de Orthanc en Windows Server 2012 R2, IIS/ARR, OHIF e integración con la aplicación .NET 8", ParagraphStyle("pt3", parent=P, alignment=TA_CENTER, fontSize=12, textColor=colors.HexColor("#444"))))
    story.append(Spacer(1, 12*mm))
    story.append(Paragraph("https://appsintranet.esculapiosis.com/PortalImagenologia", ParagraphStyle("url", parent=P, alignment=TA_CENTER, fontName="Courier", fontSize=11, textColor=NARANJA)))
    story.append(Spacer(1, 30*mm))
    meta = Table([["Proyecto", "webImagenologia (WILSP1971/webImagenologia)"],
                  ["Servidor", "Windows Server 2012 R2 · IIS · .NET 8"],
                  ["PACS", "dcm4chee (DIMSE/WADO-URI) + Orthanc (DICOMweb)"],
                  ["Fecha", "2026-08-07"],
                  ["Versión guía", "1.0"]],
                 colWidths=[35*mm, 120*mm])
    meta.setStyle(TableStyle([
        ("FONTNAME",(0,0),(0,-1),"Helvetica-Bold"),("FONTNAME",(1,0),(1,-1),"Helvetica"),
        ("FONTSIZE",(0,0),(-1,-1),10),("TEXTCOLOR",(0,0),(0,-1),AZUL),
        ("LINEBELOW",(0,0),(-1,-1),0.4,GRIS_BORDE),
        ("TOPPADDING",(0,0),(-1,-1),5),("BOTTOMPADDING",(0,0),(-1,-1),5),
    ]))
    story.append(meta)
    story.append(PageBreak())

# ============================ TOC ============================
def toc():
    h1("Contenido")
    items = [
        "1.  Contexto del proyecto",
        "2.  Cómo expone los datos el PACS (hallazgos reales)",
        "3.  Arquitectura de la solución (Plan A y Plan B)",
        "4.  Instalación y configuración de Orthanc (Windows Server 2012 R2)",
        "5.  IIS: reverse proxy ARR + URL Rewrite (sub-aplicación y hardening)",
        "6.  OHIF: build y despliegue como aplicación estática",
        "7.  Integración con la aplicación .NET 8 (gateway, token, auditoría)",
        "8.  Seguridad y hardening",
        "9.  Plan de pruebas end-to-end",
        "10. Bitácora de operación del enjambre (esta sesión)",
        "11. Solución de problemas y puesta en marcha de Orthanc",
        "Anexo A. Datos reales de referencia",
    ]
    for it in items:
        story.append(Paragraph(it, TOCI))
    story.append(PageBreak())

# ============================ CAP TROUBLESHOOTING (se inserta antes del anexo) ============================
def cap_trouble():
    h1("11. Solución de problemas y puesta en marcha de Orthanc")
    h3("11.1 Error 1359 al detener/reiniciar el servicio Orthanc")
    p("\"Windows no pudo detener el servicio Orthanc. Error 1359: Error interno\" es un fallo del control de servicios (SCM), <b>no</b> del <font face='Courier' size=9>orthanc.json</font> (la config solo se lee al arrancar). Fuérzalo por línea de comandos (CMD/PowerShell <b>como Administrador</b> en el servidor):")
    code(
"net stop Orthanc\n"
"REM si vuelve a fallar con 1359, mata el proceso y arranca:\n"
"taskkill /F /IM Orthanc.exe\n"
"net start Orthanc")
    h3("11.2 Validar el orthanc.json en modo consola (paso clave)")
    p("Si Orthanc no vuelve a arrancar, corre el binario a mano para ver el error real (ajusta rutas):")
    code('"C:\\Program Files\\Orthanc Server\\Orthanc.exe" --verbose "C:\\Program Files\\Orthanc Server\\Configuration"')
    p("Si ves <font face='Courier' size=9>HTTP server listening on port 8042</font>, el JSON es válido. Causas típicas de fallo:")
    bullets([
        "<b>Faltan las carpetas</b> <font face='Courier' size=9>C:\\Orthanc\\Storage</font> / <font face='Courier' size=9>C:\\Orthanc\\Index</font> → créalas (<font face='Courier' size=9>mkdir</font>).",
        "<b>Claves duplicadas</b>: la instalación lee TODOS los <font face='Courier' size=9>*.json</font> de <font face='Courier' size=9>Configuration\\</font> y los fusiona; una clave repetida en dos archivos rompe el arranque. Deja un solo archivo con estas claves.",
        "<b>Ruta de Plugins incorrecta</b>: verifica que exista <font face='Courier' size=9>C:\\Program Files\\Orthanc Server\\Plugins</font> con <font face='Courier' size=9>OrthancDicomWeb.dll</font> y <font face='Courier' size=9>OrthancStoneWebViewer.dll</font>.",
    ])
    p("Para ver qué config usa el servicio: <font face='Courier' size=9>sc.exe qc Orthanc</font> (¡con <b>.exe</b>! en PowerShell <font face='Courier' size=9>sc</font> es alias de Set-Content) → mira el <font face='Courier' size=9>BINARY_PATH_NAME</font>. En la instalación real: <font face='Courier' size=9>C:\\Program Files\\Orthanc Server\\OrthancService.exe</font>, y lee TODOS los <font face='Courier' size=9>*.json</font> de <font face='Courier' size=9>Configuration\\</font>.")
    warn("Error real observado: SQLite Unable to open the database (code 1002)", "Orthanc arranca, carga plugins y luego muere con <font face='Courier' size=9>[SQLite: Unable to open the database]</font>. Causa: <b>el directorio de la base de datos no existe</b>. El instalador de Windows deja por defecto <font face='Courier' size=9>StorageDirectory</font> e <font face='Courier' size=9>IndexDirectory</font> en <font face='Courier' size=9>C:\\Orthanc</font>. Solución: crear la carpeta y arrancar.")
    code(
"New-Item -ItemType Directory -Force -Path C:\\Orthanc | Out-Null\n"
"Start-Service Orthanc\n"
"Get-Service Orthanc            # debe quedar Running; probar http://localhost:8042")
    h3("11.2b Aplicar la config del gateway: editar el archivo core (NO usar override)")
    danger("Orthanc 1.12.1 prohíbe claves duplicadas entre archivos", "La instalación es modular: cada plugin tiene su <font face='Courier' size=9>.json</font> (DicomWeb→dicomweb.json, StoneWebViewer→stone-webviewer.json, …) y NINGUNA clave se repite. Si agregas un archivo de override con una clave que ya existe (p.ej. <font face='Courier' size=9>DicomAet</font> en <font face='Courier' size=9>orthanc.json</font>), Orthanc aborta: <font face='Courier' size=9>Bad file format: the configuration section \"DicomAet\" is defined in 2 different configuration files (code 15)</font>. <b>No uses archivos de override que dupliquen claves.</b>")
    p("La forma correcta es editar el archivo que <b>ya</b> contiene cada clave: las claves core (DicomAet, DicomPort, DicomModalities, StorageDirectory, IndexDirectory, RemoteAccessAllowed…) van en <font face='Courier' size=9>orthanc.json</font>; la sección DicomWeb va en <font face='Courier' size=9>dicomweb.json</font>. Reemplaza el <font face='Courier' size=9>orthanc.json</font> por una versión core con nuestros valores (SIN secciones DicomWeb/StoneWebViewer):")
    code(
'{\n'
'  "Name": "Esculapio DICOMweb Gateway",\n'
'  "StorageDirectory": "C:/Orthanc/Storage",\n'
'  "IndexDirectory": "C:/Orthanc/Index",\n'
'  "HttpServerEnabled": true, "HttpPort": 8042, "SslEnabled": false,\n'
'  "RemoteAccessAllowed": false, "AuthenticationEnabled": false,\n'
'  "DicomServerEnabled": true, "DicomAet": "ESCULAPIO_ORTHANC", "DicomPort": 4242,\n'
'  "DicomAlwaysAllowEcho": true, "DicomCheckModalityHost": true,\n'
'  "DicomModalities": {\n'
'    "pacs": { "AET": "DCM4CHEE", "Host": "172.16.10.100", "Port": 11112,\n'
'              "AllowEcho": true, "AllowFind": true, "AllowMove": true, "AllowGet": true, "AllowStore": true }\n'
'  },\n'
'  "Plugins": [ "C:/Program Files/Orthanc Server/Plugins" ]\n'
'}')
    p("La sección DicomWeb (Root=<font face='Courier' size=9>/PortalImagenologia/dicomweb/</font>) se edita aparte en <font face='Courier' size=9>dicomweb.json</font>, no en orthanc.json. Tras reemplazar: <font face='Courier' size=9>Restart-Service Orthanc</font> y verificar:")
    code(
"Invoke-RestMethod http://localhost:8042/system | Select Name,DicomAet,Version\n"
"Invoke-RestMethod -Method Post http://localhost:8042/modalities/pacs/echo")
    callout("OHIF ya viene integrado en Orthanc 1.12.1", "El log de arranque muestra <font face='Courier' size=9>Registering plugin 'ohif' (version 1.0)</font> sirviendo en <font face='Courier' size=9>/ohif/</font>, además de DICOMweb 1.15 y Stone Web Viewer 2.5. Es muy probable que <b>no haga falta compilar OHIF por separado</b>: Orthanc lo sirve directamente. Se valida abriendo un estudio ya en caché por <font face='Courier' size=9>/ohif/</font>.")
    h3("11.3 Acceso al PACS para registrar ESCULAPIO_ORTHANC (para C-MOVE)")
    p("dcm4chee corre en otro servidor (<font face='Courier' size=9>172.16.10.100</font>). Se administra por navegador; según la versión:")
    bullets([
        "<b>dcm4chee-arc 5.x</b>: <font face='Courier' size=9>http://172.16.10.100:8080/dcm4chee-arc/ui2</font> → Configuration → Devices/AE → nuevo Network AE.",
        "<b>dcm4chee 2.x</b>: <font face='Courier' size=9>http://172.16.10.100:8080/dcm4chee-web3</font> → AE Management → New AET.",
    ])
    p("Registrar: AET <font face='Courier' size=9>ESCULAPIO_ORTHANC</font>, Host = IP del servidor web (donde corre Orthanc), Port <b>4242</b>. Requiere credenciales de administrador del PACS.")
    callout("Atajo: C-GET evita tocar el PACS", "Si dcm4chee soporta <b>C-GET</b>, Orthanc recupera por la misma conexión saliente y NO hace falta registrar su AE en el PACS (config ya trae <font face='Courier' size=9>AllowGet: true</font>). Pruébalo antes de depender del admin del PACS. C-ECHO (<font face='Courier' size=9>POST /modalities/pacs/echo</font>) no requiere registro y valida conectividad.")
    h3("11.4 Herramientas a descargar (servidor web)")
    table([
        ["Herramienta", "Dónde", "Archivo"],
        ["Orthanc Windows (plugins DICOMweb+Stone)", "orthanc.uclouvain.be/downloads/windows-64", "instalador .exe"],
        ["IIS URL Rewrite 2.1", "iis.net/downloads/microsoft/url-rewrite", "rewrite_amd64_en-US.msi"],
        ["IIS ARR 3.0", "iis.net/downloads/microsoft/application-request-routing", "requestRouter_amd64.msi"],
        ["OHIF Viewer (compilar en PC dev)", "github.com/OHIF/Viewers (v3.11.0)", "—"],
    ], [58*mm, 70*mm, 37*mm])
    p("Los archivos de configuración (orthanc.json con AE real, reglas IIS/ARR, config y web.config de OHIF) se entregan en el paquete <font face='Courier' size=9>PortalImagenologia-config.zip</font> con su <font face='Courier' size=9>LEEME-INSTALACION.txt</font>.")

# ============================ CAP 1 ============================
def cap1():
    h1("1. Contexto del proyecto")
    p("Existe una aplicación web de Imagenología desarrollada en <b>C# / .NET 8</b>, publicada y funcional en <b>IIS</b> sobre <b>Windows Server 2012 R2</b>, disponible en:")
    code("https://appsintranet.esculapiosis.com/PortalImagenologia")
    p("La aplicación administra hoy la información clínica de estudios de imágenes diagnósticas. La data no sale de una BD directa: proviene de una <b>API externa (\"Esculapio\", IEsculapioApiClient)</b>. La autenticación es por cookie + IDataProtection, con roles <b>Administrador</b> y <b>Radiologo</b>.")
    p("Se requiere incorporar un <b>visor de imágenes diagnósticas</b> para estudios DICOM que residen en un servidor <b>PACS</b>. El estudio se localiza por <b>(a) Número de Caso/Cuenta</b> o <b>(b) Número de Identificación del paciente</b>.")
    p("El módulo donde se implementa el visor es <b>Portal Web Radiólogos</b>: <font face='Courier' size=9>Controllers/PortalRadiologosController.cs</font>, vista <font face='Courier' size=9>Views/PortalRadiologos/Index.cshtml</font>, <font face='Courier' size=9>wwwroot/js/portalRadiologos.js</font>.")
    warn("Restricción dura", "Todo lo nuevo del visor va en módulos/carpetas propios. <b>No se modifica el código operativo y funcional existente.</b> Nunca se suben al repositorio credenciales, TokenSecret ni archivos con datos de pacientes (PHI).")

# ============================ CAP 2 ============================
def cap2():
    h1("2. Cómo expone los datos el PACS (hallazgos reales)")
    p("Estos datos <b>no son suposiciones</b>: se extrajeron de la captura de red real del visor Oviyam en producción (endpoint <font face='Courier' size=9>DicomNodes.do</font> y <font face='Courier' size=9>Echo.do</font> de un archivo HAR).")
    h3("Backends y orígenes")
    p("Existen dos backends PACS y cuatro orígenes lógicos (las pestañas de Oviyam):")
    table([
        ["Pestaña", "Backend", "Host", "Puerto", "AE Title", "Recuperación"],
        ["CAMPBELL", "dcm4chee", "172.16.10.100", "11112", "DCM4CHEE", "WADO-URI :8080/wado (JPEG)"],
        ["FUNDACIONCAMPBELL", "dcm4chee", "172.16.10.100", "11112", "DCM4CHEE", "WADO-URI :8080/wado (JPEG)"],
        ["SANTAMARTA", "dcm4chee", "172.16.50.100", "11112", "DCM4CHEE", "WADO-URI :8080/wado (JPEG)"],
        ["VISORIMAGENLOGIA", "Orthanc", "192.168.2.17", "4242", "ESCULAPIO_ORTHANC", "WADO-URI :8080/wado (JPEG)"],
    ], [30*mm, 22*mm, 27*mm, 16*mm, 30*mm, 40*mm])
    p("AE llamante de Oviyam: <font face='Courier' size=9>OVIYAM2</font> (listener 1025). C-ECHO real verificado y exitoso contra <font face='Courier' size=9>DCM4CHEE@172.16.10.100:11112</font> → <b>EchoSuccess</b>.")
    h3("Las dos vías de exposición")
    bullets([
        "<b>DIMSE (DICOM clásico, TCP)</b>: C-FIND (búsqueda), C-MOVE / C-GET (recuperación). Puertos 11112 (dcm4chee) y 4242 (Orthanc).",
        "<b>WADO-URI (HTTP)</b>: recupera la imagen ya rasterizada — <font face='Courier' size=9>http://host:8080/wado?requestType=WADO&studyUID=...&objectUID=...</font>. Es lo que usa Oviyam hoy, devolviendo JPEG.",
    ])
    warn("Limitación clave", "dcm4chee expone WADO-URI clásico (imagen JPEG/PNG por objeto) pero <b>no</b> DICOMweb REST moderno (QIDO-RS / WADO-RS / STOW-RS) que necesitan OHIF/Cornerstone para calidad diagnóstica (window/level dinámico, MPR). De ahí la estrategia del gateway Orthanc.")
    danger("Corrección respecto al prompt inicial", "El AE Title real es <b>DCM4CHEE</b> (no <font face='Courier' size=9>PACS_SERVER</font>) y el WADO está en el puerto <b>8080</b>, contexto <font face='Courier' size=9>/wado</font>. Los valores tipo <font face='Courier' size=9>PACS_SERVER</font> o <font face='Courier' size=9>wadoUrl=http://172.16.10</font> eran placeholders.")

# ============================ CAP 3 ============================
def cap3():
    h1("3. Arquitectura de la solución (Plan A y Plan B)")
    h3("Plan A — camino principal: Orthanc como pasarela DICOMweb + OHIF")
    p("Orthanc se instala como pasarela: consulta el PACS clásico por DIMSE (C-FIND/C-MOVE), cachea el estudio y lo re-expone como <b>DICOMweb moderno</b>. El visor (OHIF/Cornerstone) consume DICOMweb; la app .NET actúa como broker de seguridad (resuelve caso→StudyInstanceUID, emite token corto, audita).")
    code(
"[Radiólogo] --HTTPS--> IIS /PortalImagenologia (.NET 8)\n"
"                          |  (emite token corto, audita, autoriza)\n"
"                          v\n"
"                    Orthanc (gateway DICOMweb)  --C-FIND/C-MOVE (DIMSE)--> dcm4chee\n"
"                    localhost:8042                                          172.16.10.100 / .50.100\n"
"                    /PortalImagenologia/dicomweb  (QIDO-RS / WADO-RS)\n"
"                          ^\n"
"                          |  DICOMweb (JSON + DICOM-P10)\n"
"                    [OHIF / Cornerstone en el navegador]")
    h3("Plan B — contingencia: visor 2D sobre WADO-URI JPEG de dcm4chee")
    p("Si Orthanc no está desplegado a tiempo, un visor 2D ligero consume el <b>WADO-URI JPEG</b> que dcm4chee ya expone (el mismo de Oviyam). Es de <b>fidelidad reducida</b> (sin window/level real ni MPR) pero no bloquea la entrega. Queda como contingencia explícita, no como diseño paralelo.")
    table([
        ["Aspecto", "Plan A · DICOMweb + OHIF (nativo)", "Plan B · WADO-URI JPEG 2D"],
        ["Qué llega al navegador", "DICOM crudo (16 bits, metadata)", "Foto JPEG (8 bits, revelado fijo)"],
        ["Window/Level dinámico", "Sí, real", "No"],
        ["MPR (multiplanar)", "Sí", "No"],
        ["Mediciones", "Precisas (escala física)", "Limitadas"],
        ["Peso / dependencia", "Mayor; requiere Orthanc", "Muy ligero; ya disponible"],
        ["Apto lectura diagnóstica", "Sí", "Solo vista de referencia"],
    ], [38*mm, 68*mm, 59*mm])
    callout("Decisión del PLAN-002 (aprobado)", "Se elige <b>Plan A (Orthanc/OHIF)</b> como camino principal; <b>Plan B</b> queda documentado como contingencia si Orthanc no puede desplegarse/configurarse a tiempo. El despliegue de Orthanc se trata como precondición dura con evidencia real (C-ECHO/C-FIND).")

# ============================ CAP 4 ============================
def cap4():
    h1("4. Instalación y configuración de Orthanc (Windows Server 2012 R2)")
    h3("4.1 Prerrequisitos en el servidor")
    bullets([
        "Windows Server 2012 R2 x64, con permisos de administrador.",
        "Microsoft Visual C++ Redistributable x64 (el que pida el instalador de Orthanc; habitualmente 2015–2022 x64).",
        "Espacio en disco para la caché de Orthanc (dimensionar según volumen; guía usa ~20 GB).",
        "Puertos libres: 8042/TCP (HTTP REST/DICOMweb, solo localhost) y 4242/TCP (DICOM SCP).",
    ])
    warn("Compatibilidad de versión en 2012 R2", "2012 R2 es un SO antiguo. Instala la versión estable de Orthanc para Windows y <b>verifica que el servicio arranca</b>. Si una build muy reciente no inicia (dependencia de runtime/OS), usa una versión LTS previa de Orthanc. Valida siempre contra la máquina real antes de dar por cerrado el paso.")
    h3("4.2 Instalar Orthanc")
    bullets([
        "Descarga el <b>instalador oficial de Windows</b> (Orthanc Team / Osimis). Trae los plugins <b>DICOMweb</b> y <b>Stone Web Viewer</b>.",
        "Instálalo; queda como <b>servicio de Windows</b>. Ruta típica de plugins: <font face='Courier' size=9>C:/Program Files/Orthanc Server/Plugins</font>.",
        "Crea las carpetas de datos: <font face='Courier' size=9>C:/Orthanc/Storage</font> y <font face='Courier' size=9>C:/Orthanc/Index</font>.",
    ])
    h3("4.3 Configurar orthanc.json")
    p("Reemplaza (o fusiona) el <font face='Courier' size=9>orthanc.json</font> de la instalación con esta configuración (comentada). Ajusta rutas, AET y, según el PACS real, el <b>AET del modality</b>:")
    code(
'{\n'
'  "Name": "Esculapio DICOMweb Gateway",\n'
'  "StorageDirectory": "C:/Orthanc/Storage",\n'
'  "IndexDirectory": "C:/Orthanc/Index",\n'
'  "MaximumStorageSize": 20000,        // ~20 GB de cache\n'
'  "MaximumStorageMode": "Recycle",    // recicla lo mas viejo al llenarse\n'
'\n'
'  "HttpServerEnabled": true,\n'
'  "HttpPort": 8042,\n'
'  "SslEnabled": false,                // el TLS lo termina IIS\n'
'  "RemoteAccessAllowed": false,       // *** solo localhost: lo alcanza IIS/ARR ***\n'
'  "AuthenticationEnabled": false,     // seguro porque solo escucha en localhost\n'
'\n'
'  "DicomServerEnabled": true,\n'
'  "DicomAet": "ESCULAPIO_ORTHANC",    // *** registrar este AET EN el PACS (host+4242) ***\n'
'  "DicomPort": 4242,\n'
'  "DefaultEncoding": "Latin1",        // acentos legados; "Utf8" si el PACS lo emite\n'
'  "DicomAlwaysAllowEcho": true,\n'
'  "DicomAlwaysAllowStore": false,\n'
'  "DicomAlwaysAllowFind": false,\n'
'  "DicomAlwaysAllowMove": false,\n'
'  "DicomCheckModalityHost": true,\n'
'\n'
'  "DicomModalities": {\n'
'    "pacs": {\n'
'      "AET": "DCM4CHEE",              // AE REAL del PACS (confirmado por HAR)\n'
'      "Host": "172.16.10.100",\n'
'      "Port": 11112,\n'
'      "AllowEcho": true, "AllowFind": true, "AllowMove": true, "AllowGet": true, "AllowStore": true\n'
'    }\n'
'  },\n'
'\n'
'  "Plugins": [ "C:/Program Files/Orthanc Server/Plugins" ],\n'
'\n'
'  "DicomWeb": {\n'
'    "Enable": true,\n'
'    "Root": "/PortalImagenologia/dicomweb/",   // MISMO sub-path publico del proxy\n'
'    "EnableWado": true,\n'
'    "WadoRoot": "/PortalImagenologia/wado",\n'
'    "StudiesMetadata": "Full",                 // OHIF necesita metadata completa\n'
'    "SeriesMetadata": "Full",\n'
'    "QidoCaseSensitive": false\n'
'  },\n'
'\n'
'  "StoneWebViewer": { "DateFormat": "DD/MM/YYYY", "ShowInfoPanelAtStartup": "Always" }\n'
'}')
    p("Reinicia el servicio de Windows de Orthanc para aplicar la configuración.")
    danger("El AET del PACS debe ser el REAL", "En el andamiaje aparecía <font face='Courier' size=9>PACS_SERVER</font>, pero el HAR confirmó que el AE real de dcm4chee es <b>DCM4CHEE</b>. Usa el valor real o el C-FIND/C-MOVE fallará.")
    h3("4.4 Registro cruzado PACS ⇄ Orthanc (el paso que más se olvida)")
    p("Orthanc <b>no</b> reexpone el PACS automáticamente: su QIDO solo ve lo que tiene en caché. El flujo es C-FIND (localizar) → C-MOVE (traer a la caché) → recién ahí OHIF lo lee por DICOMweb. Para que el <b>C-MOVE</b> funcione, el PACS abre una asociación C-STORE de vuelta hacia Orthanc, así que:")
    bullets([
        "<b>En dcm4chee</b>: registrar a Orthanc como nodo/destino — AET <font face='Courier' size=9>ESCULAPIO_ORTHANC</font>, host = IP del servidor de Orthanc, puerto <b>4242</b>.",
        "<b>En Orthanc</b>: el PACS ya está declarado en <font face='Courier' size=9>DicomModalities.pacs</font>.",
    ])
    h3("4.5 Firewall")
    bullets([
        "Orthanc → PACS en <b>11112/TCP</b> (C-FIND/C-MOVE saliente).",
        "PACS → Orthanc en <b>4242/TCP</b> (C-STORE de retorno del C-MOVE).",
    ])
    h3("4.6 Pruebas de conectividad DICOM")
    p("Desde la REST de Orthanc (en el servidor):")
    code(
"# C-ECHO al PACS (debe responder 200)\n"
"POST http://localhost:8042/modalities/pacs/echo\n\n"
"# C-FIND por AccessionNumber real\n"
'POST http://localhost:8042/modalities/pacs/query\n'
'{ "Level": "Study", "Query": { "AccessionNumber": "<numero_real>" } }')
    callout("Orden de pruebas sugerido", "1) <font face='Courier' size=9>http://localhost:8042</font> responde. &nbsp; 2) C-ECHO → 200. &nbsp; 3) C-FIND con un AccessionNumber real devuelve el estudio. &nbsp; 4) Abrir con motor <b>Stone</b> (sin build) valida el C-MOVE punta a punta. &nbsp; 5) Luego desplegar OHIF.")

# ============================ CAP 5 ============================
def cap5():
    h1("5. IIS: reverse proxy ARR + URL Rewrite")
    p("Orthanc escucha solo en <font face='Courier' size=9>localhost:8042</font>; nadie de la red lo alcanza directo. IIS publica el DICOMweb bajo el mismo dominio HTTPS y sub-path del portal, terminando TLS y aplicando reglas de solo-lectura.")
    h3("5.1 Requisitos IIS")
    bullets([
        "Instalar <b>Application Request Routing (ARR)</b> y <b>URL Rewrite</b> en IIS.",
        "En ARR: habilitar <b>Enable proxy</b> (Server Farms / ARR settings).",
    ])
    h3("5.2 Estructura de aplicaciones anidadas")
    code(
"/PortalImagenologia          -> app ASP.NET Core (portal .NET 8)\n"
"/PortalImagenologia/ohif     -> Application estatica (OHIF)\n"
"/PortalImagenologia/dicomweb -> regla ARR: reverse proxy hacia http://localhost:8042/...\n"
"/PortalImagenologia/wado     -> (idem, si se usa WADO-URI)")
    h3("5.3 Regla de reescritura (solo-lectura hacia Orthanc)")
    p("La regla endurecida reenvía GET a Orthanc y <b>rechaza POST/PUT/DELETE/PATCH</b> hacia <font face='Courier' size=9>/dicomweb</font> (bloquea STOW). En URL Rewrite hay que registrar las <i>server variables</i> <font face='Courier' size=9>HTTP_X_FORWARDED_PROTO</font> y <font face='Courier' size=9>HTTP_X_FORWARDED_HOST</font> (y <font face='Courier' size=9>HTTP_AUTHORIZATION</font> si se activa auth) en <b>View Server Variables</b>.")
    code(
'<!-- web.config del portal: reenvio de /dicomweb a Orthanc en localhost -->\n'
'<rule name="dicomweb-proxy" stopProcessing="true">\n'
'  <match url="^dicomweb/(.*)" />\n'
'  <conditions>\n'
'    <add input="{REQUEST_METHOD}" pattern="^(GET|HEAD|OPTIONS)$" />\n'
'  </conditions>\n'
'  <action type="Rewrite" url="http://localhost:8042/PortalImagenologia/dicomweb/{R:1}" />\n'
'  <serverVariables>\n'
'    <set name="HTTP_X_FORWARDED_PROTO" value="https" />\n'
'    <set name="HTTP_X_FORWARDED_HOST" value="{HTTP_HOST}" />\n'
'  </serverVariables>\n'
'</rule>')
    warn("Gotcha de herencia", "El <font face='Courier' size=9>web.config</font> del portal padre debe envolver su <font face='Courier' size=9>&lt;system.webServer&gt;</font> en <font face='Courier' size=9>&lt;location path=\".\" inheritInChildApplications=\"false\"&gt;</font>. Si no, el handler de ASP.NET Core y las reglas ARR se heredan en la app hija (OHIF) y rompen el servido estático.")

# ============================ CAP 6 ============================
def cap6():
    h1("6. OHIF: build y despliegue como aplicación estática")
    p("OHIF es una SPA en React: se <b>compila una vez</b> en una PC de desarrollo (Node LTS 18/20 + Yarn) y se publican los estáticos bajo <font face='Courier' size=9>/PortalImagenologia/ohif</font>. <b>El Windows Server 2012 R2 no compila</b> OHIF; solo sirve los estáticos.")
    h3("6.1 Build (en PC de desarrollo)")
    code(
"git clone https://github.com/OHIF/Viewers.git\n"
"cd Viewers\n"
"git checkout v3.11.0            # release v3 estable probada\n"
"yarn install\n"
"# copiar esculapio.js -> platform/app/public/config/esculapio.js\n"
"# Windows PowerShell:\n"
'$env:PUBLIC_URL="/PortalImagenologia/ohif/"; $env:APP_CONFIG="config/esculapio.js"; yarn build')
    p("Resultado en <font face='Courier' size=9>platform/app/dist/</font>. Verifica que <font face='Courier' size=9>dist/app-config.js</font> tenga <font face='Courier' size=9>routerBasename: \"/PortalImagenologia/ohif\"</font> y que el data source apunte a <font face='Courier' size=9>/PortalImagenologia/dicomweb</font>.")
    h3("6.2 Publicar en el servidor")
    bullets([
        "Copiar todo <font face='Courier' size=9>platform/app/dist/</font> a <font face='Courier' size=9>C:\\inetpub\\...\\PortalImagenologia\\ohif\\</font>.",
        "Colocar el <font face='Courier' size=9>web.config</font> de OHIF (MIME .wasm + fallback SPA) en la raíz de <font face='Courier' size=9>ohif\\</font>.",
        "En IIS Manager: clic derecho sobre <font face='Courier' size=9>ohif</font> → <b>Convert to Application</b>, con App Pool propio <b>\"No Managed Code\"</b> (es estático).",
    ])
    warn("Si carga la UI pero no las imágenes", "Casi siempre es: (a) el MIME de <font face='Courier' size=9>.wasm</font> (lo cubre el web.config de OHIF), (b) el <font face='Courier' size=9>DicomWeb.Root</font> de Orthanc no coincide con <font face='Courier' size=9>/PortalImagenologia/dicomweb</font>, o (c) el estudio aún no llegó por C-MOVE (revisa el job en Orthanc).")

# ============================ CAP 7 ============================
def cap7():
    h1("7. Integración con la aplicación .NET 8")
    p("La app .NET es el <b>broker</b>: autentica al radiólogo, resuelve el estudio y emite un token corto para que el visor acceda al DICOMweb. Archivos de referencia (andamiaje en <font face='Courier' size=9>ActualizacionCodigo/</font>, no versionado):")
    table([
        ["Archivo", "Rol"],
        ["OrthancGatewayService.cs", "C-FIND (buscar en PACS) y C-MOVE (traer a Orthanc) vía REST de Orthanc"],
        ["VisorController.cs", "Endpoints del visor: resolver caso→estudio, abrir visor, emitir token"],
        ["VisorTokenService.cs", "Token corto (JWT ~10 min) para acceso temporal al estudio"],
        ["VisorAuditoriaService.cs", "Trazabilidad: quién abrió qué estudio y cuándo"],
        ["appsettings.Visor.json", "Config: OrthancRestBaseUrl, OrthancDicomWebBaseUrl, OrthancAet, TokenMinutos"],
    ], [55*mm, 110*mm])
    h3("7.1 Configuración (appsettings — sección Visor)")
    code(
'"Visor": {\n'
'  "OrthancRestBaseUrl": "http://localhost:8042",\n'
'  "OrthancDicomWebBaseUrl": "http://localhost:8042/PortalImagenologia/dicomweb",\n'
'  "OrthancAet": "ESCULAPIO_ORTHANC",\n'
'  "PacsModalityName": "pacs",\n'
'  "TokenSecret": "<NO subir al repo: User-Secrets / variable de entorno>",\n'
'  "TokenMinutos": 10\n'
'}')
    h3("7.2 Flujo de búsqueda (dos llaves de acceso)")
    code(
"Caso/Cuenta  --+\n"
"               +--> [.NET] resuelve -> PatientID / StudyInstanceUID\n"
"Identificacion-+          |\n"
"                          v\n"
"             C-FIND (Orthanc->PACS) -> C-MOVE a cache Orthanc\n"
"                          |\n"
"                          v\n"
"             OHIF abre /ohif/viewer?StudyInstanceUIDs={UID}")
    p("La app ya tiene la relación clínica caso↔paciente (vía API Esculapio); el visor solo necesita el <b>StudyInstanceUID</b> para el QIDO/WADO-RS. Conectar la pasarela al VisorController según <font face='Courier' size=9>gateway-wiring.txt</font> (Resolver usa C-FIND; Abrir dispara C-MOVE).")

# ============================ CAP 8 ============================
def cap8():
    h1("8. Seguridad y hardening")
    bullets([
        "<b>HTTPS/TLS</b>: lo termina IIS; Orthanc con <font face='Courier' size=9>SslEnabled: false</font>.",
        "<b>Aislamiento de Orthanc</b>: <font face='Courier' size=9>RemoteAccessAllowed: false</font> → solo 127.0.0.1. Lo alcanzan únicamente IIS/ARR y el backend en la misma máquina.",
        "<b>Solo-lectura</b>: el proxy endurecido rechaza POST/PUT/DELETE/PATCH hacia /dicomweb (bloquea STOW). Opcional: <font face='Courier' size=9>orthanc-readonly.lua</font> para reforzarlo en Orthanc.",
        "<b>DICOM entrante restringido</b>: <font face='Courier' size=9>DicomAlwaysAllowStore/Find/Move: false</font> + <font face='Courier' size=9>DicomCheckModalityHost: true</font> → solo la modalidad <font face='Courier' size=9>pacs</font> declarada opera.",
        "<b>Autenticación</b>: la app .NET autentica al radiólogo (cookie + roles Administrador/Radiologo). El visor recibe un token corto (JWT ~10 min).",
        "<b>Autorización por estudio</b>: el token se emite solo tras validar que el usuario puede ver ese caso.",
        "<b>Auditoría / trazabilidad</b>: VisorAuditoriaService registra accesos a estudios.",
        "<b>PHI</b>: nunca subir HAR, capturas con datos de pacientes ni credenciales a GitHub.",
    ])
    callout("Defensa en profundidad opcional", "Si algún día se expone Orthanc fuera de localhost: <font face='Courier' size=9>AuthenticationEnabled: true</font> + <font face='Courier' size=9>RegisteredUsers</font>, e inyectar el header <font face='Courier' size=9>Authorization</font> en la regla ARR (más <font face='Courier' size=9>OrthancUser/OrthancPassword</font> en appsettings).")

# ============================ CAP 9 ============================
def cap9():
    h1("9. Plan de pruebas end-to-end")
    bullets([
        "Servicio Orthanc arriba → <font face='Courier' size=9>http://localhost:8042</font> responde en el servidor.",
        "C-ECHO al PACS → 200 (<font face='Courier' size=9>POST /modalities/pacs/echo</font>).",
        "C-FIND con un AccessionNumber real → devuelve el estudio.",
        "Motor <b>Stone</b> (sin build): abrir un estudio → Orthanc hace C-MOVE y Stone lo muestra (valida la pasarela).",
        "IIS/ARR: <font face='Courier' size=9>https://appsintranet.esculapiosis.com/PortalImagenologia/dicomweb/studies</font> responde (QIDO) por HTTPS.",
        "OHIF: navegar a <font face='Courier' size=9>/PortalImagenologia/ohif/</font> carga la SPA; desde una grilla, \"Ver imágenes\" abre el estudio con branding Esculapio.",
        "Búsqueda por Caso/Cuenta y por Identificación → hasta visualizar el estudio.",
        "Auditoría: verificar que quedó el registro del acceso.",
    ])

# ============================ CAP 10 ============================
def cap10():
    h1("10. Bitácora de operación del enjambre (esta sesión)")
    p("Registro de lo realizado para que el <b>Avengers Swarm</b> produzca el diseño/plan del visor (operado por SSH en <font face='Courier' size=9>swarm@157.173.123.197</font>).")
    h3("10.1 Documentación y prompt")
    bullets([
        "Se creó <font face='Courier' size=9>docs/PACS-exposicion.md</font> con los datos reales del PACS (extraídos del HAR).",
        "Se creó <font face='Courier' size=9>docs/PROMPT-visor-diagnostico.md</font> (prompt maestro que reemplaza planes previos).",
        "Commits en <font face='Courier' size=9>WILSP1971/webImagenologia</font> (rama main).",
    ])
    h3("10.2 Reset del plan anterior y relanzamiento")
    bullets([
        "Se archivó PLAN-001 en <font face='Courier' size=9>~/proyectos/.swarm_archive_20260807_reset/</font> y se limpió el estado vivo del <font face='Courier' size=9>.swarm</font>.",
        "<font face='Courier' size=9>git pull</font> en el VPS para bajar los docs nuevos.",
        "Se relanzó la tarea en sesión tmux persistente <font face='Courier' size=9>tarea_webImagenologia</font>.",
    ])
    h3("10.3 Fix del bloqueo de Bash (causa de que la 1ª corrida muriera)")
    warn("Causa raíz", "En <font face='Courier' size=9>~/.claude/settings.json</font> del enjambre existía <font face='Courier' size=9>\"ask\": [\"Bash\"]</font>, que forzaba confirmación de cada Bash; en modo headless (<font face='Courier' size=9>claude -p</font>) nadie la responde y la tarea muere. Anulaba el <font face='Courier' size=9>--dangerously-skip-permissions</font>.")
    bullets([
        "Se quitó <font face='Courier' size=9>\"ask\": [\"Bash\"]</font> (backup en <font face='Courier' size=9>settings.json.bak_20260807</font>); <font face='Courier' size=9>defaultMode</font> queda en <font face='Courier' size=9>bypassPermissions</font>.",
        "Se copió el andamiaje a <font face='Courier' size=9>ActualizacionCodigo/</font> en el VPS <b>sin el HAR ni F12DOM (PHI)</b> ni <font face='Courier' size=9>Instaladores/</font> (193 MB), y se añadió a <font face='Courier' size=9>.gitignore</font>.",
    ])
    h3("10.4 Resultado: PLAN-002 aprobado")
    p("El enjambre generó el <b>PLAN-002</b> (<font face='Courier' size=9>.swarm/PLAN-002.md</font>) con Plan A (Orthanc/OHIF) como camino principal y Plan B (WADO-URI JPEG 2D) como contingencia. Quedó aprobado.")
    h3("10.5 Comandos útiles de operación")
    code(
"# Ver/lanzar tarea (headless + failover)\n"
'ssh swarm@157.173.123.197 \'bash ~/sistema-agentico/scripts/swtarea.sh webImagenologia "..."\'\n\n'
"# Modo interactivo (TUI en vivo) en tmux persistente\n"
"tmux switch-client -t tarea_webImagenologia    # si ya estas dentro de tmux\n"
"unset TMUX; tmux attach -t tarea_webImagenologia\n"
"# Salir sin cortar: Ctrl+b luego d\n\n"
"# Seguir avance sin entrar\n"
"git -C ~/proyectos/webImagenologia log --oneline -10\n"
"cat ~/proyectos/.swarm/CHECKPOINTS.md")

# ============================ ANEXO ============================
def anexo():
    h1("Anexo A. Datos reales de referencia")
    table([
        ["Parámetro", "Valor real"],
        ["PACS dcm4chee — AE Title", "DCM4CHEE"],
        ["dcm4chee — hosts", "172.16.10.100 (Campbell/Fundación), 172.16.50.100 (Santa Marta)"],
        ["dcm4chee — puerto DIMSE", "11112"],
        ["dcm4chee — WADO-URI", "http://host:8080/wado (imageType JPEG)"],
        ["Orthanc — AE Title", "ESCULAPIO_ORTHANC"],
        ["Orthanc — DICOM SCP", "192.168.2.17:4242"],
        ["Orthanc — HTTP/REST/DICOMweb", "localhost:8042"],
        ["DICOMweb Root (Orthanc e IIS)", "/PortalImagenologia/dicomweb/"],
        ["OHIF (estáticos IIS)", "/PortalImagenologia/ohif"],
        ["Portal .NET 8", "https://appsintranet.esculapiosis.com/PortalImagenologia"],
        ["Repo", "https://github.com/WILSP1971/webImagenologia"],
    ], [55*mm, 110*mm])
    danger("Recordatorio PHI", "Los IPs y AE Titles son datos de infraestructura interna. <b>Nunca</b> incluir en el repositorio archivos HAR, capturas o exportes con nombres/identificaciones de pacientes.")

# ---------------- pie de pagina ----------------
def footer(canvas, doc):
    canvas.saveState()
    canvas.setFont("Helvetica", 7.5)
    canvas.setFillColor(colors.HexColor("#888888"))
    canvas.drawString(20*mm, 12*mm, "Guía Visor DICOM — Portal Web Radiólogos · Esculapiosis")
    canvas.drawRightString(196*mm, 12*mm, "Pág. %d" % doc.page)
    canvas.setStrokeColor(GRIS_BORDE)
    canvas.line(20*mm, 15*mm, 196*mm, 15*mm)
    canvas.restoreState()

def build():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    portada(); toc(); cap1(); cap2(); cap3(); cap4(); cap5(); cap6(); cap7(); cap8(); cap9(); cap10(); cap_trouble(); anexo()
    doc = SimpleDocTemplate(OUT, pagesize=letter,
                            leftMargin=20*mm, rightMargin=19*mm, topMargin=18*mm, bottomMargin=20*mm,
                            title="Guia Visor DICOM - Portal Web Radiologos",
                            author="Proyecto webImagenologia")
    doc.build(story, onFirstPage=lambda c,d: None, onLaterPages=footer)
    print("PDF generado:", OUT)

if __name__ == "__main__":
    build()
