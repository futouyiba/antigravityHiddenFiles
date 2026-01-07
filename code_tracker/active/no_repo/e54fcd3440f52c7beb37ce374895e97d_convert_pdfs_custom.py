�6import pdfplumber
import re
import os

def clean_text(text):
    return text.strip()

def is_header(line, body_font_size):
    # Simple heuristic: if font size is significantly larger than body text
    # or if it's bold (though pdfplumber bold detection is sometimes tricky)
    if not line['chars']:
        return False
    
    # Check max font size in the line
    max_size = max(c['size'] for c in line['chars'])
    if max_size > body_font_size + 1: # Threshold of 1pt diff
        return True
    
    # Check for bold (fontname often contains 'Bold')
    # This is brittle, but commonly works
    font_names = [c['fontname'] for c in line['chars']]
    if any('Bold' in f for f in font_names):
        return True
        
    return False

def get_indent_level(x0, page_width):
    # Heuristic for indentation
    # nominal left margin
    margin = 50 # approximate
    indent = x0 - margin
    if indent < 10:
        return 0
    elif indent < 30:
        return 1
    elif indent < 50:
        return 2
    else:
        return 3

def process_pdf(pdf_path, output_path):
    print(f"Processing {pdf_path}...")
    
    markdown_lines = []
    
    with pdfplumber.open(pdf_path) as pdf:
        # First pass: Determine body font size (mode)
        all_sizes = []
        for page in pdf.pages:
            chars = page.chars
            for c in chars:
                all_sizes.append(c['size'])
        
        if not all_sizes:
            print(f"No text text found in {pdf_path}")
            return

        from collections import Counter
        body_font_size = Counter(all_sizes).most_common(1)[0][0]
        print(f"Estimated body font size: {body_font_size}")

        for page in pdf.pages:
            # Extract lines with layout info
            # pdfplumber doesn't give "lines" directly in a way that groups strictly by structure, 
            # but we can sort words/chars. 
            # Using extract_text(layout=True) helps but returns string.
            # We want more control.
            
            # Instead, let's look at `extract_words` with extra info or custom grouping?
            # actually `extract_text` with x_tolerance might be enough for basic text, 
            # but for Headers we need the font info.
            
            # Let's iterate over rows.
            rows = {} # y -> list of chars
            for char in page.chars:
                # Group by rounded Y to handle slight misalignments
                y = round(char['top']) # explicit rounding might be too aggressive, let's use a tolerance
                # Using a bucketing approach
                found_bucket = False
                for existing_y in rows:
                    if abs(existing_y - char['top']) < 3: # 3pt tolerance
                        rows[existing_y].append(char)
                        found_bucket = True
                        break
                if not found_bucket:
                    rows[char['top']] = [char]
            
            # Sort rows by Y
            sorted_y = sorted(rows.keys())
            
            for y in sorted_y:
                line_chars = sorted(rows[y], key=lambda c: c['x0'])
                text = "".join([c['text'] for c in line_chars])
                text = text.strip()
                if not text:
                    continue
                
                # Check for header
                line_info = {'chars': line_chars, 'text': text}
                
                # Basic header detection
                is_head = is_header(line_info, body_font_size)
                
                prefix = ""
                if is_head:
                    # Determine level based on size
                    max_size = max(c['size'] for c in line_chars)
                    diff = max_size - body_font_size
                    if diff > 6:
                        prefix = "# "
                    elif diff > 4:
                        prefix = "## "
                    elif diff > 2:
                        prefix = "### "
                    else:
                        prefix = "#### " # Bold but small
                
                # List detection
                # Check start of text
                # We also check x0 for indentation
                first_char_x0 = line_chars[0]['x0']
                # page.width might be needed for relative calc, but absolute usually works fine for standard docs
                
                # Check if text looks like a list item
                is_list_item = False
                list_match = re.match(r'^(\d+\.|-|\*)\s+', text)
                if list_match:
                    is_list_item = True
                
                # If it's a list item, we might want to add indentation for markdown
                # But typically markdown handles nested lists by 2 or 4 spaces.
                # Let's try to map x0 to indentation spaces.
                # Assuming standard left margin is the minimum x0 seen in the document roughly.
                
                # For now, let's just create the line
                final_line = text
                
                if prefix:
                    final_line = f"\n{prefix}{text}\n"
                else:
                    # If it looks like a list item, prepending indentation
                    # We need a robust way to calculate indentation based on the document structure
                    # But for a quick script, let's try a simple mapping relative to a base
                    # If we simply output the text, manual cleanup might be needed, 
                    # but let's try to be smart.
                    
                    if is_list_item:
                         # Very rough accumulation of spaces
                         # usually indented lists are shifted by ~10-20 pts
                         # We'll need to calibrate this base x0 per page or doc.
                         # For now, let's assume < 60 is level 0, 60-90 level 1, etc.
                         if first_char_x0 > 50:
                             final_line = "    " + text # Indent 
                         if first_char_x0 > 80:
                             final_line = "        " + text
                    
                markdown_lines.append(final_line)

    with open(output_path, 'w', encoding='utf-8') as f:
        f.write("\n".join(markdown_lines))
    print(f"Saved to {output_path}")

if __name__ == "__main__":
    files = [
        "中鱼波动性掩盖问题.pdf",
        "【预研】优化-中鱼后的迅速感知.pdf"
    ]
    
    for pdf in files:
        if os.path.exists(pdf):
            md_name = pdf.replace(".pdf", ".md")
            process_pdf(pdf, md_name)
        else:
            print(f"File not found: {pdf}")
�6*cascade0822file:///d:/workOnSsd/biteFX/convert_pdfs_custom.py