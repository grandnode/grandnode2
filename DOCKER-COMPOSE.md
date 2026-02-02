# Docker Compose – GrandNode (school project)

Run GrandNode e-commerce with MongoDB using Docker Compose.

## What runs

| Service   | Port  | Role                          |
|----------|-------|--------------------------------|
| `mongodb`| 27017 | MongoDB 7 – database          |
| `grandnode` | 8080 | ASP.NET Core app – store UI & API |

- **Storefront:** http://localhost:8080  
- **Admin:** http://localhost:8080/admin (see README for demo credentials)

## Quick start

```bash
# Build and start (required: grandnode image is built locally, not pulled)
docker compose up -d --build

# View logs
docker compose logs -f grandnode
```

If you see "pull access denied for grandnode-school", you tried to pull instead of build. Use `--build` as above.

### ERR_CONNECTION_REFUSED on localhost:8080

1. **Check container status** — GrandNode may be crashing on startup:
   ```bash
   docker ps -a
   ```
   If `grandnode-web` is "Restarting" or "Exited", check logs (step 2).

2. **Check logs** for errors (e.g. missing config, MongoDB connection):
   ```bash
   docker compose logs grandnode --tail 100
   ```
   Look for "Now listening on" (success) or exception messages.

3. **App_Data volume** — If you previously had an `App_Data` volume mounted, it may have hidden `appsettings.json` and caused a crash. The current compose no longer mounts a new App_Data volume so the app can start; restart with:
   ```bash
   docker compose down && docker compose up -d --build
   ```

4. **Try explicit URL** — Use http://127.0.0.1:8080 in the browser (compose binds to 127.0.0.1:8080 on the host).

### "Value cannot be null. (Parameter 'databaseName')"

The MongoDB connection string must include a **database name**. Use a URL like `mongodb://mongodb:27017/grandnode` (not just `mongodb://mongodb:27017`). The compose file sets this via `ConnectionStrings__Mongodb`.

After MongoDB is healthy, GrandNode starts and listens on port 8080.

**After first-time installation:** GrandNode will ask you to restart. Run `docker compose restart grandnode`. The installer is disabled via env var so you won’t see the install screen again.

## Commands

```bash
# Stop
docker compose down

# Stop and remove volumes (reset DB and app data)
docker compose down -v

# Rebuild app after code changes
docker compose build grandnode && docker compose up -d grandnode
```

## Options

### Build from source (default)

The main `docker-compose.yml` builds GrandNode from the repo Dockerfile. Use this when you change code or need a custom build.

### Use official image (no build)

To run without building (faster start, official release):

```bash
copy docker-compose.override.example.yml docker-compose.override.yml
docker compose up -d
```

Override file swaps the built image for `grandnode/grandnode2:latest`.

## Data

- **MongoDB:** `mongodb_data` volume  
- **GrandNode images:** `grandnode_images` volume  
- **GrandNode App_Data:** Not mounted (using image defaults so the app can start). Settings/plugins in App_Data do not persist across container recreation.

Data in MongoDB and product images persists across `docker compose down`. Use `docker compose down -v` to wipe volumes.

## Troubleshooting

- **GrandNode exits or “cannot connect to MongoDB”**  
  Wait for MongoDB healthcheck to pass (about 10–15 s on first start), then restart:  
  `docker compose restart grandnode`

- **Port 8080 in use**  
  Change the host port in `docker-compose.yml`, e.g. `"8888:8080"`.

- **Need different MongoDB URL**  
  Set `ConnectionStrings__Mongodb` in the `grandnode` service `environment` section.
