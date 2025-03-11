using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
/// Utility class for rendering markdown-formatted text in Unity UI Toolkit
/// </summary>
public static class MarkdownRenderer
{
    // Colors for different markdown elements
    private static readonly Color HeadingColor = new Color(0.9f, 0.9f, 1.0f);
    private static readonly Color CodeColor = new Color(0.7f, 0.9f, 1.0f);
    private static readonly Color EmphasisColor = new Color(1.0f, 0.9f, 0.8f);
    private static readonly Color ListItemColor = new Color(0.9f, 1.0f, 0.9f);
    private static readonly Color TableHeaderColor = new Color(0.8f, 0.8f, 1.0f);
    private static readonly Color TableCellColor = new Color(0.8f, 0.8f, 0.8f);
    
    /// <summary>
    /// Renders markdown text as a collection of styled visual elements
    /// </summary>
    public static VisualElement RenderMarkdown(string markdownText)
    {
        var container = new VisualElement();
        container.style.flexGrow = 1;
        
        // Split the text into blocks (paragraphs, code blocks, etc.)
        var blocks = SplitIntoBlocks(markdownText);
        
        foreach (var block in blocks)
        {
            if (IsCodeBlock(block))
            {
                // Skip code blocks with file paths as they're handled separately
                if (block.StartsWith("```csharp:") || block.StartsWith("```cs:"))
                {
                    continue;
                }
                
                AddCodeBlock(container, ExtractCodeBlock(block));
            }
            else if (IsHeading(block, out int level))
            {
                AddHeading(container, block.Substring(level + 1), level);
            }
            else if (IsUnorderedList(block))
            {
                AddUnorderedList(container, block);
            }
            else if (IsOrderedList(block))
            {
                AddOrderedList(container, block);
            }
            else if (IsTable(block))
            {
                AddTable(container, block);
            }
            else
            {
                AddParagraph(container, block);
            }
        }
        
        return container;
    }
    
    private static List<string> SplitIntoBlocks(string text)
    {
        var blocks = new List<string>();
        var lines = text.Split('\n');
        
        string currentBlock = "";
        bool inCodeBlock = false;
        
        foreach (var line in lines)
        {
            // Check for code block markers
            if (line.StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    // Start of code block
                    if (!string.IsNullOrWhiteSpace(currentBlock))
                    {
                        blocks.Add(currentBlock.Trim());
                        currentBlock = "";
                    }
                    inCodeBlock = true;
                    currentBlock = line + "\n";
                }
                else
                {
                    // End of code block
                    currentBlock += line;
                    blocks.Add(currentBlock);
                    currentBlock = "";
                    inCodeBlock = false;
                }
                continue;
            }
            
            if (inCodeBlock)
            {
                // Inside code block, just append
                currentBlock += line + "\n";
                continue;
            }
            
            // Handle regular text blocks
            if (string.IsNullOrWhiteSpace(line))
            {
                if (!string.IsNullOrWhiteSpace(currentBlock))
                {
                    blocks.Add(currentBlock.Trim());
                    currentBlock = "";
                }
            }
            else
            {
                currentBlock += line + "\n";
            }
        }
        
        // Add the last block if there is one
        if (!string.IsNullOrWhiteSpace(currentBlock))
        {
            blocks.Add(currentBlock.Trim());
        }
        
