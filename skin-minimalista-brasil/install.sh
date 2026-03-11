#!/bin/bash
# Script de instalação da Skin Minimalista Brasil para FM26
# Execute este script para instalar a skin

echo "=================================================="
echo "  Skin Minimalista Brasil - Instalador FM26"
echo "=================================================="
echo ""

# Detectar sistema operacional
if [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
    # Windows
    SKIN_DIR="/c/Users/$USER/Documents/Sports Interactive/Football Manager 2026/skins"
    FM26_ASSETS="/c/Program Files/Steam/steamapps/common/Football Manager 2026/data"
else
    # Linux/Mac
    SKIN_DIR="$HOME/.local/share/Sports Interactive/Football Manager 2026/skins"
    FM26_ASSETS="$HOME/.steam/steam/steamapps/common/Football Manager 2026/data"
fi

echo "Sistema detectado: $OSTYPE"
echo "Pasta de skins: $SKIN_DIR"
echo ""

# Criar pasta de skins se não existir
mkdir -p "$SKIN_DIR/MinimalistaBrasil"

# Copiar backgrounds
echo "Copiando backgrounds..."
cp -r backgrounds-convertidos/Texture2D "$SKIN_DIR/MinimalistaBrasil/"

echo ""
echo "✓ Instalação concluída!"
echo ""
echo "Para ativar a skin no FM26:"
echo "1. Abra o jogo"
echo "2. Vá em Preferências > Interface"
echo "3. Selecione 'MinimalistaBrasil' na lista de skins"
echo "4. Aplique e reinicie o jogo"
echo ""
echo "=================================================="
