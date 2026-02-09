import socket
import struct

# Configuration
UDP_IP = "127.0.0.1"
UDP_PORT = 5555

# Setup Socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind((UDP_IP, UDP_PORT))

# Mappings to match C# logic
SHAPES = {
    0: "Unknown",
    1: "Circle",
    2: "Triangle",
    3: "Square",
    4: "Star",
    5: "FINISHER (Line)"
}

LANES = {
    0: "LEFT",
    1: "MIDDLE",
    2: "RIGHT",
    3: "NONE"
}

print(f"--- UGAHacks 11: Magic Spell Bridge ---")
print(f"Listening for binary packets on port {UDP_PORT}...")
print(f"Press Ctrl+C to stop.\n")

try:
    while True:
        data, addr = sock.recvfrom(1024)

        if not data:
            continue

        # Identify packet type by the first byte
        packet_type = data[0]

        if packet_type == 0:
            # --- TYPE 0: STREAM (16 Bytes) ---
            # < : Little Endian
            # B : Byte (Type - 1B)
            # h : Short (ID - 2B)
            # i : Int (Offset - 4B)
            # f : Float (X - 4B)
            # f : Float (Y - 4B)
            # ? : Bool (Drawing - 1B)
            try:
                fmt = '<B h i f f ?'
                unpacked = struct.unpack(fmt, data)
                _, stroke_id, offset, x, y, drawing = unpacked

                mode = "DRAWING" if drawing else "CURSOR"
                print(f"[{mode:7}] ID: {stroke_id:6} | Pkt: {offset:4} | X: {x:.3f} | Y: {y:.3f}")

            except Exception as e:
                print(f"Error unpacking Stream (Type 0): {e} | Data len: {len(data)}")

        elif packet_type == 1:
            # --- TYPE 1: ACTION (5 Bytes) ---
            # < : Little Endian
            # B : Byte (Type - 1B)
            # h : Short (ID - 2B)
            # B : Byte (Class ID - 1B)
            # B : Byte (Lane ID - 1B)
            try:
                fmt = '<B h B B'
                unpacked = struct.unpack(fmt, data)
                _, stroke_id, class_id, lane_id = unpacked

                shape_name = SHAPES.get(class_id, f"Unknown({class_id})")
                lane_name = LANES.get(lane_id, f"Unknown({lane_id})")

                print(f"\n" + "=" * 50)
                print(f"  SPELL DETECTED")
                print(f"  Stroke ID: {stroke_id}")
                print(f"  Shape:     {shape_name}")
                print(f"  Target:    Lane {lane_id} ({lane_name})")
                print("=" * 50 + "\n")

            except Exception as e:
                print(f"Error unpacking Action (Type 1): {e} | Data len: {len(data)}")

        else:
            print(f"Unknown packet type received: {packet_type}")

except KeyboardInterrupt:
    print("\nStopping listener...")
finally:
    sock.close()