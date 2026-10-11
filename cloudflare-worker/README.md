# Cloudflare Worker bridge

The Blazor WebAssembly app sends all external-data requests to this Worker:

- `GET /odds/{sportKey}` — The Odds API, using the `ODDS_API_KEY` Worker secret.
- `GET /standings?league={id}&season={year}` — API-Football, using the `API_FOOTBALL_KEY` Worker secret.
- `GET /stryktips/draws/{drawNumber}` — Svenska Spel's public draw API.

No provider API key is included in the browser app or committed to this repository. The existing public Worker URL remains in `wwwroot/appsettings.json` until you change it to your deployed Worker's URL.

## Prerequisites

1. Install Node.js LTS.
2. From this directory, install Wrangler: `npm install --save-dev wrangler`.
3. Sign in to Cloudflare: `npx wrangler login`.
4. Create a Cloudflare Worker named `football-proxy`, or change `name` in `wrangler.toml` to the name you want.

## Add your API keys as Worker secrets

Run these commands from `cloudflare-worker` and paste the keys when Wrangler prompts. Do not put the values in `wrangler.toml`, source files, Blazor appsettings, or browser code.

```powershell
npx wrangler secret put ODDS_API_KEY
npx wrangler secret put API_FOOTBALL_KEY
```

The `ALLOWED_ORIGINS` value in `wrangler.toml` is not a secret. Replace the sample localhost origins with the exact origins where your app runs, comma-separated. For example, include your GitHub Pages/custom domain as `https://yourname.github.io` (include the repository path only in the app URL, not in the origin). Keep localhost entries only if you develop locally.

## Deploy

From `cloudflare-worker`:

```powershell
npx wrangler deploy
```

Wrangler prints the deployed URL, normally `https://football-proxy.<your-account>.workers.dev/`.

## Configure the Blazor app

Update `Cloudflare:WorkerBaseUrl` in `wwwroot/appsettings.json` to the deployed URL, including a trailing slash:

```json
{
  "Cloudflare": {
	"WorkerBaseUrl": "https://football-proxy.<your-account>.workers.dev/"
  }
}
```

The value is public configuration and contains no secret. Rebuild and publish the Blazor app after changing it.

## Quick checks

Replace `<worker-url>` with your deployed Worker URL:

```powershell
Invoke-RestMethod "<worker-url>/odds/soccer_epl"
Invoke-RestMethod "<worker-url>/standings?league=39&season=2025"
Invoke-RestMethod "<worker-url>/stryktips/draws/4974"
```

The first two requests require valid Worker secrets and active provider plans/quotas. The odds route uses decimal H2H odds for the EU region. The standings route returns API-Football's upstream response shape. Svenska Spel returns its draw JSON. If an app request receives 403, add the page's exact `Origin` to `ALLOWED_ORIGINS` and deploy again.

## Protect usage

CORS limits which browser origins can read responses; it is not authentication and does not prevent direct requests to a public Worker URL. Configure Cloudflare rate limiting or WAF rules for this Worker to reduce abuse and protect provider quotas. Never rely on an origin allowlist as a way to keep the Worker endpoint private.

## API provider notes

The Odds API route uses `https://api.the-odds-api.com/v4` with EU region, H2H market, and decimal odds. The standings route uses API-Sports API-Football v3 and the `x-apisports-key` header. Confirm the corresponding API keys/plans with those providers; Cloudflare itself does not issue those provider keys.
