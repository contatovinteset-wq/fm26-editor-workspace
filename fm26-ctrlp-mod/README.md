# FM26 Ctrl+P Export Mod

## Objetivo
Reativar a funcionalidade de exportação (Ctrl+P) no Football Manager 2026.

## Como funciona
O mod usa BepInEx para hook no sistema de input do Unity e detecta quando Ctrl+P é pressionado.
Quando detectado, chama a função `UpdateExportCurrentItemBinding` no carousel/tabela ativo.

## Instalação
1. Instalar BepInEx-IL2CPP no FM26
2. Copiar o DLL do mod para `BepInEx/plugins/`

## Status
- [x] Análise do código descompilado
- [x] Identificação da função `ExportCurrentItemToBinding` em `SICarousel`
- [ ] Criação do mod BepInEx
- [ ] Teste no jogo

## Referências
- `SICarousel` classe (SI.Bindable namespace)
- `UpdateExportCurrentItemBinding(int index)` - método que exporta o item atual
- `ExportCurrentItemToBinding` - BindingPath para o destino da exportação
