# Hetzner CI/CD (GitHub Actions + GHCR + Docker Compose)

Dieser Ablauf ergänzt die bestehenden Workflows und nutzt:

- PR auf `master`: nur Build/Test (Backend + Frontend), kein Deploy.
- Push auf `master`: Build/Push Docker Images nach GHCR + SSH-Deploy auf Hetzner.
- `workflow_dispatch`: manuelles Build/Push/Deploy (API und/oder Client auswählbar).

## Benötigte GitHub Secrets

Setze die folgenden **Repository Secrets** in GitHub (`Settings -> Secrets and variables -> Actions`):

- `HETZNER_SSH_HOST` (z. B. `1.2.3.4`)
- `HETZNER_SSH_PORT` (z. B. `22`)
- `HETZNER_SSH_USER` (z. B. `deploy`)
- `HETZNER_SSH_PRIVATE_KEY` (private SSH Key für den Server-User)
- `HETZNER_SSH_KNOWN_HOSTS` (Output von `ssh-keyscan -H <host>`)
- `HETZNER_DEPLOY_PATH` (z. B. `/opt/speakstorelocate`)
- `GHCR_USERNAME` (GitHub User/Robot mit Pull-Rechten auf GHCR)
- `GHCR_TOKEN` (PAT mit mindestens `read:packages`)

## Erster Go-Live (einmalig auf Server)

1. Verzeichnisstruktur erstellen:

```bash
sudo mkdir -p /opt/speakstorelocate/compose
sudo chown -R <deploy-user>:<deploy-user> /opt/speakstorelocate
```

2. Compose-Datei aus dem Repo auf den Server kopieren:

```bash
scp deploy/compose/docker-compose.hetzner.yml <deploy-user>@<server>:/opt/speakstorelocate/compose/
```

3. `compose/.env` auf dem Server anlegen (wird von CI **nicht überschrieben**):

```bash
cat >/opt/speakstorelocate/compose/.env <<'EOF'
GHCR_OWNER=<github-owner-in-kleinbuchstaben>
API_BASE=https://<deine-domain>/api/storage

# Beispiel: API Runtime-Konfiguration
ASPNETCORE_ENVIRONMENT=Production
AWS__S3__BucketName=...
AWS__S3__AccessKey=...
AWS__S3__SecretKey=...
AWS__DynamoDB__TableName=...
AWS__DynamoDB__AccessKey=...
AWS__DynamoDB__SecretKey=...
AWS__Transcribe__AccessKey=...
AWS__Transcribe__SecretKey=...
Deepgram__ApiKey=...
ElevenLabs__ApiKey=...
OpenAI__ApiKey=...
OpenAI__DefaultModel=...
EOF
```

4. In GitHub den Workflow `.github/workflows/hetzner-cicd.yml` einmal per `workflow_dispatch` starten.

## Rollback

Rollback API auf vorherigen Tag:

```bash
cd /opt/speakstorelocate/compose
API_IMAGE_TAG=<vorheriger-sha-tag> docker compose --env-file .env -f docker-compose.hetzner.yml up -d api
```

Rollback Client auf vorherigen Tag:

```bash
cd /opt/speakstorelocate/compose
CLIENT_IMAGE_TAG=<vorheriger-sha-tag> docker compose --env-file .env -f docker-compose.hetzner.yml up -d client
```

## Troubleshooting

- SSH schlägt fehl:
  - `HETZNER_SSH_PRIVATE_KEY` passt nicht zum Server-User.
  - `HETZNER_SSH_KNOWN_HOSTS` mit `ssh-keyscan -H <host>` neu erzeugen.
  - Firewall erlaubt keinen Zugriff auf `HETZNER_SSH_PORT`.

- GHCR Auth schlägt fehl:
  - `GHCR_TOKEN` hat kein `read:packages`.
  - `GHCR_USERNAME`/`GHCR_TOKEN` testen mit:

    ```bash
    echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USERNAME" --password-stdin
    ```

- `manifest unknown` beim Pull:
  - Prüfen, ob Build-Job für API/Client in GitHub erfolgreich war.
  - Vorhandene Tags in GHCR prüfen (`latest`, `sha-<commit-sha>`).

- Compose nutzt falsche Werte:
  - Inhalt von `/opt/speakstorelocate/compose/.env` prüfen.
  - Sicherstellen, dass die Compose-Datei `--env-file .env` nutzt.