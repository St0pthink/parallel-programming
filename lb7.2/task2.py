import pandas as pd
import re   
import string
import multiprocessing as mp
import numpy as np
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


def vectorize(text, uniq_list):
    vect = dict.fromkeys(uniq_list, 0)
    cleared_text = clean_text(text)
    for word in cleared_text:
        vect[word] += 1

    return tuple(vect.values())


def vectorization(texts, uniq):
    vectors = []
    uniq_list = sorted(uniq)

    with mp.Pool(mp.cpu_count()) as pool:
        vector_chunks = [(text, uniq_list) for text in texts]
        vectors = pool.starmap(vectorize, vector_chunks)
        
    return vectors


def find_dist(vect1,vect2):
    return sum((a - b) ** 2 for a, b in zip(vect1, vect2)) ** 0.5


def make_model(texts,uniq,k):
    rng = np.random.RandomState(1)
    ids = rng.permutation(len(texts))
    n = round(len(texts) * 0.8)
    train_ids = ids[:n]
    test_ids  = ids[n:]
    
    train_texts = texts.iloc[train_ids]
    test_texts  = texts.iloc[test_ids]

    X_train = vectorization(train_texts["text"],uniq)
    y_train = train_texts["label"]

    X_test = vectorization(test_texts["text"],uniq)
    y_test = test_texts["label"]

    model = {
    "X_train": X_train,
    "y_train": y_train,
    "k": k
    }
    return model,X_test,y_test


def consecutive_predict(model, x):
    X_train = model["X_train"]
    y_train = model["y_train"]
    k = model["k"]
    dists = []


    for x_train, y in zip(X_train, y_train):
        d = find_dist(x, x_train)
        dists.append((d, y))

    dists.sort(key=lambda x: x[0])
    ks = dists[:k]

    labels = [label for _, label in ks]
    if labels.count(1) >= labels.count(0):
        predict = 1
    else:
        predict = 0

    return predict


def choose_closest(chunk_X,chunk_y,k,x):
    chunk  = []

    for i in range(len(chunk_X)):
        d = find_dist(x, chunk_X[i])
        chunk.append((d, chunk_y[i]))

    chunk.sort(key=lambda t: t[0])
    return chunk[:k]


def parralel_predict(pool,model,x):
    num_processes = mp.cpu_count()
    X_train =  list(model["X_train"])
    y_train =  list(model["y_train"])
    k = model["k"]
    dists = []

    chunk_size = max(1, len(X_train) // num_processes)
    chunks_X = [X_train[i:i + chunk_size] for i in range(0, len(X_train), chunk_size)]
    chunks_y = [y_train[i:i + chunk_size] for i in range(0, len(y_train), chunk_size)]

    info = [(cx, cy, k, x) for cx, cy in zip(chunks_X, chunks_y)]
    
    dists_parts = pool.starmap(choose_closest, info)

    for items in dists_parts:
        for i in items:
            dists.append(i)

    dists.sort(key=lambda x: x[0])
    ks = dists[:k]

    labels = [label for _, label in ks]
    if labels.count(1) >= labels.count(0):
        predict = 1
    else:
        predict = 0

    return predict


def test(model,X_test,y_test):
    start = time.perf_counter()

    tp = 0
    fp = 0
    for i in range(len(X_test)):
        predict =  consecutive_predict(model,X_test[i])
        if predict == 1 and y_test.iloc[i] == 1:
            tp += 1
        elif predict == 1 and y_test.iloc[i] == 0:
            fp += 1
    print((tp/(fp+tp))*100)

    end = time.perf_counter()
    print(f"{end - start:.6f}")

    print()
    start = time.perf_counter()

    tp = 0
    fp = 0
    num_processes = mp.cpu_count()
    with mp.Pool(num_processes) as pool:
        y_pred = []
        for x in X_test:
            y_pred.append(parralel_predict(pool, model, x))

    for i in range(len(X_test)):
        if y_pred[i] == 1 and y_test.iloc[i] == 1:
            tp += 1
        elif y_pred[i] == 1 and y_test.iloc[i] == 0:
            fp += 1
    print((tp/(fp+tp))*100)

    end = time.perf_counter()
    print(f"{end - start:.6f}")


if __name__ == "__main__":
    texts = pd.read_csv("WELFake_Dataset.csv", nrows=300)
    good_texts = texts["text"].notna() & (texts["text"].str.strip() != "")
    texts = texts[good_texts]

    uniq = pararell_uniq(texts)

    k = 5 #менять для тестов k тут

    model,X_test,y_test = make_model(texts,uniq,k)
    test(model,X_test,y_test)