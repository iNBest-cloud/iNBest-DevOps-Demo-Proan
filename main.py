import os
import sqlite3
from flask import Flask, request

app = Flask(__name__)

# --- Vulnerabilidad 1: SQL Injection ---
def get_user(username):
    conn = sqlite3.connect("test.db")
    cursor = conn.cursor()
    # Construcción insegura de query
    query = "SELECT * FROM users WHERE username = '" + username + "';"
    cursor.execute(query)  # vulnerable a inyección SQL
    return cursor.fetchall()

# --- Vulnerabilidad 2: Command Injection ---
@app.route("/ping", methods=["GET"])
def ping():
    ip = request.args.get("ip")
    # Ejecuta comandos del sistema directamente con input del usuario
    return os.popen("ping -c 1 " + ip).read()

# --- Vulnerabilidad 3: Hardcoded secrets ---
API_KEY = "123456789-SECRET-KEY"

# --- Vulnerabilidad 4: XSS (Cross-Site Scripting) ---
@app.route("/hello", methods=["GET"])
def hello():
    name = request.args.get("name", "world")
    # Renderiza input directamente sin escapar
    return f"<h1>Hello {name}</h1>"

if __name__ == "__main__":
    app.run(debug=True)
