# FASE 12 — QA Global + Release

## Objetivo
Ejecutar todos los gates de validación global, generar documentación final y
preparar los artefactos para el despliegue en IIS + Windows Server.

## Tareas

### 1. Gates globales (todos deben estar en PASS)
Ejecutar los scripts de validación de `docs/validation-rules.md`:

```powershell
# Build completo
dotnet build src/WebImagenologia.sln --configuration Release

# Tests completos
dotnet test src/WebImagenologia.Tests/ --logger trx

# Formato
dotnet format --verify-no-changes src/WebImagenologia.sln

# Búsqueda de secrets en código
# (regex sobre src/: password\s*=\s*"[^"]{4,}" → 0 resultados)

# HttpClient unicity
# (grep new HttpClient() en src/ excluyendo EsculapioApiClient.cs → 0 resultados)

# SQL inline en controllers
# (grep SELECT|INSERT|UPDATE|DELETE en src/Controllers/ → 0 resultados)
```

### 2. Revisión de accesibilidad
- Verificar que todos los `<input>` tienen `<label>` asociado
- Verificar `aria-label` en botones de solo ícono
- Verificar `alt` en imágenes

### 3. Revisión de seguridad
- Confirmar que el ConnectionString solo existe en sesión cifrada
- Confirmar que no hay credenciales en `appsettings.json`
- Confirmar que todas las rutas de administrador tienen `[Authorize(Roles = "Administrador")]`
- Confirmar que el portal de radiólogos tiene `[Authorize(Roles = "Radiologo")]`

### 4. Documentación final en README.md
Actualizar con:
- Instrucciones completas de despliegue en IIS
- Variables de entorno requeridas
- Pasos de configuración post-deploy
- URL de la instancia N8N y workflow a importar

### 5. Notas de despliegue IIS
```
1. Publicar: dotnet publish src/WebImagenologia.Web/ -c Release -o publish/
2. Crear site en IIS apuntando a publish/
3. Application Pool: No Managed Code (o .NET CLR 4.0 según hosting model)
4. Configurar variables de entorno en el Application Pool o web.config:
   - EsculapioApi__BaseUrl
   - DataProtection__KeysPath (carpeta persistente)
5. Importar workflow N8N desde n8n/workflows/programacion-estudios.json
6. Configurar credenciales MySQL en N8N Credentials Store
```

### 6. Tests de regresión final
- Test de login con cada rol (Admin, Radiólogo, Operador)
- Test de acceso denegado en rutas protegidas
- Test de audio upload con archivo válido
- Test de audio upload rechazado con archivo inválido

## Archivos a generar / actualizar
- `README.md` (instrucciones de deploy)
- `src/WebImagenologia.Tests/RegressionTests.cs`
- `docs/deploy-notes.md`

## Gates de esta fase (todos deben ser PASS)
- `build`: ok
- `tests`: 100% verde
- `format`: ok
- `secrets`: ok
- `no-sql-inline`: ok
- `no-direct-http`: ok
- `lint-cshtml`: ok
- `accessibility`: ok (revisión manual)
- `json-n8n`: ok

## Reporte final esperado
```yaml
phase: "12"
status: PASS
artifacts:
  - README.md
  - docs/deploy-notes.md
  - publish/ (carpeta de publicación)
validations:
  build: ok
  tests: "N/N"
  lint: ok
  secrets: ok
  accessibility: ok
  json-n8n: ok
blockers: []
next_phase: null
notes: |
  Plataforma lista para deploy. N módulos implementados.
  N tests pasando. Workflow N8N importado y verificado.
```
