# File: PythonScripts/pytest_runner.py
import sys, json, socket, pytest

class JsonTestReporter:
    def __init__(self, writer):
        self.writer = writer

    def pytest_runtest_logreport(self, report):
        if report.when == 'call': # Only report on the main test execution phase
            test_result = {
                "event": "test_finished",
                "name": report.nodeid,
                "status": report.outcome, # 'passed', 'failed', 'skipped'
                "duration": round(report.duration, 4)
            }
            if report.failed:
                test_result["error"] = str(report.longrepr)
            
            self.writer.write(json.dumps(test_result) + '\n')
            self.writer.flush()
            
    def pytest_sessionfinish(self, session):
        finish_msg = {"event": "session_finished", "exitstatus": int(session.exitstatus)}
        self.writer.write(json.dumps(finish_msg) + '\n')
        self.writer.flush()

def run_tests(writer, target):
    reporter = JsonTestReporter(writer)
    pytest.main([target], plugins=[reporter])

# ... (main template, on "run_tests" command call run_tests)