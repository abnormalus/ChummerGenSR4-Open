#!/usr/bin/env python3
"""Run one verification stage with visible progress and bounded process lifetime."""
import argparse
import math
import os
import queue
import signal
import subprocess
import sys
import threading
import time


def positive(value):
    number = float(value)
    if not math.isfinite(number) or number <= 0:
        raise argparse.ArgumentTypeError("must be a finite positive number")
    return number


def stop_tree(process):
    # A timed-out build may leave compiler/test children alive. Own a separate
    # process group on POSIX; Windows taskkill provides tree termination.
    if os.name == "nt":
        try:
            subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"],
                           stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=5)
        except (OSError, subprocess.TimeoutExpired):
            pass
        if process.poll() is None:
            process.kill()
    else:
        try:
            os.killpg(process.pid, signal.SIGTERM)
            # Give normal cleanup a short opportunity, then also kill descendants
            # that outlive the parent or ignore termination.
            try:
                process.wait(timeout=2)
            except subprocess.TimeoutExpired:
                pass
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        print("[runner] Process cleanup did not complete; inspect remaining processes.", flush=True)


def run(command, label, timeout, idle_timeout, heartbeat):
    started = last_output = time.monotonic()
    next_status = started + heartbeat
    print(f"[{label}] START; deadline {timeout:g}s; idle limit {idle_timeout:g}s", flush=True)
    try:
        process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                   start_new_session=os.name != "nt")
    except OSError as error:
        print(f"[{label}] Could not start command: {type(error).__name__}", flush=True)
        return 127
    output = queue.Queue()

    def read_output():
        try:
            while data := os.read(process.stdout.fileno(), 4096):
                output.put(data)
        finally:
            output.put(None)

    # Read chunks rather than lines: a missing newline must not hide progress.
    threading.Thread(target=read_output, daemon=True).start()
    ended = False
    try:
        while not ended or process.poll() is None:
            now = time.monotonic()
            reason = "overall deadline" if now - started >= timeout else (
                "no output" if now - last_output >= idle_timeout else None)
            if reason:
                print(f"\n[{label}] TIMEOUT ({reason}); stopping process tree", flush=True)
                stop_tree(process)
                return 124
            if now >= next_status:
                print(f"\n[{label}] RUNNING {now-started:.0f}s; last output {now-last_output:.0f}s ago", flush=True)
                next_status = now + heartbeat
            try:
                data = output.get(timeout=0.1)
            except queue.Empty:
                continue
            if data is None:
                ended = True
            else:
                last_output = time.monotonic()
                sys.stdout.buffer.write(data)
                sys.stdout.buffer.flush()
        result = process.wait()
        print(f"\n[{label}] EXIT {result}; elapsed {time.monotonic()-started:.1f}s", flush=True)
        return result if result >= 0 else 128 - result
    except KeyboardInterrupt:
        stop_tree(process)
        return 130


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--label", required=True)
    parser.add_argument("--timeout", type=positive, required=True)
    parser.add_argument("--idle-timeout", type=positive, default=180)
    parser.add_argument("--heartbeat", type=positive, default=15)
    parser.add_argument("command", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    command = args.command[1:] if args.command[:1] == ["--"] else args.command
    if not command:
        parser.error("supply a command after --")
    return run(command, args.label, args.timeout, args.idle_timeout, args.heartbeat)


if __name__ == "__main__":
    sys.exit(main())
