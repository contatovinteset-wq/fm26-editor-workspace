import codecs
import re

text = codecs.open('dump_7.txt', 'r', encoding='utf-8', errors='ignore').read()
home_idx = text.find('ReMapper-HomeClub')
if home_idx != -1:
    print('HomeClub context:', text[max(0, home_idx-500):home_idx+2000])
