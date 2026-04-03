import os

keywords_staff = ["Equipa Técnica", "Equipe Técnica", "Staff", "PersonListTable", "PersonList", "client_object_list_table", "staff_list"]
keywords_match = ["Estatísticas", "MatchStats", "Match Stats", "Posse de bola", "Resumo do Jogo", "Match Result", "main stats", "passing", "offensive", "defensive", "goalkeeping"]

out_lines = []
for i in range(1, 8):
    fname = f"dump_{i}.txt"
    if os.path.exists(fname):
        with open(fname, 'r', encoding='utf-8', errors='ignore') as f:
            lines = f.readlines()
            for l_idx, line in enumerate(lines):
                for k in keywords_staff:
                    if k.lower() in line.lower() and len(k) > 5:
                        out_lines.append(f"{fname}:{l_idx} (Staff) -> {line.strip()}\n")
                for k in keywords_match:
                    if k.lower() in line.lower() and len(k) > 5:
                        out_lines.append(f"{fname}:{l_idx} (Match) -> {line.strip()}\n")

with open('tmp_scan_out8.txt', 'w', encoding='utf-8') as f:
    f.writelines(out_lines)
