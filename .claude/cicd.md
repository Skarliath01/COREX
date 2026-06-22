# Corex — CI/CD

## Vue d'ensemble

```
Push feature branch
       │
       ▼
  CI: Build + Tests
       │
       ▼ (PR vers develop)
  CI: Build + Tests + Analyse statique
       │
       ▼ (merge develop → main)
  CD: Build Release + Sign + Package + Upload
       │
       ▼
  Release GitHub + Backend deploy
```

---

## Branches

| Branche | Rôle | Protection | Merge autorisé depuis |
|---------|------|-----------|----------------------|
| `main` | Production — releases stables | PR obligatoire, 0 push direct | `staging` uniquement |
| `staging` | Recette — validation QA avant prod | PR obligatoire | `dev` uniquement |
| `dev` | Intégration — toutes les features | PR obligatoire | `feature/*`, `fix/*`, `hotfix/*` |
| `feature/*` | Dev quotidien | Libre | Créée depuis `dev` obligatoirement |
| `fix/*` | Bugfix non urgent | Libre | Créée depuis `dev` obligatoirement |
| `hotfix/*` | Bugfix critique prod | Libre | Créée depuis `dev`, promotion rapide |

---

## Workflow 1 — CI sur Pull Request

**Fichier** : `.github/workflows/ci.yml`

```yaml
name: CI

on:
  pull_request:
    branches: [dev, staging, main]
  push:
    branches: [dev]

env:
  DOTNET_VERSION: '8.0.x'
  SOLUTION: 'src/Corex.sln'

jobs:
  build-and-test:
    name: Build & Test
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Setup MSBuild
        uses: microsoft/setup-msbuild@v2

      - name: Restore NuGet
        run: dotnet restore ${{ env.SOLUTION }}

      - name: Build Debug
        run: dotnet build ${{ env.SOLUTION }} --configuration Debug --no-restore

      - name: Run Unit Tests
        run: |
          dotnet test src/Corex.Tests/Corex.Tests.csproj \
            --configuration Debug \
            --no-build \
            --filter "Category=Unit" \
            --logger "trx;LogFileName=unit-results.trx" \
            --collect:"XPlat Code Coverage"

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: '**/*.trx'

      - name: Upload Coverage
        uses: actions/upload-artifact@v4
        with:
          name: coverage
          path: '**/coverage.cobertura.xml'

  static-analysis:
    name: Static Analysis
    runs-on: windows-latest
    needs: build-and-test

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Run Roslyn Analyzers
        run: |
          dotnet build ${{ env.SOLUTION }} \
            --configuration Debug \
            /p:TreatWarningsAsErrors=true \
            /p:EnableNETAnalyzers=true \
            /p:AnalysisLevel=latest

      - name: Check formatting
        run: dotnet format ${{ env.SOLUTION }} --verify-no-changes --severity warn
```

---

## Workflow 2 — Release (merge main)

**Fichier** : `.github/workflows/release.yml`

