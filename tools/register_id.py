#!/usr/bin/env python3
"""
FM26 ID Registry - Registre IDs de clubes e competições brasileiras
Execute: python3 register_id.py --help
"""

import json
import argparse
from pathlib import Path
from datetime import datetime

REGISTRY_PATH = Path(__file__).parent.parent / "id-registry.json"

def load_registry():
    """Carrega o registro de IDs"""
    if REGISTRY_PATH.exists():
        with open(REGISTRY_PATH, 'r', encoding='utf-8') as f:
            return json.load(f)
    return {
        "competitions": {},
        "clubs": {},
        "players": {},
        "last_updated": None
    }

def save_registry(registry):
    """Salva o registro"""
    registry["last_updated"] = datetime.now().isoformat()
    with open(REGISTRY_PATH, 'w', encoding='utf-8') as f:
        json.dump(registry, f, indent=2, ensure_ascii=False)
    print(f"✅ Registro salvo em {REGISTRY_PATH}")

def add_competition(registry, id_num, name_game, name_real, country="Brasil"):
    """Adiciona uma competição"""
    registry["competitions"][str(id_num)] = {
        "name_game": name_game,
        "name_real": name_real,
        "country": country
    }
    print(f"✅ Competição adicionada: {id_num} - {name_game} ({name_real})")

def add_club(registry, id_num, name_game, name_real, city, state, division=""):
    """Adiciona um clube"""
    registry["clubs"][str(id_num)] = {
        "name_game": name_game,
        "name_real": name_real,
        "city": city,
        "state": state,
        "division": division
    }
    print(f"✅ Clube adicionado: {id_num} - {name_game} ({name_real})")

def add_player(registry, id_num, name, club_id, nationality="Brasil"):
    """Adiciona um jogador"""
    registry["players"][str(id_num)] = {
        "name": name,
        "club_id": club_id,
        "nationality": nationality
    }
    print(f"✅ Jogador adicionado: {id_num} - {name}")

def show_registry(registry):
    """Mostra o registro atual"""
    print("\n" + "="*50)
    print("📋 REGISTRO DE IDs FM26 BRASIL")
    print("="*50)
    
    print(f"\n🏆 Competições: {len(registry['competitions'])}")
    for id_num, data in registry['competitions'].items():
        print(f"  {id_num}: {data['name_game']} → {data['name_real']}")
    
    print(f"\n⚽ Clubes: {len(registry['clubs'])}")
    for id_num, data in registry['clubs'].items():
        print(f"  {id_num}: {data['name_game']} → {data['name_real']} ({data['city']}-{data['state']})")
    
    print(f"\n🏃 Jogadores: {len(registry['players'])}")
    for id_num, data in list(registry['players'].items())[:10]:
        print(f"  {id_num}: {data['name']}")
    
    print(f"\n📅 Última atualização: {registry.get('last_updated', 'N/A')}")

def main():
    parser = argparse.ArgumentParser(description="Registrar IDs do FM26")
    subparsers = parser.add_subparsers(dest="command", help="Comandos")
    
    # Comando: competition
    comp_parser = subparsers.add_parser("comp", help="Adicionar competição")
    comp_parser.add_argument("id", type=int, help="ID da competição")
    comp_parser.add_argument("name_game", help="Nome no jogo (ex: Série A)")
    comp_parser.add_argument("name_real", help="Nome real (ex: Brasileirão)")
    comp_parser.add_argument("--country", default="Brasil", help="País")
    
    # Comando: club
    club_parser = subparsers.add_parser("club", help="Adicionar clube")
    club_parser.add_argument("id", type=int, help="ID do clube")
    club_parser.add_argument("name_game", help="Nome no jogo (ex: COR)")
    club_parser.add_argument("name_real", help="Nome real (ex: Corinthians)")
    club_parser.add_argument("city", help="Cidade")
    club_parser.add_argument("state", help="Estado (sigla)")
    club_parser.add_argument("--division", default="", help="Divisão")
    
    # Comando: player
    player_parser = subparsers.add_parser("player", help="Adicionar jogador")
    player_parser.add_argument("id", type=int, help="ID do jogador")
    player_parser.add_argument("name", help="Nome do jogador")
    player_parser.add_argument("club_id", type=int, help="ID do clube")
    player_parser.add_argument("--nationality", default="Brasil", help="Nacionalidade")
    
    # Comando: show
    subparsers.add_parser("show", help="Mostrar registro")
    
    args = parser.parse_args()
    registry = load_registry()
    
    if args.command == "comp":
        add_competition(registry, args.id, args.name_game, args.name_real, args.country)
        save_registry(registry)
    elif args.command == "club":
        add_club(registry, args.id, args.name_game, args.name_real, args.city, args.state, args.division)
        save_registry(registry)
    elif args.command == "player":
        add_player(registry, args.id, args.name, args.club_id, args.nationality)
        save_registry(registry)
    elif args.command == "show" or args.command is None:
        show_registry(registry)
    else:
        parser.print_help()

if __name__ == "__main__":
    main()
