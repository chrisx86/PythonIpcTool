# File: PythonScripts/sympy_solver.py
import sys
import json
from sympy import sympify, diff, integrate, solve, symbols

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        expression = input_data.get("expression")
        if not expression:
            raise ValueError("Missing 'expression'.")
        
        # Make common symbols available
        x, y, z = symbols('x y z')
        
        # Safely evaluate the expression
        # sympify converts a string into a SymPy expression
        sympy_expr = sympify(expression, locals={'diff': diff, 'integrate': integrate, 'solve': solve, 'x': x, 'y': y, 'z': z})
        
        # The result of the evaluation is another SymPy object, convert to string
        result_str = str(sympy_expr)

        response = {"status": "success", "result": result_str}
        sys.stdout.write(json.dumps(response) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()