import re

fpath = r'e:\pbl3\flight\frontend\src\App.jsx'
with open(fpath, encoding='utf-8') as f:
    text = f.read()

before = text.count('\n')

# Collapse 2+ consecutive blank lines to single blank line  
text = re.sub(r'\n\n+', '\n\n', text)

after = text.count('\n')

with open(fpath, 'w', encoding='utf-8', newline='\n') as f:
    f.write(text)

print('Before lines:', before)
print('After lines:', after)
print('Done!')
