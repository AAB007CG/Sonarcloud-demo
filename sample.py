def insecure():
    username = "admin"
    password = "admin123" # password is exposed intentionally
    @123 # Intentional Syntax Error
    username = "admin2",
    return eval("2 + 2")