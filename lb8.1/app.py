import aiohttp, asyncio
from aiohttp import web
from datetime import datetime

import aiohttp, asyncio
from aiohttp import web
from datetime import datetime

async def get_all():
    timeout = aiohttp.ClientTimeout(total=5)
    connector = aiohttp.TCPConnector(ssl=False)

    async with aiohttp.ClientSession(timeout=timeout, connector=connector) as s:

        async def one(url):
            try:
                resp = await s.get(url)
                if resp.status != 200:
                    return None, f"HTTP {resp.status}"
                data = await resp.json()
                return data, None
            except asyncio.TimeoutError:
                return None, "таймаут"
            except Exception as e:
                return None, f"{type(e).__name__}: {e}"

        cat_data,   cat_err   = await one("https://catfact.ninja/fact")
        joke_data,  joke_err  = await one("https://official-joke-api.appspot.com/random_joke")
        quote_data, quote_err = await one("https://api.quotable.io/random")

    now = datetime.now().strftime("%H:%M:%S")

    if cat_err:
        cat_txt = cat_err
    else:
        cat_txt = cat_data.get("fact", "нет факта")

    if joke_err:
        joke_txt = joke_err
    else:
        joke_txt = f"{joke_data.get('setup','...')} - {joke_data.get('punchline','...')}"

    if quote_err:
        quote_txt = "нет цитаты"
    else:
        text = quote_data.get("content", "нет текста")
        author = quote_data.get("author", "неизвестный автор")
        quote_txt = f"\"{text}\" — {author}"

    return {
        "cat":   cat_txt,
        "joke":  joke_txt,
        "quote": quote_txt,
        "time":  now,
    }


async def api_data(request):
    data = await get_all()
    return web.json_response(data)

async def index(request):
    return web.FileResponse("index.html")

def create_app():
    app = web.Application()
    app.router.add_get("/", index)
    app.router.add_get("/api/data", api_data)
    return app

if __name__ == "__main__":
    asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())

    try:
        app = create_app()
        web.run_app(app, port=8080)
    except KeyboardInterrupt:
        print("Сервер остановлен.")