        return blocks;
    }
    
    private static bool IsCodeBlock(string block)
    {
        return block.StartsWith("```") && block.EndsWith("```");
    }
    
    private static string ExtractCodeBlock(string block)
    {
        // Remove the opening and closing markers
        var lines = block.Split('\n');
        if (lines.Length <= 2)
        {
            return "";
        }
        
        // Skip first and last line (the markers)
        var codeLines = new List<string>();
        for (int i = 1; i < lines.Length - 1; i++)
        {
            codeLines.Add(lines[i]);
        }
        
        return string.Join("\n", codeLines);
    }
    
    private static bool IsHeading(string block, out int level)
    {
        level = 0;
        if (string.IsNullOrWhiteSpace(block))
        {
            return false;
        }
        
        // Count the number of # at the start
        for (int i = 0; i < block.Length; i++)
        {
            if (block[i] == '#')
            {
                level++;
            }
            else
            {
                break;
            }
        }
        
        return level > 0 && level <= 6 && block.Length > level && block[level] == ' ';
    }
    
    private static bool IsUnorderedList(string block)
    {
        var lines = block.Split('\n');
        bool hasBulletPoint = false;
        
        foreach (var line in lines)
        {
            string trimmed = line.TrimStart();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    hasBulletPoint = true;
                }
                else if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("  "))
                {
                    // If we find a non-empty line that doesn't start with a bullet or isn't indented, it's not a list
                    return false;
                }
            }
        }
        
        return hasBulletPoint;
    }
    
    private static bool IsOrderedList(string block)
    {
        var lines = block.Split('\n');
        bool hasNumberedItem = false;
        
        foreach (var line in lines)
        {
            string trimmed = line.TrimStart();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
                {
                    hasNumberedItem = true;
                }
                else if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("  "))
                {
                    // If we find a non-empty line that doesn't start with a number or isn't indented, it's not a list
                    return false;
                }
            }
        }
        
        return hasNumberedItem;
    }
    
    private static bool IsTable(string block)
    {
        var lines = block.Split('\n');
        if (lines.Length < 2)
        {
            return false;
        }
        
        // Check for table header separator (---|---|---)
        bool hasHeaderSeparator = false;
        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"^\s*\|?\s*-+\s*\|(\s*-+\s*\|)+\s*$"))
            {
                hasHeaderSeparator = true;
                break;
            }
        }
        
        return hasHeaderSeparator;
    }
    
    private static void AddCodeBlock(VisualElement container, string code)
    {
        var codeBlockContainer = new VisualElement();
        codeBlockContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        codeBlockContainer.style.borderLeftWidth = 2;
        codeBlockContainer.style.borderLeftColor = new Color(0.3f, 0.6f, 0.9f);
        codeBlockContainer.style.paddingLeft = 10;
        codeBlockContainer.style.paddingRight = 10;
        codeBlockContainer.style.paddingTop = 5;
        codeBlockContainer.style.paddingBottom = 5;
        codeBlockContainer.style.marginTop = 5;
        codeBlockContainer.style.marginBottom = 10;
        
        var codeText = new Label(code);
        codeText.style.whiteSpace = WhiteSpace.Normal;
        codeText.style.color = CodeColor;
        codeText.style.unityFont = Resources.Load<Font>("Fonts/RobotoMono-Regular");
        
        codeBlockContainer.Add(codeText);
        container.Add(codeBlockContainer);
    }
    
    private static void AddHeading(VisualElement container, string text, int level)
    {
        var heading = new Label(text.Trim());
        
        // Set font size based on heading level
        float fontSize = 20 - (level * 2); // h1=18, h2=16, h3=14, etc.
        heading.style.fontSize = fontSize;
        heading.style.unityFontStyleAndWeight = FontStyle.Bold;
        heading.style.color = HeadingColor;
        heading.style.marginTop = 10;
        heading.style.marginBottom = 5;
        
        container.Add(heading);
    }
    
    private static void AddParagraph(VisualElement container, string text)
    {
        var paragraph = new Label(FormatInlineMarkdown(text));
        paragraph.style.whiteSpace = WhiteSpace.Normal;
        paragraph.style.marginBottom = 8;
        paragraph.enableRichText = true;
        
        container.Add(paragraph);
    }
    
    private static void AddUnorderedList(VisualElement container, string block)
    {
        var listContainer = new VisualElement();
        listContainer.style.marginLeft = 15;
        listContainer.style.marginBottom = 10;
        
        var lines = block.Split('\n');
        foreach (var line in lines)
        {
            string trimmed = line.TrimStart();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }
            
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                var itemContainer = new VisualElement();
                itemContainer.style.flexDirection = FlexDirection.Row;
                itemContainer.style.marginBottom = 3;
                
                var bullet = new Label("•");
                bullet.style.marginRight = 5;
                bullet.style.color = ListItemColor;
                bullet.style.unityFontStyleAndWeight = FontStyle.Bold;
                
                string bulletText = trimmed.StartsWith("- ") ? trimmed.Substring(2) : trimmed.Substring(2);
                
                var content = new Label(FormatInlineMarkdown(bulletText));
                content.style.whiteSpace = WhiteSpace.Normal;
                content.style.flexGrow = 1;
                content.enableRichText = true;
                
                itemContainer.Add(bullet);
                itemContainer.Add(content);
                listContainer.Add(itemContainer);
            }
        }
        
        container.Add(listContainer);
    }
    
    private static void AddOrderedList(VisualElement container, string block)
    {
        var listContainer = new VisualElement();
        listContainer.style.marginLeft = 15;
        listContainer.style.marginBottom = 10;
        
        var lines = block.Split('\n');
        int number = 1;
        
        foreach (var line in lines)
        {
            string trimmed = line.TrimStart();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }
            
            var match = Regex.Match(trimmed, @"^(\d+)\.\s(.*)$");
            if (match.Success)
            {
                var itemContainer = new VisualElement();
                itemContainer.style.flexDirection = FlexDirection.Row;
                itemContainer.style.marginBottom = 3;
                
                var numberLabel = new Label(match.Groups[1].Value + ".");
                numberLabel.style.marginRight = 5;
                numberLabel.style.minWidth = 20;
                numberLabel.style.color = ListItemColor;
                
                var content = new Label(FormatInlineMarkdown(match.Groups[2].Value));
                content.style.whiteSpace = WhiteSpace.Normal;
                content.style.flexGrow = 1;
                content.enableRichText = true;
                
                itemContainer.Add(numberLabel);
                itemContainer.Add(content);
                listContainer.Add(itemContainer);
                
                number++;
            }
        }
        
        container.Add(listContainer);
    }
    
    private static void AddTable(VisualElement container, string block)
    {
        var tableContainer = new VisualElement();
        tableContainer.style.marginBottom = 10;
        
        var lines = block.Split('\n');
        bool isHeader = true;
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || Regex.IsMatch(line, @"^\s*\|?\s*-+\s*\|(\s*-+\s*\|)+\s*$"))
            {
                // Skip empty lines and separator lines
                if (Regex.IsMatch(line, @"^\s*\|?\s*-+\s*\|(\s*-+\s*\|)+\s*$"))
                {
                    isHeader = false;
                }
                continue;
            }
            
            // Parse the table row
            var rowContainer = new VisualElement();
            rowContainer.style.flexDirection = FlexDirection.Row;
            rowContainer.style.marginBottom = 2;
            
            // Split the line into cells
            var cells = line.Split('|');
            
            foreach (var cell in cells)
            {
                if (string.IsNullOrWhiteSpace(cell))
                {
                    continue;
                }
                
                var cellElement = new Label(FormatInlineMarkdown(cell.Trim()));
                cellElement.style.paddingLeft = 5;
                cellElement.style.paddingRight = 5;
                cellElement.style.paddingTop = 2;
                cellElement.style.paddingBottom = 2;
                cellElement.style.borderTopWidth = 1;
                cellElement.style.borderRightWidth = 1;
                cellElement.style.borderBottomWidth = 1;
                cellElement.style.borderLeftWidth = 1;
                cellElement.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
                cellElement.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
                cellElement.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                cellElement.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
                cellElement.style.flexGrow = 1;
                cellElement.style.backgroundColor = isHeader ? TableHeaderColor : TableCellColor;
                cellElement.enableRichText = true;
                
                rowContainer.Add(cellElement);
            }
            
            tableContainer.Add(rowContainer);
        }
        
        container.Add(tableContainer);
    }
    
    private static string FormatInlineMarkdown(string text)
    {
        // Bold: **text** or __text__
        text = Regex.Replace(text, @"\*\*(.*?)\*\*|__(.*?)__", "<b>$1$2</b>");
        
        // Italic: *text* or _text_
        text = Regex.Replace(text, @"(?<!\*)\*(?!\*)(.*?)(?<!\*)\*(?!\*)|(?<!_)_(?!_)(.*?)(?<!_)_(?!_)", "<i>$1$2</i>");
        
        // Code: `text`
        text = Regex.Replace(text, @"`(.*?)`", "<color=#" + ColorUtility.ToHtmlStringRGB(CodeColor) + ">$1</color>");
        
        // Links: [text](url)
        text = Regex.Replace(text, @"\[(.*?)\]\((.*?)\)", "<color=#3498db><u>$1</u></color>");
        
        return text;
    }
} 