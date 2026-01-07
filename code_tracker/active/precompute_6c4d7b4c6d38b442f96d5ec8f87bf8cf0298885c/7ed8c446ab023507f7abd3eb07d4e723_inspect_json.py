çimport json
from pathlib import Path

DATA_ROOT = Path(r'D:\fishinggame\precompute\data\1\1001')

def load_json(filename):
    with open(DATA_ROOT / filename, 'r', encoding='utf-8') as f:
        return json.load(f)

stock = load_json('fish_stock.json')
pond = load_json('fish_pond_list.json')
store = load_json('pond_store.json')

print("--- fish_stock.json sample ---")
first_stock = next(iter(stock.values()))
print(first_stock.keys())
print(first_stock)

print("\n--- fish_pond_list.json sample ---")
first_pond = next(iter(pond.values()))
print(first_pond.keys())
print(first_pond)

print("\n--- pond_store.json sample ---")
first_store = next(iter(store.values()))
print(first_store.keys())
print(first_store)
ç"(6c4d7b4c6d38b442f96d5ec8f87bf8cf0298885c21file:///d:/fishinggame/precompute/inspect_json.py:!file:///d:/fishinggame/precompute