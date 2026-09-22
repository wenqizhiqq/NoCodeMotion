import ast
src=open('D:/wqz/code/NoCodeMotion/_hints.py','r',encoding='utf-8-sig').read()
print("first30:", repr(src[:30]))
try:
    d=ast.literal_eval(src)
    print("OK keys:", len(d))
except Exception as e:
    print("ERR:", e)
