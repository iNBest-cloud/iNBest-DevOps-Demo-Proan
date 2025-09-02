import os
import sqlite3
from flask import Flask, request, escape
import subprocess
import secrets

app = Flask(__name__)

# --- Corrección 1: SQL Injection (usar consultas parametrizadas) ---
def get_user(username):
    conn = sqlite3.connect("test.db")
    cursor = conn.cursor()
    query = "SELECT * FROM users WHERE username = ?;"
    cursor.execute(query, (username,))  # consulta segura con parámetros
    return cursor.fetchall()

# --- Corrección 2: Command Injection (validar input y usar subprocess con lista) ---
@app.route("/ping", methods=["GET"])
def ping():
    ip = request.args.get("ip")
    # Validación básica para IPs (ejemplo)
    if not ip or not all(c.isdigit() or c == '.' for c in ip):
        return "Invalid IP", 400
    try:
        result = subprocess.run(["ping", "-c", "1", ip], capture_output=True, text=True, check=True)
        return result.stdout
    except subprocess.CalledProcessError:
        return "Ping failed", 500

# --- Corrección 3: No usar secretos hardcodeados ---
# Se recomienda cargar secretos desde variables de entorno o gestor de secretos
API_KEY = os.getenv("API_KEY", secrets.token_hex(16))

# --- Corrección 4: XSS (escapar input del usuario antes de renderizar) ---
@app.route("/hello", methods=["GET"])
def hello():
    name = request.args.get("name", "world")
    safe_name = escape(name)  # evita XSS
    return f"<h1>Hello {safe_name}</h1>"

if __name__ == "__main__":
    # Desactivar debug en producción
    app.run(debug=False)
