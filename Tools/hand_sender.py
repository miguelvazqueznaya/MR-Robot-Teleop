import os, cv2, json, socket, math, time
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision


MODEL = os.path.join(os.path.dirname(__file__), "hand_landmarker.task") 

#collect settings used to build detector
options = vision.HandLandmarkerOptions(
    base_options=mp_python.BaseOptions(model_asset_path=MODEL), #which model file to load
    running_mode=vision.RunningMode.VIDEO, #video running mode for synchronous sequential frames
    num_hands=1, #only look for one hand
    min_hand_detection_confidence=0.6, #how sure palm detector must be before saying hand is present
    min_hand_presence_confidence=0.6, #how sure landmark model must be that hand is still present
    min_tracking_confidence=0.6, 
)
landmarker = vision.HandLandmarker.create_from_options(options) #build the detector. 

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) #ipv4, create udp network scket
addr = ("127.0.0.1", 5052) #address (my laptop)
cap = cv2.VideoCapture(0)
t0 = time.time()

def d(a, b): return math.dist((a.x, a.y), (b.x, b.y))

while True:
    ok, frame = cap.read()
    if not ok: break
    rgb = cv2.cvtColor(cv2.flip(frame, 1), cv2.COLOR_BGR2RGB)
    image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
    result = landmarker.detect_for_video(image, int((time.time() - t0) * 1000))

    msg = {"tracked": False}
    if result.hand_landmarks:
        lm = result.hand_landmarks[0]
        size = d(lm[0], lm[9])               # wrist to middle knuckle = hand size
        pinch = d(lm[4], lm[8]) / size       # thumb tip to index tip, normalised
        msg = {"tracked": True, "x": lm[9].x, "y": lm[9].y,
               "size": size, "pinch": pinch}
    print(msg)
    sock.sendto(json.dumps(msg).encode(), addr)