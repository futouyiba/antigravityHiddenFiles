�import re
import os

def refine_markdown(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    lines = content.split('\n')
    new_lines = []
    
    for line in lines:
        stripped = line.strip()
        
        # specific fixes based on observation
        
        # fix lists that were mistaken for headers
        # ## • -> -
        # ## ◦ ->   -
        # ## ▪ ->     -
        
        # Regex for these patterns
        # We need to handle variable number of #
        
        # Check for bullet points hidden in headers
        match_bullet = re.match(r'^(#+)\s*[•](.*)', stripped)
        match_circle = re.match(r'^(#+)\s*[◦](.*)', stripped)
        match_square = re.match(r'^(#+)\s*[▪](.*)', stripped)
        
        if match_bullet:
            # Level 1 list
            new_lines.append(f"- {match_bullet.group(2).strip()}")
        elif match_circle:
             # Level 2 list
            new_lines.append(f"  - {match_circle.group(2).strip()}")
        elif match_square:
             # Level 3 list
            new_lines.append(f"    - {match_square.group(2).strip()}")
        else:
            # Check for numbered lists in headers
            # e.g. # 1. Background -> 1. Background (headers usually don't have ., but maybe they do)
            # Actually, user wants headers to be headers. "1. Background" IS likely a header.
            # But "4.1 Basic Idea" is also a header.
            # So I should keep headers if they look like section titles.
            
            # What about "## ◦ a1"? That was fixed above.
            
            # Clean up excessive # if it seems wrong?
            # E.g. "## 1.Background" is fine.
            
            # Just add the line as is if no list marker found
            new_lines.append(line)

    # Join and clean up newlines
    text = "\n".join(new_lines)
    
    # Remove more than 2 newlines
    text = re.sub(r'\n{3,}', '\n\n', text)
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(text)
    print(f"Refined {file_path}")

if __name__ == "__main__":
    files = [
        "中鱼波动性掩盖问题.md",
        "【预研】优化-中鱼后的迅速感知.md"
    ]
    
    for md in files:
        if os.path.exists(md):
            refine_markdown(md)
        else:
            print(f"File not found: {md}")
�*cascade082.file:///d:/workOnSsd/biteFX/refine_markdown.py