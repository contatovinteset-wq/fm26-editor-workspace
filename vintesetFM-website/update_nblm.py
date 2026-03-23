import sys

with open('notebooklm_automator.py', 'r', encoding='utf-8') as f:
    lines = f.readlines()

start_idx = -1
end_idx = -1

for i, line in enumerate(lines):
    if '# NOVO FLUXO: Clicar no vídeo/áudio' in line:
        start_idx = i
        break

for i, line in enumerate(lines):
    if 'PENDENTE (Nenhum video/audio' in line:
        end_idx = i
        break

if start_idx == -1 or end_idx == -1:
    print('Tokens of search not found')
    sys.exit(1)

new_code = '''                # NOVO FLUXO DEFINITIVO: Busca exaustiva inteligente pelos botões "Play" e "3 pontinhos" (Kebab Menu)
                download_regex = re.compile("download|baixar|fazer|transferir|salvar", re.IGNORECASE)
                download_found = False
                target_btn = None
                
                # Coletar potenciais botões de mídia e menus
                candidate_locators = [
                    # Prioridade 1: Botões com aria-haspopup (padrão do Google para menus de 3 pontinhos)
                    'button[aria-haspopup="true"], [role="button"][aria-haspopup="true"], [aria-haspopup="menu"]',
                    # Prioridade 2: Aria-labels diretas de Play ou Opções
                    '[aria-label*="Mais" i], [aria-label*="Opções" i], [aria-label*="More" i]',
                    '[aria-label*="Play" i], [aria-label*="Reproduz" i], [aria-label*="Tocar" i]',
                    # Prioridade 3: Qualquer botão que contenha um SVG mas NÃO tenha texto nem imagem (ícones isolados)
                    'button:not(:has-text(/[a-zA-Z]/)):has(svg):not(:has(img)), [role="button"]:not(:has-text(/[a-zA-Z]/)):has(svg):not(:has(img))'
                ]
                
                for loc in candidate_locators:
                    candidates = page.locator(loc)
                    for i in range(candidates.count()):
                        btn = candidates.nth(i)
                        
                        # Ignorar foto de perfil (se tiver imagem) ou botões explícitos a pular
                        if btn.locator('img').count() > 0:
                            continue
                            
                        if btn.is_visible():
                            try:
                                btn.click(timeout=1000)
                                time.sleep(1.5) # Espera modal ou menu abrir
                            except: pass
                            
                            # Verifica se o botão de download mágico apareceu!
                            d_btns = page.get_by_role("button", name=download_regex)
                            if d_btns.count() == 0:
                                d_btns = page.get_by_text(download_regex)
                                
                            if d_btns.count() > 0:
                                # Pegar o visível
                                for j in range(d_btns.count()):
                                    if d_btns.nth(j).is_visible():
                                        target_btn = d_btns.nth(j)
                                        download_found = True
                                        break
                                        
                            if download_found:
                                break
                    if download_found:
                        break
                
                if download_found and target_btn:
                    print(f"-> Ouro encontrado! Iniciando interceptação de download...")
                    
                    try:
                        with page.expect_download(timeout=60000) as download_info:
                            target_btn.click(timeout=3000)
                        
                        download = download_info.value
                        orig_name = download.suggested_filename
                        ext = ".mp4" if "mp4" in orig_name.lower() or "video" in orig_name.lower() else ".mp3"
                        
                        safe_name = f"{idx:02d}_{sanitize_filename(raw_title)}{ext}"
                        dest_path = os.path.join(DOWNLOADS_DIR, safe_name)
                        
                        print(f"-> Salvando arquivo como: {safe_name}...")
                        download.save_as(dest_path)
                        
                        index_data.append({
                            "id": url,
                            "titulo": raw_title,
                            "arquivo": safe_name,
                            "tipo": "video" if ext == ".mp4" else "audio"
                        })
                        
                        with open(INDEX_FILE, "w", encoding="utf-8") as f:
                            json.dump(index_data, f, indent=2, ensure_ascii=False)
                            
                        print("-> Sucesso absoluto!")
                    except Exception as e:
                        print(f"-> Falha ao acionar o download: {e}")
                        with open("download_log.txt", "a", encoding="utf-8") as lf:
                            lf.write(f"[{idx}] {raw_title} - ERRO DOWNLOAD: {str(e)}\\n")
                    
                else:
                    print("-> ATENÇÃO: Nenhum botão de download visível após tentar abrir todas as opções para este caderno.")
                    with open("download_log.txt", "a", encoding="utf-8") as lf:
                        lf.write(f"[{idx}] {raw_title} - PENDENTE (Nenhum video/audio encontrado)\\n")
'''

with open('notebooklm_automator.py', 'w', encoding='utf-8') as f:
    f.writelines(lines[:start_idx])
    f.write(new_code)
    f.writelines(lines[end_idx+2:])

print('Feito via script updater')
