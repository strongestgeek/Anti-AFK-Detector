#!/usr/bin/env python3

import json
import time
from datetime import datetime
from collections import deque
from pynput import mouse
import numpy as np
import signal
import sys

class MouseActivityTracker:
    def __init__(self):
        self.movements = deque(maxlen=1000)  # Store last 1000 movements
        self.last_position = None
        self.start_time = time.time()
        self.running = True
        self.suspicious_patterns = []
        
        # Configuration
        self.PERIODIC_THRESHOLD = 30.0  # Time in seconds to check for periodic movements
        self.PERIODIC_TOLERANCE = 0.5    # Tolerance in seconds for periodic movements
        self.CONTINUOUS_MOVEMENT_THRESHOLD = 300  # 5 minutes of continuous movement
        self.MIN_MOVEMENT_DISTANCE = 5    # Minimum pixels to consider as movement
        
        # Set up signal handler for graceful shutdown
        signal.signal(signal.SIGINT, self.signal_handler)

    def signal_handler(self, signum, frame):
        print("\nShutting down and saving results...")
        self.running = False
        self.save_results()
        sys.exit(0)

    def on_move(self, x, y):
        if not self.running:
            return False
        
        current_time = time.time()
        
        if self.last_position:
            distance = np.sqrt((x - self.last_position[0])**2 + (y - self.last_position[1])**2)
            if distance >= self.MIN_MOVEMENT_DISTANCE:
                self.movements.append({
                    'x': x,
                    'y': y,
                    'timestamp': current_time,
                    'distance': distance
                })
                self.analyze_movements()
        
        self.last_position = (x, y)
        return True

    def analyze_movements(self):
        if len(self.movements) < 3:
            return

        self.check_periodic_movements()
        self.check_continuous_movement()

    def check_periodic_movements(self):
        if len(self.movements) < 3:
            return

        # Get time intervals between movements
        intervals = []
        for i in range(1, len(self.movements)):
            interval = self.movements[i]['timestamp'] - self.movements[i-1]['timestamp']
            intervals.append(interval)

        # Check for regular intervals around our threshold
        for i in range(len(intervals)-2):
            three_intervals = intervals[i:i+3]
            if all(abs(interval - self.PERIODIC_THRESHOLD) < self.PERIODIC_TOLERANCE 
                  for interval in three_intervals):
                self.log_suspicious_pattern(
                    "Periodic Movement",
                    f"Detected regular movement every {self.PERIODIC_THRESHOLD} seconds (±{self.PERIODIC_TOLERANCE}s)",
                    three_intervals
                )

    def check_continuous_movement(self):
        if len(self.movements) < 2:
            return

        recent_movements = list(self.movements)[-20:]  # Look at last 20 movements
        total_time = recent_movements[-1]['timestamp'] - recent_movements[0]['timestamp']
        
        if total_time >= self.CONTINUOUS_MOVEMENT_THRESHOLD:
            total_distance = sum(m['distance'] for m in recent_movements)
            if total_distance > 0:  # If there's been any movement
                self.log_suspicious_pattern(
                    "Continuous Movement",
                    f"Detected continuous movement for {total_time:.1f} seconds",
                    {"total_distance": total_distance, "duration": total_time}
                )

    def log_suspicious_pattern(self, pattern_type, description, data):
        timestamp = datetime.now().isoformat()
        self.suspicious_patterns.append({
            'timestamp': timestamp,
            'type': pattern_type,
            'description': description,
            'data': data
        })
        print(f"[{{timestamp}}] Suspicious pattern detected: {{description}}")

    def save_results(self):
        with open('suspicious_patterns.json', 'w') as f:
            json.dump({
                'patterns': self.suspicious_patterns,
                'total_runtime': time.time() - self.start_time
            }, f, indent=2)
        print(f"Results saved to suspicious_patterns.json")


def main():
    print("Starting Mouse Activity Tracker...")
    print("Press Ctrl+C to stop and save results")
    
    tracker = MouseActivityTracker()
    with mouse.Listener(on_move=tracker.on_move) as listener:
        listener.join()

if __name__ == "__main__":
    main()