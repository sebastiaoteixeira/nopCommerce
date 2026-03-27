#!/usr/bin/env python3
import subprocess
import sys

result = subprocess.run(
    [sys.executable, "/home/sebastiao/Projects/uni/as/nopCommerce/diagrams/generate_component_diagram.py"],
    capture_output=True,
    text=True
)

print(result.stdout)
if result.stderr:
    print(result.stderr, file=sys.stderr)
sys.exit(result.returncode)
