def insecure():
    username = "admin"
    password = "admin123" # password is exposed intentionally
    @123 # Intentional Syntax Error
    return eval("2 + 2")