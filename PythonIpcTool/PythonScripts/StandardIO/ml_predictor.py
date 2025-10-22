# File: PythonScripts/ml_predictor.py
import sys
import json
import joblib
import numpy as np

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        model_path = input_data.get("model_path")
        features = input_data.get("features")

        if not model_path or not features:
            raise ValueError("Missing 'model_path' or 'features' in input JSON.")

        model = joblib.load(model_path)
        
        # Assuming features dict order matches model training order
        feature_values = np.array(list(features.values())).reshape(1, -1)
        
        prediction = model.predict(feature_values)
        
        # Convert numpy type to standard Python type for JSON serialization
        prediction_result = float(prediction[0])

        response = {"status": "success", "prediction": prediction_result}

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

    sys.stdout.write(json.dumps(response) + '\n')
    sys.stdout.flush()

if __name__ == "__main__":
    main()