```yaml
name: Release

on:
  push:
    branches: [main]
    tags:
      - 'v*.*.*'

env:
  DOTNET_VERSION: '8.0.x'
  SOLUTION: 'src/Corex.sln'

jobs:
  build-release:
    name: Build Release
    runs-on: windows-latest
    environment: production

    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Get version from tag
        id: version
        run: echo "VERSION=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT
        shell: bash

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Setup MSBuild
        uses: microsoft/setup-msbuild@v2

      - name: Restore
        run: dotnet restore ${{ env.SOLUTION }}

      - name: Build Release
        run: |
          dotnet publish src/Corex.App/Corex.App.csproj \
            --configuration Release \
            --runtime win-x64 \
            --self-contained false \
            /p:Version=${{ steps.version.outputs.VERSION }} \
            /p:PublishSingleFile=false \
            --output ./publish

      - name: Build Native DLL (C++)
        run: |
          msbuild src/Corex.Native/Corex.Native.vcxproj \
            /p:Configuration=Release \
            /p:Platform=x64

      # Signature EV — certificat stocké dans GitHub Secrets
      - name: Sign executables
        env:
          EV_CERT_BASE64: ${{ secrets.EV_CERT_BASE64 }}
          EV_CERT_PASSWORD: ${{ secrets.EV_CERT_PASSWORD }}
          TIMESTAMP_URL: 'http://timestamp.sectigo.com'
        run: |
          $certBytes = [Convert]::FromBase64String($env:EV_CERT_BASE64)
          $certPath = "ev_cert.pfx"
          [IO.File]::WriteAllBytes($certPath, $certBytes)

          $filesToSign = Get-ChildItem ./publish -Include "*.exe","*.dll" -Recurse
          foreach ($file in $filesToSign) {
            & "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe" sign `
              /fd SHA256 `
              /td SHA256 `
              /tr $env:TIMESTAMP_URL `
              /f $certPath `
              /p $env:EV_CERT_PASSWORD `
              $file.FullName
          }
          Remove-Item $certPath
        shell: powershell

      - name: Verify signatures
        run: |
          Get-ChildItem ./publish -Include "*.exe","*.dll" -Recurse | ForEach-Object {
            $sig = Get-AuthenticodeSignature $_.FullName
            if ($sig.Status -ne "Valid") {
              Write-Error "Signature invalide : $($_.FullName)"
              exit 1
            }
          }
        shell: powershell

      - name: Build Installer (Inno Setup)
        run: |
          & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
            /DAppVersion=${{ steps.version.outputs.VERSION }} `
            installer/corex_setup.iss
        shell: powershell

      - name: Sign Installer
        env:
          EV_CERT_BASE64: ${{ secrets.EV_CERT_BASE64 }}
          EV_CERT_PASSWORD: ${{ secrets.EV_CERT_PASSWORD }}
        run: |
          # Même process de signature sur le .exe installeur
          $certBytes = [Convert]::FromBase64String($env:EV_CERT_BASE64)
          $certPath = "ev_cert.pfx"
          [IO.File]::WriteAllBytes($certPath, $certBytes)
          & "signtool.exe" sign /fd SHA256 /td SHA256 /tr http://timestamp.sectigo.com /f $certPath /p $env:EV_CERT_PASSWORD "installer/Output/CorexSetup-${{ steps.version.outputs.VERSION }}.exe"
          Remove-Item $certPath
        shell: powershell

      - name: Run Integration Tests on build
        run: |
          dotnet test src/Corex.Tests/Corex.Tests.csproj \
            --configuration Release \
            --filter "Category=Integration" \
            --logger "trx;LogFileName=integration-results.trx"

      - name: Compute SHA256
        id: sha256
        run: |
          $hash = (Get-FileHash "installer/Output/CorexSetup-${{ steps.version.outputs.VERSION }}.exe" -Algorithm SHA256).Hash
          echo "HASH=$hash" >> $env:GITHUB_OUTPUT
        shell: powershell

      - name: Upload installer artifact
        uses: actions/upload-artifact@v4
        with:
          name: corex-installer-${{ steps.version.outputs.VERSION }}
          path: installer/Output/*.exe

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: installer/Output/*.exe
          body: |
            ## Corex v${{ steps.version.outputs.VERSION }}

            **SHA256** : `${{ steps.sha256.outputs.HASH }}`

            ### Installation
            Télécharger `CorexSetup-${{ steps.version.outputs.VERSION }}.exe` et exécuter en tant qu'administrateur.

            ### Changelog
            Voir [CHANGELOG.md](CHANGELOG.md)
          draft: false
          prerelease: ${{ contains(github.ref, '-beta') || contains(github.ref, '-rc') }}

  notify-backend:
    name: Notify Backend (new version available)
    runs-on: ubuntu-latest
    needs: build-release
    environment: production

    steps:
      - name: Trigger backend update endpoint
        env:
          BACKEND_WEBHOOK: ${{ secrets.BACKEND_RELEASE_WEBHOOK }}
        run: |
          curl -X POST "$BACKEND_WEBHOOK" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer ${{ secrets.BACKEND_API_KEY }}" \
            -d '{
              "version": "${{ steps.version.outputs.VERSION }}",
              "channel": "stable",
              "sha256": "${{ steps.sha256.outputs.HASH }}"
            }'
```

---

## Workflow 3 — Nightly Build (bêta)

**Fichier** : `.github/workflows/nightly.yml`

```yaml
name: Nightly

on:
  schedule:
    - cron: '0 2 * * *'  # 2h du matin chaque jour
  workflow_dispatch:       # Déclenchement manuel possible

jobs:
  nightly-build:
    runs-on: windows-latest
    if: github.ref == 'refs/heads/dev'

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Build nightly
        run: |
          dotnet build src/Corex.sln \
            --configuration Release \
            /p:Version=0.0.0-nightly.$(date +%Y%m%d)

      - name: Run all tests
        run: dotnet test src/Corex.Tests/Corex.Tests.csproj --configuration Release

      - name: Upload nightly artifact
        uses: actions/upload-artifact@v4
        with:
          name: corex-nightly-${{ github.run_number }}
          path: '**/publish/**'
          retention-days: 7
```

---

## GitHub Environments

| Environment | Secret requis | Utilisé par |
|------------|--------------|-------------|
| `production` | `EV_CERT_BASE64`, `EV_CERT_PASSWORD`, `BACKEND_RELEASE_WEBHOOK`, `BACKEND_API_KEY` | workflow release |
| `staging` | `STAGING_BACKEND_URL` | tests intégration |

---

## Versioning — SemVer strict

```
v{MAJOR}.{MINOR}.{PATCH}[-{pre}]

v1.0.0        → Release stable
v1.0.1        → Bugfix
v1.1.0        → Nouvelle feature rétrocompatible
v2.0.0        → Breaking change (rare)
v1.1.0-beta.1 → Bêta publique
v1.1.0-rc.1   → Release candidate
```

**Convention de tag** : toujours depuis `main`, jamais depuis `develop` directement.

```bash
git checkout main
git merge develop
git tag v1.0.0
git push origin v1.0.0
```

---

## Checklist pre-release manuelle

Avant de tagger une release :
- [ ] Tous les tests unitaires passent sur `main`
- [ ] Tests manuels sur Windows 10 22H2 VM
- [ ] Tests manuels sur Windows 11 24H2 VM
- [ ] Snapshot + restauration validés end-to-end
- [ ] Zéro alerte Defender sur l'installeur signé
- [ ] `CHANGELOG.md` à jour
- [ ] Version bumped dans `.csproj`
- [ ] Soumission AV labs si changement du binaire principal
