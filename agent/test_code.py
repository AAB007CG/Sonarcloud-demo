# test_review.py
def process_data(list_a, list_b):
    # This is inefficient and prone to index errors
    for i in range(len(list_a)):
        item_a = list_a[i]
        item_b = list_b[i]
        print(f"Index {i}: {item_a} - {item_b}")

# Hardcoded secret (Security Risk)
API_KEY = "12345-ABCDE"
process_data([1, 2], ["apple"]) # This will crash (lists are different lengths)