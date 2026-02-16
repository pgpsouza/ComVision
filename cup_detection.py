import cv2
from ultralytics import YOLO

# 1. Carrega o modelo pré-treinado
# O 'yolov8n.pt' é a versão Nano (mais leve e rápida).
# Ele será baixado automaticamente na primeira execução.
model = YOLO('yolov8n.pt')

# 2. Inicia a captura de vídeo (0 geralmente é a webcam integrada)
cap = cv2.VideoCapture(0)

# Verifica se a câmera abriu
if not cap.isOpened():
    print("Erro ao acessar a webcam.")
    exit()

print("Pressione 'q' para sair.")

while True:
    ret, frame = cap.read()
    if not ret:
        break

    # 3. Realiza a detecção no frame atual
    # classes=[41] -> O ID 41 no dataset COCO corresponde a "Cup" (Xícara/Copo)
    # conf=0.5 -> Só mostra se tiver mais de 50% de certeza
    results = model.predict(frame, conf=0.5, classes=[41], verbose=False)

    # 4. Desenha os resultados no frame (quadrados e nomes)
    annotated_frame = results[0].plot()

    # Mostra a janela com o vídeo
    cv2.imshow("Detector de Xicaras - YOLOv8", annotated_frame)

    # Sai do loop se apertar 'q'
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# Libera a câmera e fecha janelas
cap.release()
cv2.destroyAllWindows()