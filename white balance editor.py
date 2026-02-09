import cv2

cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)

cap.set(cv2.CAP_PROP_SETTINGS, 1) 

while True:
    ret, frame = cap.read()
    if not ret: break
    cv2.imshow('Magic Wand Tracker', frame)
    if cv2.waitKey(1) & 0xFF == ord('q'): break

cap.release()
cv2.destroyAllWindows()