# File: PythonScripts/code_highlighter.py
import sys
import json
from pygments import highlight
from pygments.lexers import get_lexer_by_name
from pygments.formatters import HtmlFormatter

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        language = input_data.get("language", "text")
        code = input_data.get("code", "")
        
        lexer = get_lexer_by_name(language, stripall=True)
        formatter = HtmlFormatter(style='default', full=True, cssclass="highlight")
        
        html_output = highlight(code, lexer, formatter)
        
        # Extract CSS from the full HTML output
        css = formatter.get_style_defs('.highlight')

        response = {"status": "success", "html": html_output, "css": css}
        sys.stdout.write(json.dumps(response) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()