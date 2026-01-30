# src/app.py
# Intentional vulnerabilities for CodeQL scanning (DO NOT USE IN PRODUCTION)

from flask import Flask, request, jsonify
import sqlite3
import subprocess
import yaml  # pip install pyyaml
from pathlib import Path

app = Flask(__name__)

# ❌ Hardcoded secret (bad practice; for scanner detection only)
STRIPE_SECRET_KEY = "sk_live_FAKE_HARDCODED_KEY"  # BAD

DB_PATH = "test.db"

def get_db():
    conn = sqlite3.connect(DB_PATH)
    return conn

# ❌ SQL Injection: unsafe string concatenation
@app.get("/user")
def get_user():
    user_id = request.args.get("id", "")
    query = f"SELECT id, username FROM users WHERE id = '{user_id}'"  # BAD

    conn = get_db()
    try:
        rows = conn.execute(query).fetchall()
        return jsonify({"rows": rows})
    finally:
        conn.close()

# ❌ Command Injection: user input in shell command with shell=True
@app.post("/run")
def run():
    cmd = request.json.get("cmd", "echo Hello")
    proc = subprocess.Popen(cmd, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)  # BAD
    out, err = proc.communicate()
    if proc.returncode != 0:
        return (err.decode("utf-8"), 500)
    return out.decode("utf-8")

# ❌ Path Traversal: reading user-supplied file path without validation
@app.get("/read")
def read_file():
    filename = request.args.get("file", "README.md")
    # BAD: attacker can request ../../etc/passwd, etc.
    try:
        with open(filename, "r", encoding="utf-8") as f:
            return f.read()
    except Exception as e:
        return (str(e), 404)

# ❌ Insecure deserialization: yaml.load with unsafe Loader
@app.post("/config")
def load_config():
    body = request.data.decode("utf-8")
    cfg = yaml.load(body, Loader=yaml.Loader)  # BAD: use SafeLoader in real code
    return jsonify(cfg)

if __name__ == "__main__":
    # Optional: tiny setup so endpoints can run locally if needed (not required for CodeQL)
    if not Path(DB_PATH).exists():
        conn = sqlite3.connect(DB_PATH)
        conn.execute("CREATE TABLE users (id TEXT, username TEXT)")
        conn.execute("INSERT INTO users VALUES ('1', 'alice'), ('2', 'bob')")
        conn.commit()
        conn.close()

    app.run(port=5000)