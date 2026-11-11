const jsonHeaders = { "content-type": "application/json; charset=utf-8" };

function getCorsHeaders(request, env) {
  const origin = request.headers.get("Origin");
  const allowedOrigins = (env.ALLOWED_ORIGINS ?? "")
    .split(",")
    .map((value) => value.trim())
    .filter(Boolean);

  const headers = new Headers({ Vary: "Origin" });
  if (origin && allowedOrigins.includes(origin)) {
    headers.set("Access-Control-Allow-Origin", origin);
    headers.set("Access-Control-Allow-Methods", "GET, OPTIONS");
    headers.set("Access-Control-Allow-Headers", "Content-Type");
    headers.set("Access-Control-Max-Age", "86400");
  }

  return { origin, allowedOrigins, headers };
}

function jsonResponse(request, env, body, status = 200) {
  const { headers } = getCorsHeaders(request, env);
  for (const [name, value] of Object.entries(jsonHeaders)) {
    headers.set(name, value);
  }
  headers.set("Cache-Control", "no-store");
  return new Response(JSON.stringify(body), { status, headers });
}

function upstreamResponse(request, env, upstream) {
  const { headers } = getCorsHeaders(request, env);
  headers.set("Content-Type", upstream.headers.get("Content-Type") ?? "application/json; charset=utf-8");
  headers.set("Cache-Control", "no-store");
  return new Response(upstream.body, { status: upstream.status, headers });
}

function isPositiveInteger(value) {
  return isDigits(value) && Number(value) > 0;
}

function isDigits(value) {
  return typeof value === "string"
    && value.length > 0
    && [...value].every((character) => character >= "0" && character <= "9");
}

function isSportKey(value) {
  return typeof value === "string"
    && value.length > 0
    && [...value].every((character) =>
      (character >= "a" && character <= "z")
      || (character >= "A" && character <= "Z")
      || (character >= "0" && character <= "9")
      || "._-".includes(character));
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const pathSegments = url.pathname.split("/").filter(Boolean);
    const { origin, allowedOrigins } = getCorsHeaders(request, env);

    if (origin && !allowedOrigins.includes(origin)) {
      return jsonResponse(request, env, { error: "Origin is not allowed." }, 403);
    }

    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: getCorsHeaders(request, env).headers });
    }

    if (request.method !== "GET") {
      return jsonResponse(request, env, { error: "Method not allowed." }, 405);
    }

    try {
      const oddsKey = pathSegments.length === 2 && pathSegments[0] === "odds" && isSportKey(pathSegments[1])
        ? pathSegments[1]
        : null;
      if (oddsKey) {
        if (!env.ODDS_API_KEY) {
          return jsonResponse(request, env, { error: "Worker secret ODDS_API_KEY is not configured." }, 503);
        }

        const upstreamUrl = new URL(`https://api.the-odds-api.com/v4/sports/${encodeURIComponent(oddsKey)}/odds/`);
        upstreamUrl.search = new URLSearchParams({
          regions: "eu",
          markets: "h2h",
          oddsFormat: "decimal",
          apiKey: env.ODDS_API_KEY
        }).toString();

        const upstream = await fetch(upstreamUrl, {
          headers: { Accept: "application/json" },
          cf: { cacheTtl: 60, cacheEverything: true }
        });
        return upstreamResponse(request, env, upstream);
      }

      if (url.pathname === "/standings") {
        if (!env.API_FOOTBALL_KEY) {
          return jsonResponse(request, env, { error: "Worker secret API_FOOTBALL_KEY is not configured." }, 503);
        }

        const league = url.searchParams.get("league");
        const season = url.searchParams.get("season");
        if (!isPositiveInteger(league) || !isDigits(season) || season.length !== 4) {
          return jsonResponse(request, env, { error: "Valid league and season query parameters are required." }, 400);
        }

        const upstreamUrl = new URL("https://v3.football.api-sports.io/standings");
        upstreamUrl.search = new URLSearchParams({ league, season }).toString();
        const upstream = await fetch(upstreamUrl, {
          headers: { Accept: "application/json", "x-apisports-key": env.API_FOOTBALL_KEY },
          cf: { cacheTtl: 300, cacheEverything: true }
        });
        return upstreamResponse(request, env, upstream);
      }

      if (url.pathname === "/nhl/standings") {
        const upstream = await fetch("https://api-web.nhle.com/v1/standings/now", {
          headers: { Accept: "application/json" },
          cf: { cacheTtl: 300, cacheEverything: true }
        });
        return upstreamResponse(request, env, upstream);
      }

      const drawNumber = pathSegments.length === 3
        && pathSegments[0] === "stryktips"
        && pathSegments[1] === "draws"
        && isPositiveInteger(pathSegments[2])
        ? pathSegments[2]
        : null;
      if (drawNumber) {
        const upstream = await fetch(
          `https://api.spela.svenskaspel.se/draw/1/stryktipset/draws/${drawNumber}`,
          { headers: { Accept: "application/json" }, cf: { cacheTtl: 30, cacheEverything: true } }
        );
        return upstreamResponse(request, env, upstream);
      }

      return jsonResponse(request, env, { error: "Not found." }, 404);
    } catch {
      return jsonResponse(request, env, { error: "The upstream request failed." }, 502);
    }
  }
};
