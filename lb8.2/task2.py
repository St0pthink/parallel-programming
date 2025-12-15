import asyncio, aiohttp, aiofiles, re, os
from urllib.parse import urljoin, urlparse

start = "https://docs.python.org/3/"
folder = "python_docs" 
cnt = 0

LIMIT_D1 = 20  # Ограничение на число загружаемых ссылко на глубине 1
d1_cnt = 0

async def parse_func(session, q, sem, s):
    global cnt, d1_cnt
    
    while True:

        item = await q.get()
        
        try:
            u = item[0]
            d = item[1]

            async with sem:
                print('Обработка: ' + u + ' (Глубина ' + str(d) + ')')
                await asyncio.sleep(1) 

                async with session.get(u, ssl=False) as resp:
                    if resp.status == 200:
                        txt = await resp.text()

                        cnt = cnt + 1
                        name = "page_" + str(cnt) + ".html"
                        
                        if not os.path.exists(folder):
                            os.makedirs(folder)

                        path = folder + "/" + name
                        async with aiofiles.open(path, "w", encoding="utf-8") as f:
                            await f.write(txt)

                        if d < 2:
                            found = re.findall(r'href=["\'](.*?)["\']', txt)
                            for l in found:
                                full = urljoin(u, l)
                                if urlparse(full).netloc == urlparse(start).netloc:
                                    if full not in s:

                                        next_d = d + 1
                                        
                                        add_flag = True

                                        if next_d == 1:
                                            if d1_cnt < LIMIT_D1:
                                                d1_cnt += 1
                                            else:
                                                add_flag = False

                                        if add_flag:
                                            s.add(full)
                                            await q.put((full, next_d))

                    else:
                        print(f"Error status: {resp.status} на {u}")
        except Exception as e:
            print(f"Ошибка: {e}")
        finally:
            q.task_done()

async def main():
    q = asyncio.Queue()
    s = set()
    sem = asyncio.Semaphore(3)

    q.put_nowait((start, 0))
    s.add(start)

    headers = {"User-Agent": "Mozilla/5.0"}

    async with aiohttp.ClientSession(headers=headers) as sess:
        tasks = []
        for i in range(3):
            t = asyncio.create_task(parse_func(sess, q, sem, s))
            tasks.append(t)

        await q.join()

        for t in tasks:
            t.cancel()

if __name__ == "__main__":
    asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
    asyncio.run(main())