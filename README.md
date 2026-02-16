# ComVision

Projeto simples para detecção de xícaras/copos em tempo real usando YOLOv8 e webcam.

## Preparar ambiente virtual (venv)

1. **Criar ambiente virtual** (na pasta do projeto):

```bash
python -3.10 -m venv .venv
```

2. **Ativar o ambiente virtual**:

- **Windows (PowerShell)**:
  ```bash
  .venv\Scripts\Activate.ps1
  ```
- **Windows (Prompt de Comando)**:
  ```bash
  .venv\Scripts\activate
  ```

3. **Atualizar `pip` (recomendado)**:

```bash
python -m pip install --upgrade pip
```

4. **Instalar dependências necessárias**:

```bash
pip install ultralytics opencv-python
```

## Como rodar o aplicativo

Com o ambiente virtual **ativado** e as dependências instaladas:

```bash
python cup_detection.py
```

- Certifique-se de que o arquivo de modelo `yolov8n.pt` está presente na pasta do projeto (ou será baixado automaticamente pela primeira vez).
- Uma janela será aberta com o vídeo da webcam e as detecções de xícaras/copos.
- Pressione a tecla **`q`** para encerrar o programa.
