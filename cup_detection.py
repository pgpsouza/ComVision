import os
import time
from datetime import datetime

import cv2
from ultralytics import YOLO

# 1. Carrega o modelo pré-treinado
# O 'yolov8n.pt' é a versão Nano (mais leve e rápida).
# Ele será baixado automaticamente na primeira execução.
model = YOLO("yolov8n.pt")

# 2. Inicia a captura de vídeo (0 geralmente é a webcam integrada)
cap = cv2.VideoCapture(0)

# Verifica se a câmera abriu
if not cap.isOpened():
    print("Erro ao acessar a webcam.")
    exit()

print("Pressione 'q' para sair.")

# Controle de captura de fotos
photos_taken = 0
last_capture_time = 0.0

while True:
    ret, frame = cap.read()
    if not ret:
        break

    # 3. Realiza a detecção no frame atual
    # classes=[41] -> O ID 41 no dataset COCO corresponde a "Cup" (Xícara/Copo)
    # conf=0.5 -> Só mostra se tiver mais de 50% de certeza
    results = model.predict(frame, conf=0.5, classes=[41], verbose=False)

    # Verifica se há pelo menos uma caneca detectada
    has_cup = len(results[0].boxes) > 0

    # Se uma caneca estiver sendo detectada, tirar até 3 fotos
    if has_cup:
        current_time = time.time()

        # Tira uma foto a cada 2 segundos, até 3 fotos
        if photos_taken < 3 and current_time - last_capture_time >= 2.0:
            # Monta o caminho da pasta: Documentos/CupDetection/dd.mm.aa
            documents_dir = os.path.join(os.path.expanduser("~"), "Documents")
            base_dir = os.path.join(documents_dir, "CupDetection")
            date_folder = datetime.now().strftime("%d.%m.%y")
            save_dir = os.path.join(base_dir, date_folder)

            # Cria as pastas se ainda não existirem
            os.makedirs(save_dir, exist_ok=True)

            # Nome do arquivo (com horário para evitar sobrescrita)
            timestamp_str = datetime.now().strftime("%H-%M-%S")
            filename = f"cup_{photos_taken + 1}_{timestamp_str}.jpg"
            filepath = os.path.join(save_dir, filename)

            # Salva a imagem atual
            cv2.imwrite(filepath, frame)
            print(f"Foto salva em: {filepath}")

            photos_taken += 1
            last_capture_time = current_time
    else:
        # Se parar de detectar caneca, reseta o contador para uma nova sequência
        photos_taken = 0
        last_capture_time = 0.0

    # 4. Desenha os resultados no frame (quadrados e nomes)
    annotated_frame = results[0].plot()

    # Mostra a janela com o vídeo
    cv2.imshow("Detector de Xicaras - YOLOv8", annotated_frame)

    # Sai do loop se apertar 'q'
    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

# Libera a câmera e fecha janelas
cap.release()
cv2.destroyAllWindows()