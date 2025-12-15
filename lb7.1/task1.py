import pandas as pd
import re   
import string
import multiprocessing as mp
import time
def clean_text(text):
    """Очистка и токенизация текста"""
    if pd.isna(text):
        return []
    
    # Приведение к нижнему регистру
    text = text.lower()
    
    # Удаление пунктуации и специальных символов
    text = re.sub(f'[{re.escape(string.punctuation)}]', '', text)
    
    # Разбиение на слова (учитываем только слова из букв)
    words = re.findall(r'\b[a-z]+\b', text)
    
    return words


def consecutive_uniq(texts):
    uniq = set()
    for text in texts["text"]:
        cleared_text = clean_text(text)
        for word in cleared_text:
            uniq.add(word)

    return uniq

def make_uniq_chunk(chunk):
    chunk_uniq = set()
    for text in chunk["text"]:
        cleared_text = clean_text(text)
        for word in cleared_text:
            chunk_uniq.add(word)
    return chunk_uniq

def pararell_uniq(texts):
    num_processes = mp.cpu_count()
    # Разбиваем DataFrame на чанки
    chunk_size = len(texts) // num_processes
    chunks = [texts[i:i + chunk_size] for i in range(0, len(texts), chunk_size)]
    with mp.Pool(num_processes) as pool:
        uniq_chunks = pool.map(make_uniq_chunk,chunks)
    uniq = set().union(*uniq_chunks)
    return uniq
    

if __name__ == "__main__":
    texts = pd.read_csv("WELFake_Dataset.csv")
    start = time.perf_counter()
    c_unqi = consecutive_uniq(texts)
    end = time.perf_counter()
    print(f"{end - start:.6f}")

    start = time.perf_counter()
    p_unqi = pararell_uniq(texts)
    end = time.perf_counter()
    print(f"{end - start:.6f}")