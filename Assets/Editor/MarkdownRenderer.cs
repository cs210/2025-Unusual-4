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
    private static readonly Color HeadingColor = new Color(0.9f, 0.9f, 1.0f, 1f);
    private static readonly Color CodeColor = new Color(0.7f, 0.9f, 1.0f, 1f);
    private static readonly Color EmphasisColor = new Color(1.0f, 0.9f, 0.8f, 1f);
    private static readonly Color ListItemColor = new Color(0.9f, 1.0f, 0.9f, 1f);
    private static readonly Color TableHeaderColor = new Color(0.8f, 0.8f, 1.0f, 1f);
    private static readonly Color TableCellColor = new Color(0.8f, 0.8f, 0.8f, 1f);

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

                var codeBlockElement = RenderCodeBlock("", ExtractCodeBlock(block));
                container.Add(codeBlockElement);
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

                string bulletText = trimmed.Substring(2);

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
                cellElement.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                cellElement.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                cellElement.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                cellElement.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f, 1f);
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

    // Modified RenderCodeBlock method with a darker gray header, copy button, and code syntax highlighting.
    public static VisualElement RenderCodeBlock(string filePath, string code)
    {
        var container = new VisualElement();

        // Header container with a darker gray background (#333333) and rounded top corners
        var headerContainer = new VisualElement();
        headerContainer.style.backgroundColor = new Color(51f / 255f, 51f / 255f, 51f / 255f, 1f);
        headerContainer.style.flexDirection = FlexDirection.Row;
        headerContainer.style.justifyContent = Justify.SpaceBetween;
        headerContainer.style.alignItems = Align.Center;
        headerContainer.style.paddingLeft = 10;
        headerContainer.style.paddingRight = 10;
        headerContainer.style.paddingTop = 5;
        headerContainer.style.paddingBottom = 5;
        headerContainer.style.borderTopLeftRadius = 5;
        headerContainer.style.borderTopRightRadius = 5;
        headerContainer.style.width = new Length(95, LengthUnit.Percent);
        headerContainer.style.marginLeft = new Length(2.5f, LengthUnit.Percent);

        // Header label
        Label headerLabel;
        if (!string.IsNullOrEmpty(filePath))
        {
            headerLabel = new Label("Code: " + System.IO.Path.GetFileName(filePath) + " (" + filePath + ")");
        }
        else
        {
            headerLabel = new Label("C#");
        }
        headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        headerLabel.style.color = new Color(220f / 255f, 220f / 255f, 220f / 255f, 1f);
        headerLabel.style.flexGrow = 1;
        headerContainer.Add(headerLabel);

        // Copy button
        var copyButton = new Button();
        copyButton.text = "Copy";
        copyButton.style.marginLeft = 5;
        copyButton.clicked += () =>
        {
            GUIUtility.systemCopyBuffer = code;
            copyButton.text = "Copied";
            copyButton.schedule.Execute(() => { copyButton.text = "Copy"; }).StartingIn(2000);
        };
        headerContainer.Add(copyButton);

        container.Add(headerContainer);

        // Code block container with a dark background (#1E1E1E), rounded bottom corners, and slight horizontal margin
        var codeBlockContainer = new VisualElement();
        codeBlockContainer.style.backgroundColor = new Color(30f / 255f, 30f / 255f, 30f / 255f, 1f);
        codeBlockContainer.style.borderBottomLeftRadius = 5;
        codeBlockContainer.style.borderBottomRightRadius = 5;
        codeBlockContainer.style.paddingLeft = 10;
        codeBlockContainer.style.paddingRight = 10;
        codeBlockContainer.style.paddingTop = 5;
        codeBlockContainer.style.paddingBottom = 5;
        codeBlockContainer.style.marginTop = 0;
        codeBlockContainer.style.marginBottom = 10;
        codeBlockContainer.style.width = new Length(95, LengthUnit.Percent);
        codeBlockContainer.style.marginLeft = new Length(2.5f, LengthUnit.Percent);

        // Apply syntax highlighting to the code
        string highlightedCode = HighlightSyntax(code);

        var codeText = new Label(highlightedCode);
        codeText.style.whiteSpace = WhiteSpace.Normal;
        codeText.style.color = new Color(212f / 255f, 212f / 255f, 212f / 255f, 1f);
        codeText.enableRichText = true;

        // Use monospaced font if available
        var monoFont = Resources.Load<Font>("Fonts/RobotoMono-Regular");
        if (monoFont != null)
        {
            codeText.style.unityFontDefinition = new StyleFontDefinition(monoFont);
        }

        codeBlockContainer.Add(codeText);
        container.Add(codeBlockContainer);

        return container;
    }

    // Updated syntax highlighter that avoids highlighting keywords inside comments
    // and uses typical "VS Dark" style colors.
    private static string HighlightSyntax(string code)
    {
        // Split code by comments (capturing the comment markers)
        var parts = Regex.Split(code, @"(//.*?$)", RegexOptions.Multiline);
        for (int i = 0; i < parts.Length; i++)
        {
            // If this part is a comment, wrap it with the comment color without altering its contents.
            if (Regex.IsMatch(parts[i], @"^//"))
            {
                parts[i] = $"<color=#6A9955>{parts[i]}</color>";
            }
            else
            {
                // Highlight strings
                parts[i] = Regex.Replace(parts[i], "\"(.*?)\"", "<color=#CE9178>\"$1\"</color>");

                // Highlight numeric constants
                parts[i] = Regex.Replace(parts[i], @"\b\d+\b", "<color=#B5CEA8>$0</color>");

                // Highlight class/struct/interface/enum names after their keywords
                parts[i] = Regex.Replace(
                    parts[i],
                    @"(?<=\b(class|struct|enum|interface)\s+)([A-Za-z_]\w*)",
                    "<color=#4EC9B0>$2</color>"
                );

                // Highlight typical C# keywords in non-comment segments
                string[] keywords = new string[] {
                    "using", "public", "private", "static", "class", "void", 
                    "if", "else", "return", "for", "foreach", "while", "switch", "case", 
                    "break", "continue", "new", "true", "false", "null", "var"
                };

                foreach (var keyword in keywords)
                {
                    parts[i] = Regex.Replace(
                        parts[i],
                        $@"\b{keyword}\b",
                        $"<color=#569CD6>{keyword}</color>"
                    );
                }
            }
        }
        return string.Join("", parts);
    }
}
