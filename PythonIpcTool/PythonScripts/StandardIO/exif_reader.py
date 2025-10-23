# File: PythonScripts/exif_reader.py
import sys
import json
from PIL import Image
from PIL.ExifTags import TAGS

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        image_path = input_data.get("image_path")
        if not image_path:
            raise ValueError("Missing 'image_path' in input.")
            
        exif_data = {}
        with Image.open(image_path) as img:
            exif_info = img._getexif()
            if exif_info:
                for tag_id, value in exif_info.items():
                    tag = TAGS.get(tag_id, tag_id)
                    # Some values are bytes, decode them if possible
                    if isinstance(value, bytes):
                        try:
                            value = value.decode(errors='ignore')
                        except:
                            pass
                    exif_data[tag] = str(value)

        response = {"status": "success", "exif_data": exif_data}
        sys.stdout.write(json.dumps(response, indent=2) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()